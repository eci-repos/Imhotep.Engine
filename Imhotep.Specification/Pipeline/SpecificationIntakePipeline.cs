using System;
using System.Collections.Generic;
using System.Text;
using Imhotep.SemanticModel.Graph;
using Imhotep.Specification.Parsing;
using Imhotep.Specification.Normalization;
using Imhotep.Specification.Evaluation;
using Imhotep.Specification.Feedback;

namespace Imhotep.Specification.Pipeline;

/// <summary>
/// Orchestrates the IMHOTEP Specification Intake Pipeline.
/// Wires together Parsing, Normalization, Readiness Evaluation, and the Feedback Loop.
/// </summary>
public class SpecificationIntakePipeline
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
   public async Task<CanonicalSemanticModel> ProcessPayloadAsync(string rawStp, CancellationToken cancellationToken = default)
   {
      // 1. Intake & Parse: Extract metadata and the 13 canonical entities
      ParsedPayload parsedPayload = await _parser.ParseAsync(rawStp, cancellationToken);

      // 2. Normalize: Build the Semantic Graph and Traceability Edges
      CanonicalSemanticModel semanticModel = await _normalizer.NormalizeAsync(parsedPayload, cancellationToken);

      // 3. Evaluate: Check against ISL Specification Readiness Levels
      SpecificationReadinessReport readinessReport = await _evaluator.EvaluateAsync(semanticModel, cancellationToken);

      // 4. Governance & Feedback Loop: If not Autonomous-Ready, halt and escalate
      if (!readinessReport.IsAutonomousReady)
      {
         // Format the strict Advisory Collaboration block
         string clarificationBlock = _formatter.FormatClarifications(readinessReport);

         // Pull the digital Andon Cord, routing back to Human Governance
         await _dispatcher.DispatchAsync(semanticModel.TransactionId, clarificationBlock, cancellationToken);
      }

      // 5. Machine-Valid & Autonomous-Ready: Return the model for the Planning Engine
      return semanticModel;
   }
}

