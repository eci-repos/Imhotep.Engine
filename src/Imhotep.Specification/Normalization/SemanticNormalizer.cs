using Imhotep.SemanticModel.Entities;
using Imhotep.SemanticModel.Graph;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.Specification.Normalization;

public class SemanticNormalizer : ISemanticNormalizer
{

   public Task<CanonicalSemanticModel> NormalizeAsync(ParsedPayload payload, CancellationToken cancellationToken = default)
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
      // var edges = BuildTraceabilityEdges(payload.ExtractedEntities); 
      var edges = new List<TraceabilityEdge>();

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
         RelationshipEdge = edges
      };

      return Task.FromResult(semanticModel);
   }

   /// <summary>
   /// Parses the raw markdown text under the "# Project" header into a formal ISL v1.1 ProjectEntity.
   /// </summary>
   private ProjectEntity ParseProject(string content, ParsedPayload payload)
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

   private List<T> ParseEntities<T>(string content) where T : ICanonicalEntity, new()
   {
      var entities = new List<T>();
      if (string.IsNullOrWhiteSpace(content)) return entities;

      // TODO: Implement regex logic to extract IDs (e.g., REQ-001) and instantiate T
      return entities;
   }

   private List<TraceabilityEdge> BuildTraceabilityGraph(IReadOnlyDictionary<string, string> sections)
   {
      var edges = new List<TraceabilityEdge>();
      // Add the regex cross-reference logic provided previously here
      return edges;
   }
}