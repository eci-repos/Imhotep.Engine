---
TRANSACTION_ID: TASK-BOOTSTRAP-INIT-001
AGENT_ROLES: [Specification Interpreter, Architecture Planner, Security Validator, Test Generator]
TARGET_ARCHITECTURE: IMHOTEP Distributed Autonomous Platform (.NET Enterprise Services)
---

# CONTEXT ASSEMBLY:
[To be populated: We will define the target subsystem here (e.g., Agent Orchestrator, Planning Engine, or Governance Service). We will also supply the relevant ISL v2.x architecture specifications, data schemas, and deployment targets to ground the reasoning agents.]

# OPERATIONAL CONSTRAINTS:
You must establish a bidirectional Traceability Graph. Assign persistent, unique identifiers to every defined entity, and explicitly cross-reference them across sections. Ensure the Validation section includes specific, deterministic rules mapped to testing tools. Compliance standards must be tagged as [Mandatory], [Recommended], or [Optional].

# OUTPUT CONTRACT:
Output ONLY a strict ISL Markdown document. Do not include conversational preambles, acknowledgments, or concluding remarks so the output is immediately machine-parseable. Output the document utilizing the following 13 discrete canonical headers exactly: Project, Context, Stakeholder, Actor, Capability, Requirement, Service, Interface, DataEntity, Workflow, Policy, Infrastructure, Validation.

Project
[Identifier]: [Name and Purpose of the Subsystem]

Context
[Identifier]: [Operating environment and architectural boundaries per ISL v2.0]

Stakeholder
[Identifier]: [Human governance roles, e.g., IT Architects, Security Validators]

Actor
[Identifier]: [Interacting subsystems, workers, or human triggers]

Capability
[Identifier]: [High-level functions this subsystem provides]

Requirement
[Identifier]: [Specific functional and non-functional rules]

Service
[Identifier]: [Logical deployable components]

Interface
[Identifier]: [Internal APIs, message queues, or service boundaries]

DataEntity
[Identifier]: [Structured memory or state models per ISL v2.2]

Workflow
[Identifier]: [Step-by-step orchestration logic]

Policy
[Identifier]: [Security, isolation, and Zero-Trust constraints per ISL v3.10]

Infrastructure
[Identifier]: [Deployment scale and topology per ISL v2.7]

Validation
[Identifier]: [Deterministic verification tools and rules mapped to ISL v1.6]

### CLARIFICATIONS REQUIRED
[Reserved for the Specification Interpreter agent to output structured questions if our provided Context Assembly contains ambiguous jurisdictional boundaries or validation criteria.]

