---
TRANSACTION_ID: TASK-BOOTSTRAP-TRACEABILITY-001
AGENT_ROLES: [Specification Interpreter, Architecture Planner, Security Validator, Test Generator]
TARGET_ARCHITECTURE: Minimal Autonomous Construction System (MACS) .NET REST Service
SYSTEM_ID: macs-traceability
SPECIFICATION_VERSION: 1.0.0
ISL_VERSION: 1.0
---

# CONTEXT ASSEMBLY:
**Target Subsystem:** The IMHOTEP Traceability Engine (`Imhotep.Traceability` domain module and `Imhotep.TraceabilityService` reference service).
**Operating Environment & Architectural Boundaries:** A .NET Enterprise Service environment supporting both local-first and distributed cluster deployment profiles. The Traceability Engine operates within the platform's Data Plane, independent of Planning and Execution layers. It maintains strict dependencies only on the `Imhotep.State` and `Imhotep.Observability` modules.
**Domain Standards & Alignment:** The engine's data schemas and graph generation logic must strictly align with the **W3C PROV-DM (Provenance Data Model)** to ensure standardized mapping of Entity, Activity, and Agent derivations.

# OPERATIONAL CONSTRAINTS:
You must establish a bidirectional Traceability Graph. Assign persistent, unique traceability identifiers formatted as `{PREFIX}-{system-id}-{sequence}` to every defined canonical entity. Explicitly cross-reference downstream entities to the upstream constraints they fulfill to enable automated impact analysis and targeted reconstruction. Ensure the Validation section includes specific, deterministic rules mapped to testing tools. Compliance standards must be tagged as [Mandatory], [Recommended], or [Optional].

# OUTPUT CONTRACT:
Output ONLY a strict ISL Markdown document. Do not include conversational preambles, acknowledgments, or concluding remarks so the output is immediately machine-parseable. Output the document utilizing the 13 discrete canonical headers exactly to enforce Strict Entity Demarcation for the Specification Engine parser.

## Project
* **id:** PROJ-TRACE-001
* **name:** IMHOTEP Traceability Engine
* **description:** The foundational data plane service responsible for generating, maintaining, and querying the mathematically verifiable, bidirectional provenance graph of the autonomous software construction lifecycle.
* **version:** 1.0.0
* **readiness-level:** Machine-Valid
* **risk-tier:** Standard
* **owner:** IMHOTEP Platform Architecture Team

## Context
* **id:** CTX-TRACE-001
* **name:** Platform Data Plane Environment
* **description:** The system operates at the lowest tier of the platform's control plane. It sits beneath the Planning and Execution layers, meaning it cannot depend on them. 
* **external-systems:** [Imhotep.State, Imhotep.Observability]
* **domain-context:** Semantic graph structure strictly aligns with W3C PROV-DM (Provenance Data Model).

## Stakeholder
* **id:** STK-TRACE-001
  * **name:** Platform Architects
  * **role:** Architecture Reviewer
  * **concerns:** [automated impact analysis, safe system evolution, downstream artifact tracing]
  * **approval-authority:** true
* **id:** STK-TRACE-002
  * **name:** Court Auditors / Compliance Officers
  * **role:** Auditor
  * **concerns:** [mathematical non-repudiation, immutable proof of validation, regulatory compliance]
  * **approval-authority:** true

## Actor
* **id:** ACT-TRACE-001
  * **name:** Planning Engine
  * **actor-type:** system
  * **description:** Queries the Traceability Engine to perform impact analysis and generate task graphs.
  * **permissions:** [read-graph, execute-impact-analysis]
* **id:** ACT-TRACE-002
  * **name:** Execution Runtime
  * **actor-type:** system
  * **description:** Writes explicit traceability links during autonomous artifact generation and validation.
  * **permissions:** [write-graph, append-snapshot]

## Capability
* **id:** CAP-TRACE-001
  * **name:** Bidirectional Graph Management
  * **description:** The ability to persist and traverse a directed graph connecting all specification entities, tasks, artifacts, and validation results.
* **id:** CAP-TRACE-002
  * **name:** Automated Impact Analysis
  * **description:** The ability to calculate exactly which downstream elements are affected by an upstream specification entity change.
* **id:** CAP-TRACE-003
  * **name:** Immutable Snapshotting
  * **description:** The capability to capture versioned, point-in-time representations of the entire graph state at critical lifecycle checkpoints.

## Requirement
* **id:** REQ-TRACE-001
  * **statement:** The system MUST map the 13 ISL canonical node types and 19 edge types directly to W3C PROV-DM concepts.
  * **priority:** must-have
  * **source:** STK-TRACE-001
  * **validation:** VAL-TRACE-001
  * **fulfills:** CAP-TRACE-001
* **id:** REQ-TRACE-002
  * **statement:** The system MUST provide an impact analysis traversal algorithm capable of identifying all downstream artifacts affected by a specification change.
  * **priority:** must-have
  * **source:** ACT-TRACE-001
  * **validation:** VAL-TRACE-003
  * **fulfills:** CAP-TRACE-002
* **id:** REQ-TRACE-003
  * **statement:** The system MUST generate immutable TraceabilitySnapshot records at required lifecycle checkpoints.
  * **priority:** must-have
  * **source:** STK-TRACE-002
  * **validation:** VAL-TRACE-004
  * **fulfills:** CAP-TRACE-003

## Service
* **id:** SRV-TRACE-001
  * **name:** Traceability Service
  * **responsibility:** A deployable .NET subsystem providing the core graph persistence and query capabilities required by the execution runtime.
  * **requirements:** [REQ-TRACE-001, REQ-TRACE-002, REQ-TRACE-003]
  * **interfaces:** [INT-TRACE-001]
  * **data-entities:** [ENT-TRACE-NODE, ENT-TRACE-EDGE, ENT-TRACE-SNAPSHOT]
  * **statefulness:** stateful

## Interface
* **id:** INT-TRACE-001
  * **name:** Graph Query & Mutation API
  * **interface-type:** rest-api
  * **exposed-by:** SRV-TRACE-001
  * **used-by:** [ACT-TRACE-001, ACT-TRACE-002]
  * **contract:** OpenAPI v3 Contract
  * **authentication:** enterprise-identity-token
  * **versioning-strategy:** URI versioning

## DataEntity
* **id:** ENT-TRACE-NODE
  * **name:** TraceabilityNode
  * **description:** The canonical information model representing a vertex in the graph.
  * **attributes:** [nodeId:string:required, nodeType:string:required, metadata:object:optional]
  * **sensitivity:** internal
* **id:** ENT-TRACE-EDGE
  * **name:** TraceabilityEdge
  * **description:** The canonical information model representing a directed relationship.
  * **attributes:** [edgeId:string:required, sourceNodeId:string:required, targetNodeId:string:required, edgeType:string:required]
  * **sensitivity:** internal
* **id:** ENT-TRACE-SNAPSHOT
  * **name:** TraceabilitySnapshot
  * **description:** An immutable data record preserving the exact state of the graph.
  * **attributes:** [snapshotId:string:required, nodeCount:integer:required, edgeCount:integer:required, storageLocation:string:required]
  * **sensitivity:** internal

## Workflow
* **id:** WKF-TRACE-001
  * **name:** Impact Analysis Workflow
  * **trigger:** Planning Engine submits a changed SpecificationEntity.
  * **steps:**
    1. ACT-TRACE-001 requests impact analysis via INT-TRACE-001.
    2. SRV-TRACE-001 recursively traverses all downstream ENT-TRACE-EDGE associations.
    3. SRV-TRACE-001 returns an ImpactAnalysisRecord listing the affected nodes.
  * **exception-paths:** If the requested root node is missing or orphaned, the system MUST return a structured validation error and halt traversal.
  * **terminal-states:** [ImpactAnalysisRecord Generated, Error Returned]

## Policy
* **id:** POL-TRACE-001
  * **name:** Immutable History [Mandatory]
  * **policy-type:** data-handling
  * **rule:** Traceability edges MUST NOT be deleted to ensure absolute non-repudiation; corrections MUST be handled via supersession edge types.
  * **violation-response:** block
  * **applies-to:** [ENT-TRACE-EDGE, SRV-TRACE-001]
  * **validation:** [VAL-TRACE-005]
* **id:** POL-TRACE-002
  * **name:** Orphan Node Prevention [Mandatory]
  * **policy-type:** operational
  * **rule:** Every node MUST eventually connect to the root Project entity. Untraced stable artifacts MUST be rejected.
  * **violation-response:** block
  * **applies-to:** [ENT-TRACE-NODE, SRV-TRACE-001]
  * **validation:** [VAL-TRACE-002]

## Infrastructure
* **id:** INF-TRACE-001
  * **runtime-platform:** .NET 8 or later
  * **deployment-model:** container
  * **environments:** [local, dev, prod]
  * **resource-requirements:** { "cpu": "1 core minimum", "memory": "1GB minimum" }
  * **scaling-model:** horizontal
  * **monitoring:** [request latency, query depth, snapshot integrity]

## Validation
* **id:** VAL-TRACE-001
  * **name:** PROV-DM Compliance Check
  * **validates:** [REQ-TRACE-001]
  * **validation-type:** test
  * **method:** Deterministic .NET unit tests verifying `ENT-TRACE-NODE` and `ENT-TRACE-EDGE` map correctly to W3C PROV-DM syntax.
  * **pass-condition:** 100% schema compliance.
  * **automation-level:** automated
* **id:** VAL-TRACE-002
  * **name:** Orphan Detection Test
  * **validates:** [POL-TRACE-002]
  * **validation-type:** test
  * **method:** Integration test attempting to commit an artifact without a `derived-from` edge.
  * **pass-condition:** Request is mathematically rejected by the service.
  * **automation-level:** automated
* **id:** VAL-TRACE-003
  * **name:** Impact Analysis Traversal Test
  * **validates:** [REQ-TRACE-002]
  * **validation-type:** test
  * **method:** Execution of recursive CTE queries against a mocked relational graph state.
  * **pass-condition:** Query returns the exact mathematical set of expected downstream nodes.
  * **automation-level:** automated
* **id:** VAL-TRACE-004
  * **name:** Snapshot Immutability Check
  * **validates:** [REQ-TRACE-003]
  * **validation-type:** static-analysis
  * **method:** Code analysis verifying snapshot records contain no mutable properties or setters.
  * **pass-condition:** Zero mutability violations found.
  * **automation-level:** automated
* **id:** VAL-TRACE-005
  * **name:** Deletion Prevention Test
  * **validates:** [POL-TRACE-001]
  * **validation-type:** test
  * **method:** Execution of a hard-delete command against `ENT-TRACE-EDGE`.
  * **pass-condition:** The deletion is blocked and throws an exception.
  * **automation-level:** automated
