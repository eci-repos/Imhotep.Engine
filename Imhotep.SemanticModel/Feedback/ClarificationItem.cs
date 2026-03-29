using System;
using System.Collections.Generic;
using System.Text;

// -------------------------------------------------------------------------------------------------
namespace Imhotep.SemanticModel.Feedback;

/// <summary>
/// Represents a specific structural gap, ambiguity, or missing canonical element 
/// identified by the Specification Interpreter agent during the Advisory Collaboration phase.
/// </summary>
public record ClarificationItem(
    string TargetEntityIdentifier, // eg., "POL-CJIS-001" or "Unknown" if an entity is missing
    string IssueCategory,          // eg., "MissingTraceabilityId", "ConflictingPolicy", ...
    string Description,            // A clear explanation of the gap preventing autonomous exec.
    string SuggestedResolution     // A structured recommendation for the Human Governance Team
);
