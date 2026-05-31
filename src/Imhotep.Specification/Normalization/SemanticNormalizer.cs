using Imhotep.SemanticModel.Entities;
using Imhotep.SemanticModel.Graph;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Specification.Normalization;

public class SemanticNormalizer : ISemanticNormalizer
{

   public Task<CanonicalSemanticModel> NormalizeAsync(StructuredSpecificationPayload payload, CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      if (payload == null || payload.ExtractedEntities == null)
         throw new ArgumentException("Payload and ExtractedEntities cannot be null.");

      // 1. ISL v1.1 Sec 25.2: Parse each strongly-typed section
      // Pass both the extracted markdown string (with a fallback) and the full payload 
      string projectContent = payload.ExtractedEntities.GetValueOrDefault("Project") ?? string.Empty;
      var project = ParseProject(projectContent, payload);

      var contexts = ParseEntities<ContextEntity>(payload.ExtractedEntities.GetValueOrDefault("Context"));
      var stakeholders = ParseEntities<StakeholderEntity>(payload.ExtractedEntities.GetValueOrDefault("Stakeholder"));
      var actors = ParseEntities<ActorEntity>(payload.ExtractedEntities.GetValueOrDefault("Actor"));
      var capabilities = ParseEntities<CapabilityEntity>(payload.ExtractedEntities.GetValueOrDefault("Capability"));
      var requirements = ParseEntities<RequirementEntity>(payload.ExtractedEntities.GetValueOrDefault("Requirement"));
      var services = ParseEntities<ServiceEntity>(payload.ExtractedEntities.GetValueOrDefault("Service"));
      var interfaces = ParseEntities<InterfaceEntity>(payload.ExtractedEntities.GetValueOrDefault("Interface"));
      var dataEntities = ParseEntities<DataEntityModel>(payload.ExtractedEntities.GetValueOrDefault("DataEntity"));
      var workflows = ParseEntities<WorkflowEntity>(payload.ExtractedEntities.GetValueOrDefault("Workflow"));
      var policies = ParseEntities<PolicyEntity>(payload.ExtractedEntities.GetValueOrDefault("Policy"));
      var infrastructures = ParseEntities<InfrastructureEntity>(payload.ExtractedEntities.GetValueOrDefault("Infrastructure"));
      var validations = ParseEntities<ValidationEntity>(payload.ExtractedEntities.GetValueOrDefault("Validation"));

      // 2. Map structural TraceabilityEdges (ISL v1.4 Sec 10.1)
      var edges = new List<TraceabilityEdge>();

      // Explicit Edge Creation for the Traceability Graph

      // A. Build Traceability Edges for Validations (Rule: "validates")
      if (validations != null)
      {
         foreach (var validation in validations)
         {
            if (validation.Validates != null)
            {
               foreach (var targetId in validation.Validates)
               {
                  edges.Add(new TraceabilityEdge
                  {
                     EdgeId = $"EDG-{validation.TraceabilityId}-{targetId}",
                     SourceId = validation.TraceabilityId,
                     TargetId = targetId,
                     RelationshipType = "validates" // Strictly defined in ISL v1.1 / v1.4
                  });
               }
            }
         }
      }

      // B. Build Traceability Edges for Services (Rule: "implements")
      if (services != null)
      {
         foreach (var service in services)
         {
            if (service.Requirements != null)
            {
               foreach (var reqId in service.Requirements)
               {
                  edges.Add(new TraceabilityEdge
                  {
                     EdgeId = $"EDG-{service.TraceabilityId}-{reqId}",
                     SourceId = service.TraceabilityId,
                     TargetId = reqId,
                     RelationshipType = "implements"
                  });
               }
            }
         }
      }

      // 3. Resolve Root Identity and Version (ISL v1.1 Sec 10.2 & 28.1)
      string resolvedSystemId = project?.SystemId ?? payload.SystemId ?? "macs-default-system";
      string specVersion = payload.SpecificationVersion ?? "1.0.0";
      string canonicalModelVersion = "1.1.0";

      // 4. Instantiate the strongly-typed Canonical Semantic Model
      var semanticModel = new CanonicalSemanticModel
      {
         SystemId = resolvedSystemId,
         Version = specVersion,
         ModelVersion = canonicalModelVersion,

         TransactionId = payload.TransactionId,
         TargetArchitecture = payload.TargetArchitecture,
         Project = project,
         Contexts = contexts,
         Stakeholders = stakeholders,
         Actors = actors,
         Capabilities = capabilities,
         Requirements = requirements,
         Services = services,
         Interfaces = interfaces,
         DataEntities = dataEntities,
         Workflows = workflows,
         Policies = policies,
         Infrastructures = infrastructures,
         Validations = validations,

         // Edges are now fully populated and injected into the semantic model
         RelationshipEdge = edges
      };

      return Task.FromResult(semanticModel);
   }


   /// <summary>
   /// Parses the raw markdown text under the "# Project" header into a formal ISL v1.1 ProjectEntity.
   /// </summary>
   private ProjectEntity ParseProject(string content, StructuredSpecificationPayload payload)
   {
      // 1. ISL v1.0 Sec 8.3 & MACS STP: Regex to extract the strictly demarcated entity.
      // Expected format: **PROJ-INTAKE-001**: Court Case Intake REST Service. A MACS proof-of-concept subsystem...
      // Group "id": Extracts the identifier (e.g., PROJ-INTAKE-001)
      // Group "name": Extracts the title up to the first period (e.g., Court Case Intake REST Service)
      // Group "desc": Extracts the rest of the paragraph
      var match = Regex.Match(content, @"^\s*\*\*(?<id>[a-zA-Z0-9\-]+)\*\*\s*:\s*(?<name>[^\.]+)\.\s*(?<desc>.*)", RegexOptions.Singleline);

      string id = match.Success ? match.Groups["id"].Value.Trim() : $"PRJ-{Guid.NewGuid():N}";
      string name = match.Success ? match.Groups["name"].Value.Trim() : "Unknown Project";
      string description = match.Success ? match.Groups["desc"].Value.Trim() : content.Trim();

      // 2. ISL v1.1 Sec 10.0: Hydrate the fully compliant Root Project Entity
      return new ProjectEntity
      {
         Type = EntityType.Project,

         // Extracted from the markdown string
         TraceabilityId = id,
         Name = name,
         Description = description,

         // ISL v1.1 Sec 8.1 Base Schema Fields
         Version = payload.SpecificationVersion ?? "1.0.0",
         SourceSection = "Project",
         Status = "active",
         Relationships = null,
         Metadata = null,

         // ISL v1.1 Sec 10.2: Project-Specific Root Identity (Derived from ParsedPayload frontmatter)
         SystemId = payload.SystemId ?? "macs-default-system",
         SpecificationVersion = payload.SpecificationVersion ?? "1.0.0",
         IslVersion = payload.IslVersion ?? "1.0",

         // Defaults for MACS POC (In a full implementation, Domain/Owner might be extracted from frontmatter)
         Domain = "justice",
         Owner = "Enterprise Architecture",

         // Execution Gating properties [ISL v1.1 Sec 10.2]
         // Note: The Normalizer sets baseline values; the IReadinessEvaluator will actively upgrade/downgrade this later
         ReadinessLevel = "draft",
         RiskTier = "standard",

         Created = DateTimeOffset.UtcNow,
         LastModified = DateTimeOffset.UtcNow
      };
   }

   private List<T> ParseEntities<T>(string content) where T : class, new()
   {
      var entities = new List<T>();
      if (string.IsNullOrWhiteSpace(content)) return entities;

      var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
      T currentEntity = null;

      var fieldRegex = new Regex(@"^\s*[\*\-]\s*(?:\*\*)?([a-zA-Z-]+):(?:\*\*)?\s*(.*)");

      foreach (var line in lines)
      {
         var match = fieldRegex.Match(line);
         if (match.Success)
         {
            string key = match.Groups[1].Value.ToLowerInvariant();
            string value = match.Groups[2].Value.Trim();

            // ISL v1.0 dictates 'id' acts as the anchor for a new entity [1]
            if (key == "id")
            {
               currentEntity = new T();
               SetPropertyValue(currentEntity, "id", value);
               entities.Add(currentEntity);
            }
            else if (currentEntity != null)
            {
               // Assign the nested indented fields (name, role, concerns, etc.)
               SetPropertyValue(currentEntity, key, value);
            }
         }
      }

      return entities;
   }

   /// <summary>
   /// Uses Reflection to map string keys (like 'approval-authority') to C# properties (like 'ApprovalAuthority') 
   /// and handles type conversion for booleans, enums, and string lists.
   /// </summary>
   private void SetPropertyValue(object obj, string propertyName, string value)
   {
      if (obj == null || string.IsNullOrWhiteSpace(propertyName)) return;

      // 1. Normalize the property name: remove hyphens so "approval-authority" matches "ApprovalAuthority"
      string normalizedPropertyName = propertyName.Replace("-", "");

      // 2. Find the property on the target object ignoring case
      PropertyInfo property = obj.GetType().GetProperty(normalizedPropertyName,
          BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

      if (property != null && property.CanWrite)
      {
         try
         {
            object convertedValue = value;

            // 3. Handle specific ISL Data Type Conversions

            // Convert Boolean (e.g., "true" -> true)
            if (property.PropertyType == typeof(bool))
            {
               convertedValue = bool.Parse(value);
            }
            // Convert Arrays/Lists (e.g., "[automated impact analysis, safe system evolution]" -> List<string>)
            else if (property.PropertyType == typeof(List<string>) ||
                     property.PropertyType == typeof(IReadOnlyList<string>) ||
                     property.PropertyType == typeof(IEnumerable<string>))
            {
               // Strip the brackets and split by comma
               string cleanList = value.Trim('[', ']');
               convertedValue = cleanList.Split(',')
                                         .Select(s => s.Trim())
                                         .Where(s => !string.IsNullOrEmpty(s))
                                         .ToList();
            }
            // Convert Enums (e.g., "must-have" -> Priority.MustHave)
            else if (property.PropertyType.IsEnum)
            {
               // Remove hyphens to match standard C# Enum names
               string cleanEnumValue = value.Replace("-", "");
               convertedValue = Enum.Parse(property.PropertyType, cleanEnumValue, ignoreCase: true);
            }

            // 4. Apply the converted value to the object
            property.SetValue(obj, convertedValue);
         }
         catch (Exception ex)
         {
            // If conversion fails (e.g., bad enum string), silently catch or log it 
            // so the rest of the parsing doesn't crash.
            Console.WriteLine($"[DEBUG] Failed to map property '{propertyName}' with value '{value}': {ex.Message}");
         }
      }
   }

}
