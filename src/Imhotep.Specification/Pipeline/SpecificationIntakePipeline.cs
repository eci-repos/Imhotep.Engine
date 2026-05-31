using System;
using System.Collections.Generic;
using System.Text;
using Imhotep.SemanticModel.Graph;
using Imhotep.Specification.Parsing;
using Imhotep.Specification.Normalization;
using Imhotep.Specification.Evaluation;
using Imhotep.Specification.Feedback;
using Imhotep.Contracts.Governance;

namespace Imhotep.Specification.Pipeline;

/// <summary>
/// Encapsulates the complete, validated output of the Specification Intake Pipeline.
/// </summary>
public record IntakePipelineResult(
    CanonicalSemanticModel SemanticModel,
    SpecificationReadinessReport ReadinessReport
);

/// <summary>
/// ISL v3.0: Formal contract for orchestrating the specification intake, 
/// semantic normalization, and readiness evaluation lifecycle.
/// </summary>
public interface ISpecificationIntakePipeline
{
   Task<IntakePipelineResult> ProcessPayloadAsync(string rawStp, CancellationToken cancellationToken = default);
}

/// <summary>
/// Orchestrates the IMHOTEP Specification Intake Pipeline.
/// Wires together Parsing, Normalization, Readiness Evaluation, and the Feedback Loop.
/// </summary>
public class SpecificationIntakePipeline : ISpecificationIntakePipeline
{
   private readonly IPayloadParser _parser;
   private readonly ISemanticNormalizer _normalizer;
   private readonly IReadinessEvaluator _evaluator;
   private readonly IClarificationFormatter _formatter;
   private readonly IResponseDispatcher _dispatcher;

   public SpecificationIntakePipeline(
       IPayloadParser parser,
       ISemanticNormalizer normalizer,
       IReadinessEvaluator evaluator,
       IClarificationFormatter formatter,
       IResponseDispatcher dispatcher)
   {
      _parser = parser;
      _normalizer = normalizer;
      _evaluator = evaluator;
      _formatter = formatter;
      _dispatcher = dispatcher;
   }

   /// <summary>
   /// Executes the full intake lifecycle. Returns the CanonicalSemanticModel if successful, 
   /// or throws a HumanMachineEscalationException if human clarification is required.
   /// </summary>
   public async Task<IntakePipelineResult> ProcessPayloadAsync(string rawStp, CancellationToken cancellationToken = default)
   {
      // 1. Intake & Parse: Extract metadata and the 13 canonical entities
      ExtractedPayload rawDoc = await _parser.ParseAsync(rawStp, cancellationToken);

      // 2. The Bridge: Map raw text into the strongly-typed Semantic Model payload
      var structuredPayload = new StructuredSpecificationPayload
      {
         TransactionId = rawDoc.Metadata.GetValueOrDefault("TRANSACTION_ID")
              ?? throw new InvalidOperationException("Missing mandatory TRANSACTION_ID."),

         AgentRoles = rawDoc.AgentRoles.AsReadOnly(),
         TargetArchitecture = rawDoc.Metadata.GetValueOrDefault("TARGET_ARCHITECTURE")
              ?? throw new InvalidOperationException("Missing mandatory TARGET_ARCHITECTURE."),

         SystemId = rawDoc.Metadata.GetValueOrDefault("SYSTEM_ID"),
         SpecificationVersion = rawDoc.Metadata.GetValueOrDefault("SPECIFICATION_VERSION"),
         IslVersion = rawDoc.Metadata.GetValueOrDefault("ISL_VERSION"),

         RawContextAssembly = rawDoc.ContextAssembly,
         ExtractedEntities = rawDoc.CanonicalSections
      };

      // 3. Normalize: Build the Semantic Graph and Traceability Edges
      CanonicalSemanticModel semanticModel = await _normalizer.NormalizeAsync(structuredPayload, cancellationToken);

      // 4. Evaluate: Check against ISL Specification Readiness Levels
      SpecificationReadinessReport readinessReport = await _evaluator.EvaluateAsync(semanticModel, cancellationToken);

      // 5. Governance & Feedback Loop: If not Autonomous-Ready, halt and escalate
      if (readinessReport.Level !=  ReadinessLevel.AutonomousReady)
      {
         // Format the strict Advisory Collaboration block
         string clarificationBlock = _formatter.FormatClarifications(readinessReport);

         // Pull the digital Andon Cord, routing back to Human Governance
         await _dispatcher.DispatchAsync(semanticModel.TransactionId, clarificationBlock, cancellationToken);
      }

      // 6. Machine-Valid & Autonomous-Ready: Return the model for the Planning Engine
      return new IntakePipelineResult(semanticModel, readinessReport);
   }
}

