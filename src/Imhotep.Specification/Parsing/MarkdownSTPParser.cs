using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Imhotep.SemanticModel.Graph;

namespace Imhotep.Specification.Parsing;

/// <summary>
/// Concrete implementation of IPayloadParser for processing Markdown-based 
/// Structured Transaction Payloads (STPs) in the IMHOTEP architecture.
/// </summary>
public class MarkdownSTPParser : IPayloadParser
{
   // The exact 13 discrete canonical entities mandated by the ISL Canonical Semantic Model
   private static readonly string[] CanonicalHeaders = new[]
   {
         "Project", "Context", "Stakeholder", "Actor", "Capability",
         "Requirement", "Service", "Interface", "DataEntity", "Workflow",
         "Policy", "Infrastructure", "Validation"
     };

   public Task<ParsedPayload> ParseAsync(string rawPayload, CancellationToken cancellationToken = default)
   {
      // Check for task cancellation from the runtime orchestrator before starting
      cancellationToken.ThrowIfCancellationRequested();

      if (string.IsNullOrWhiteSpace(rawPayload))
         throw new ArgumentException("Payload content cannot be empty. The Specification Engine requires a valid STP.");

      // 1. Zero-Trust Boundary: Enforce Prohibited Artifacts
      EnforceSecurityBoundaries(rawPayload);

      cancellationToken.ThrowIfCancellationRequested();

      // 2. Metadata Extraction: Parse the YAML-style frontmatter
      var metadata = ExtractFrontmatter(rawPayload);

      cancellationToken.ThrowIfCancellationRequested();

      // 3. Strict Entity Demarcation: Slice the document by the 13 canonical headers
      var canonicalSections = ExtractCanonicalSections(rawPayload);

      // 4. Extract the Raw Context Assembly block explicitly
      string rawContextAssembly = ExtractContextAssembly(rawPayload);

      var payload = new ParsedPayload(
          TransactionId: metadata.GetValueOrDefault("TRANSACTION_ID"),
          AgentRoles: ParseAgentRoles(metadata.GetValueOrDefault("AGENT_ROLES")),
          TargetArchitecture: metadata.GetValueOrDefault("TARGET_ARCHITECTURE"),
          RawContextAssembly: rawContextAssembly,
          RawContent: rawPayload,
          ExtractedEntities: canonicalSections // Passes the dictionary of the 13 entities
      );

      // Return the successfully parsed payload wrapped in a completed Task
      return Task.FromResult(payload);
   }

   /// <summary>
   /// Extracts the text specifically under the # CONTEXT ASSEMBLY: header.
   /// </summary>
   private string ExtractContextAssembly(string content)
   {
      var match = Regex.Match(content, @"\#\s*CONTEXT ASSEMBLY:\s*(.*?)(?=\#\s*OPERATIONAL CONSTRAINTS:|\#\s*OUTPUT CONTRACT:|\#\#|\z)", RegexOptions.Singleline | RegexOptions.IgnoreCase);

      return match.Success ? match.Groups[6].Value.Trim() : string.Empty;
   }

   /// <summary>
   /// Actively rejects prohibited artifacts to ensure the platform operates purely on 
   /// architectural structure rather than acting as a generic coding assistant.
   /// </summary>
   private void EnforceSecurityBoundaries(string content)
   {
      // Prohibit unstructured code snippets and UI mockups
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
            throw new InvalidOperationException(
                $"Security Boundary Violation: Prohibited artifact detected matching '{pattern}'. " +
                "The generation of UI mockups or manual code snippets is strictly prohibited within the ISL Blueprint.");
         }
      }
   }

   /// <summary>
   /// Extracts the YAML-style frontmatter bounded by '---' at the start of the payload.
   /// </summary>
   private Dictionary<string, string> ExtractFrontmatter(string content)
   {
      var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      // Match the frontmatter block between --- and ---
      var frontmatterRegex = new Regex(@"^---\s*[\r\n]+(.*?)\s*[\r\n]+---", RegexOptions.Singleline);
      var match = frontmatterRegex.Match(content);

      if (!match.Success)
      {
         throw new InvalidOperationException("Invalid STP Format: Payload must begin with valid YAML-style frontmatter.");
      }

      var frontmatterContent = match.Groups[1].Value;
      var lines = frontmatterContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

      foreach (var line in lines)
      {
         var parts = line.Split(':', 2);
         if (parts.Length == 2)
         {
            metadata[parts[0].Trim()] = parts[1].Trim();
         }
      }

      return metadata;
   }

   /// <summary>
   /// Parses the Markdown document exclusively using the 13 canonical entity headers.
   /// </summary>
   private Dictionary<string, string> ExtractCanonicalSections(string content)
   {
      var sections = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      // Regex looks for Canonical headers formatted as either Markdown headers (### Project) or Bold text (**Project**)
      // Group 1 captures the actual canonical header name.
      string headerPattern = @"^(?:\#+|\*\*)\s*(" + string.Join("|", CanonicalHeaders) + @")\b\**\s*$";
      var regex = new Regex(headerPattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);

      var matches = regex.Matches(content);

      for (int i = 0; i < matches.Count; i++)
      {
         var currentMatch = matches[i];

         // CORRECTED: Apply .Trim() here to strip out any hidden \r or \n characters
         string headerName = currentMatch.Groups[1].Value.Trim();

         // Content spans from the end of the current header to the start of the next header (or end of document)
         int startIndex = currentMatch.Index + currentMatch.Length;
         int endIndex = (i + 1 < matches.Count) ? matches[i + 1].Index : content.Length;

         string sectionContent = content.Substring(startIndex, endIndex - startIndex).Trim();

         // Normalize key to exactly match the canonical casing
         string canonicalKey = CanonicalHeaders.First(h => h.Equals(headerName, StringComparison.OrdinalIgnoreCase));
         sections[canonicalKey] = sectionContent;
      }

      return sections;
   }

   private List<string> ParseAgentRoles(string rolesMetadata)
   {
      if (string.IsNullOrWhiteSpace(rolesMetadata)) return new List<string>();

      // Clean brackets e.g., "[Specification Interpreter, Architecture Planner]"
      var cleaned = rolesMetadata.Trim('[', ']');
      return cleaned.Split(',').Select(r => r.Trim()).ToList();
   }
}