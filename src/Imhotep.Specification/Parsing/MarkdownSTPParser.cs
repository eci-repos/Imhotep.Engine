using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Specification.Parsing
{
   /// <summary>
   /// ISL v3.0: Represents the structured data extracted from an ISL Markdown STP.
   /// </summary>
   public class ExtractedPayload
   {
      public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.OrdinalIgnoreCase);
      public List<string> AgentRoles { get; set; } = new();
      public string ContextAssembly { get; set; } = string.Empty;
      public string OperationalConstraints { get; set; } = string.Empty;
      public string OutputContract { get; set; } = string.Empty;
      public Dictionary<string, string> CanonicalSections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
   }

   /// <summary>
   /// Concrete implementation of IPayloadParser for processing Markdown-based
   /// Structured Transaction Payloads (STPs) in the IMHOTEP architecture.
   /// </summary>
   public class MarkdownSTPParser : IPayloadParser
   {
      // The exact 13 discrete canonical entities mandated by the ISL Canonical Semantic Model (ISL v1.1)
      private static readonly string[] CanonicalHeaders = new[]
      {
            "Project", "Context", "Stakeholder", "Actor", "Capability",
            "Requirement", "Service", "Interface", "DataEntity",
            "Workflow", "Policy", "Infrastructure", "Validation"
        };

      public Task<ExtractedPayload> ParseAsync(string rawPayload, CancellationToken cancellationToken = default)
      {
         // Check for task cancellation from the runtime orchestrator before starting
         cancellationToken.ThrowIfCancellationRequested();

         if (string.IsNullOrWhiteSpace(rawPayload))
            throw new ArgumentException("Payload cannot be null or empty.", nameof(rawPayload));

         // 1. Enforce Zero-Trust bounds (Prohibited Artifacts)
         EnforceSecurityBoundaries(rawPayload);

         var parsed = new ExtractedPayload();

         // 2. Extract YAML Frontmatter Metadata
         parsed.Metadata = ExtractFrontmatter(rawPayload);
         if (parsed.Metadata.TryGetValue("AGENT_ROLES", out var rolesStr))
         {
            parsed.AgentRoles = ParseAgentRoles(rolesStr);
         }

         // 3. Extract Main Blueprint Sections (Resilient to #, ##, or ###)
         parsed.ContextAssembly = ExtractMainSection(rawPayload, "CONTEXT ASSEMBLY:");
         parsed.OperationalConstraints = ExtractMainSection(rawPayload, "OPERATIONAL CONSTRAINTS:");
         parsed.OutputContract = ExtractMainSection(rawPayload, "OUTPUT CONTRACT:");

         // 4. Extract 13 Canonical Entities
         parsed.CanonicalSections = ExtractCanonicalSections(rawPayload);

         return Task.FromResult(parsed);
      }

      /// <summary>
      /// Actively rejects prohibited artifacts to ensure the platform operates purely on 
      /// architectural structure rather than acting as a generic coding assistant.
      /// </summary>
      private void EnforceSecurityBoundaries(string content)
      {
         var prohibitedPatterns = new List<string>
            {
                @"```(?:csharp|cs|java|python|js|ts|html|css)", // Manual code blocks
                @"(?i)ui mockup",                               // UI mockups
                @"(?i)wireframe"                                // Wireframes
            };

         foreach (var pattern in prohibitedPatterns)
         {
            if (Regex.IsMatch(content, pattern))
            {
               throw new InvalidOperationException($"Security Boundary Violation: Prohibited artifact pattern detected ({pattern}).");
            }
         }
      }

      /// <summary>
      /// Extracts the YAML-style frontmatter bounded by '---' at the start of the payload.
      /// </summary>
      private Dictionary<string, string> ExtractFrontmatter(string content)
      {
         var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

         // Matches frontmatter block wrapped in "---"
         var match = Regex.Match(content, @"^---\s*[\r\n]+(.*?)[\r\n]+---\s*[\r\n]+", RegexOptions.Singleline);

         if (match.Success)
         {
            var lines = match.Groups[1].Value.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
               var colonIndex = line.IndexOf(':');
               if (colonIndex > 0)
               {
                  var key = line.Substring(0, colonIndex).Trim();
                  var value = line.Substring(colonIndex + 1).Trim();
                  metadata[key] = value;
               }
            }
         }
         return metadata;
      }

      /// <summary>
      /// A highly resilient extractor for the main assembly headers. 
      /// It tolerates any level of markdown heading (#, ##, ###).
      /// </summary>
      private string ExtractMainSection(string content, string headerName)
      {
         string canonicalPattern = string.Join("|", CanonicalHeaders);

         // Stop at the next main section, OR a canonical markdown header, OR a canonical inline prefix (e.g., "Project PROJ-01:")
         string stopPattern = $@"^#+\s*OPERATIONAL CONSTRAINTS:|^#+\s*OUTPUT CONTRACT:|^#+\s*CONTEXT ASSEMBLY:|^#+\s*(?:{canonicalPattern})\b|^(?:{canonicalPattern})\s+(?=[A-Z0-9\-]+:)";

         // (?im) = IgnoreCase + Multiline. RegexOptions.Singleline allows '.' to match newline characters.
         string pattern = $@"(?im)^#+\s*{Regex.Escape(headerName)}\s*(.*?)(?={stopPattern}|\z)";

         var match = Regex.Match(content, pattern, RegexOptions.Singleline);
         return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
      }

      /// <summary>
      /// Extracts the 13 discrete canonical entities.
      /// It acts as a resilient fallback: It matches strictly demarcated markdown headers (e.g., "## Project") 
      /// OR plain-text paragraphs that begin with the canonical name followed by an ID (e.g., "Project PROJ-01:").
      /// </summary>
      private Dictionary<string, string> ExtractCanonicalSections(string content)
      {
         var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
         string canonicalPattern = string.Join("|", CanonicalHeaders);

         // Group 1: Matches the boundary (either a markdown header OR a plain text trigger looking ahead for an ID).
         // Group 2: Captures the canonical name if it was a markdown header (e.g. "## Project")
         // Group 3: Captures the canonical name if it was inline plain text (e.g. "Project PROJ-001:")
         // Group 4: Captures the actual body payload of that section.
         string boundaryPattern = $@"^#+\s*({canonicalPattern})\b|^({canonicalPattern})\s+(?=[A-Z0-9]+-[A-Z0-9\-]+:)";
         string pattern = $@"(?im)({boundaryPattern})\s*(.*?)(?={boundaryPattern}|\z)";

         // RegexOptions.Singleline ensures '.' captures the line breaks inside the body.
         var matches = Regex.Matches(content, pattern, RegexOptions.Singleline);

         foreach (Match match in matches)
         {
            // Determine if it was captured as a header (Group 2) or an inline prefix (Group 3)
            string header = match.Groups[2].Success ? match.Groups[2].Value.Trim() : match.Groups[3].Value.Trim();
            string body = match.Groups[4].Value.Trim();

            // If it was captured as an inline prefix (Option A fallback), we must prepend the ID back into the body
            // because the regex consumed the word "Project " but left the "PROJ-001:" in the body. 
            // This keeps the parser behavior uniform.
            sections[header] = body;
         }

         return sections;
      }

      /// <summary>
      /// Cleans and splits the AGENT_ROLES array from the YAML frontmatter.
      /// </summary>
      private List<string> ParseAgentRoles(string rolesMetadata)
      {
         if (string.IsNullOrWhiteSpace(rolesMetadata)) return new List<string>();

         var clean = rolesMetadata.Trim('[', ']');
         return clean.Split(',')
                     .Select(r => r.Trim())
                     .Where(r => !string.IsNullOrEmpty(r))
                     .ToList();
      }
   }
}
