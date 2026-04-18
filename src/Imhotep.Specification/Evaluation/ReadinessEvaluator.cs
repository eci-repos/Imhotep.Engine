using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Imhotep.SemanticModel.Graph;
using Imhotep.SemanticModel.Entities;

namespace Imhotep.Specification.Evaluation;

/// <summary>
/// Concrete implementation of IReadinessEvaluator.
/// Evaluates the CanonicalSemanticModel against ISL Specification Readiness Levels
/// and generates a SpecificationReadinessReport.
/// </summary>
public class ReadinessEvaluator : IReadinessEvaluator
{
   public Task<SpecificationReadinessReport> EvaluateAsync(
      CanonicalSemanticModel model, CancellationToken cancellationToken = default)
   {
      // Respect the runtime orchestrator's cancellation token
      cancellationToken.ThrowIfCancellationRequested();

      var missingElements = new List<string>();
      var unmappedValidations = new List<string>();
      var conflictingPolicies = new List<string>(); // Placeholder for future advanced policy evaluation

      // 1. Evaluate Core Canonical Element Presence
      // The MACS POC requires at minimum a Project, Contexts, Requirements, and Validations.
      if (model.Project == null || string.IsNullOrWhiteSpace(model.Project.TraceabilityId))
         missingElements.Add("Project entity is missing or lacks a Traceability ID.");

      if (model.Contexts == null || !model.Contexts.Any())
         missingElements.Add("At least one Context entity is required.");

      if (model.Requirements == null || !model.Requirements.Any())
         missingElements.Add("At least one Requirement entity is required.");

      if (model.Validations == null || !model.Validations.Any())
         missingElements.Add("At least one Validation entity is required to satisfy Deterministic Validation.");

      // 2. Evaluate the Traceability Graph (Explicit Edge Verification)
      // Ensures downstream validation rules are properly mapped to upstream constraints.
      if (model.Validations != null && model.TraceabilityGraph != null)
      {
         foreach (var validation in model.Validations)
         {
            cancellationToken.ThrowIfCancellationRequested();

            // Verify if this Validation ID exists as a source in any traceability edge
            bool hasMapping = model.TraceabilityGraph.Any(e => e.SourceId == validation.TraceabilityId);

            if (!hasMapping)
            {
               unmappedValidations.Add($"Validation rule '{validation.TraceabilityId}' is unmapped. It must explicitly verify an upstream Requirement or Policy.");
            }
         }
      }

      // 3. Determine ISL Specification Readiness Levels
      // Machine-Valid: The specification has successfully populated all core required structural elements.
      bool isMachineValid = !missingElements.Any();

      // Autonomous-Ready: It is Machine-Valid AND all traceability cross-references (validations) are resolved.
      bool isAutonomousReady = isMachineValid && !unmappedValidations.Any() && !conflictingPolicies.Any();

      // 4. Generate the structured Readiness Report
      var report = new SpecificationReadinessReport(
          IsMachineValid: isMachineValid,
          IsAutonomousReady: isAutonomousReady,
          MissingCanonicalElements: missingElements,
          UnmappedValidationRules: unmappedValidations,
          ConflictingPolicies: conflictingPolicies
      );

      return Task.FromResult(report);
   }
}
