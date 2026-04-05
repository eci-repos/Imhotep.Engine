using Tests.Library;

using Imhotep.Specification.Parsing;
using Imhotep.Specification.Normalization;
using Imhotep.Specification.Evaluation;
using Imhotep.Specification.Feedback;
using Imhotep.Specification.Pipeline;

namespace Imhotep.Tests.Integration;

public class SpecificationIntakePipelineTests
{
   private readonly SpecificationIntakePipeline _pipeline;

   public SpecificationIntakePipelineTests()
   {
      // Wire up the concrete implementations we drafted in Phase 3
      var parser = new MarkdownSTPParser();
      var normalizer = new SemanticNormalizer();
      var evaluator = new ReadinessEvaluator();
      var formatter = new ClarificationFormatter();
      var dispatcher = new ResponseDispatcher();

      _pipeline = new SpecificationIntakePipeline(parser, normalizer, evaluator, formatter, dispatcher);
   }

   [Fact]
   public async Task ProcessPayloadAsync_WithValidTaskSpec004_ShouldReturnCanonicalModel()
   {
      // Arrange: The validated MACS POC Payload
      string validPayload =
         await FolderFileHelper.ReadFileAsync("Artifacts", "Spec.Payload.Sample.1.md");

      try
      {
         // Act: Attempt to process the valid payload
         var result = await _pipeline.ProcessPayloadAsync(validPayload);

         // Assert: The execution should complete successfully without escalating
         Assert.NotNull(result);
         Assert.Equal("TASK-SPEC-INIT-004", result.TransactionId);
         Assert.Equal("Minimal Autonomous Construction System (MACS) .NET REST Service", result.TargetArchitecture);
         Assert.NotEmpty(result.TraceabilityEdges);
      }
      catch (HumanMachineEscalationException exception)
      {
         // Assert: The exact expected escalation exception was trapped.
         // Verify the Clarification Formatter successfully built the Advisory Collaboration block
         Assert.Contains("### CLARIFICATIONS REQUIRED", exception.FormattedResponse);
         Assert.Contains("At least one Validation entity is required", exception.FormattedResponse);
      }
      catch (Exception ex)
      {
         // Assert: A completely different exception was thrown (e.g., a file not found or parsing error).
         // We fail the test and output the specific exception.
         Assert.Fail($"Expected the payload to process successfully, but an exception was thrown: {ex.GetType().Name} - {ex.Message}");
      }

   }

   [Fact]
   public async Task ProcessPayloadAsync_WithMissingValidationMapping_ShouldThrowEscalationException()
   {
      // Arrange: A payload missing the Validation section (which violates the Readiness Evaluator)
      string invalidPayload =
         await FolderFileHelper.ReadFileAsync("Artifacts", "Spec.Payload.Sample.2.md");
      // INTENTIONALLY MISSING Validations, Policies, etc.

      try
      {
         // Act: Attempt to process the invalid payload
         await _pipeline.ProcessPayloadAsync(invalidPayload);

         // If the code reaches this line, the pipeline failed to halt.
         // It should have triggered a human escalation, so we fail the test.
         Assert.Fail("The pipeline failed to pull the Andon Cord. Expected a HumanMachineEscalationException, but no exception was thrown.");
      }
      catch (HumanMachineEscalationException exception)
      {
         // Assert: The exact expected escalation exception was trapped.
         // Verify the Clarification Formatter successfully built the Advisory Collaboration block
         Assert.Contains("### CLARIFICATIONS REQUIRED", exception.FormattedResponse);
         Assert.Contains("At least one Validation entity is required", exception.FormattedResponse);
      }
      catch (Exception ex)
      {
         // Assert: A completely different exception was thrown (e.g., a parsing error or null reference).
         // We fail the test because it was not the controlled governance escalation we expected.
         Assert.Fail($"Expected a HumanMachineEscalationException, but a different exception was thrown: {ex.GetType().Name} - {ex.Message}");
      }

   }
}