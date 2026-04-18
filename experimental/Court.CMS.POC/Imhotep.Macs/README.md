# Minimal Autonomous Construction System (MACS)

The Intent of this Exercise is to simulate the role of the platform's Implementation Generator agent to establish the baseline Minimal Autonomous Construction System (MACS) Proof-of-Concept.

According to the IMHOTEP architecture, human intervention strictly pauses the moment the Structured Transaction Payload (STP) blueprint passes the formal Approval Gates and reaches the "Autonomous-Ready" level.
The platform's Specification-Driven Construction Directive explicitly dictates that the specification is the authoritative artifact and that no manual coding should bypass this blueprint.

In the live system, human engineers only define the intent, policies, and constraints within the STP. The machine then takes over completely:

- The Execution Runtime assumes control.
- The Planning Engine generates a construction task graph.
- The Implementation Generator agent autonomously writes the concrete .NET project files, service classes, API endpoints, and data models.
- The Tool Plugin Architecture deterministically compiles and validates that code.

Therefore, this is a demonstration of what the autonomous machine will automatically produce when it executes the NCSC NODS blueprint that had been authorized. Details of the POC follow.

## MACS Proof-of-Concept: Court Case Intake REST Service
**Execution Steps and Artifacts Repository**

This document outlines the comprehensive list of artifacts generated and the autonomous execution steps required to complete the Minimal Autonomous Construction System (MACS) Proof-of-Concept (POC) for the Court Case Intake REST Service. 

### Phase 1: Context Assembly and Pre-requisites
Before autonomous generation begins, human professionals must gather the foundational domain artifacts and schemas to ground the reasoning agents.
*   **NCSC Data Governance Policy Guide & NODS Documentation:** The operational rules dictating data privacy (PII), case lifecycle transitions, and distribution models.
*   **Canonical Relational Schema (`Psj_Court_Canonical`):** The enterprise CSV database schema defining the precise tables and constraints for court case data.
*   **Middleware Utility (`SchemaTranslator.cs`):** A custom C# utility housed in the `experimental/Court.CMS.POC` directory used to automatically translate the massive CSV schema into strict ISL-formatted Markdown blocks to prevent human parsing errors.
*   **Security & Infrastructure Specifications:** CJIS Security Policy, NIST 800-53 (Zero Trust) mandates, and Entra ID (OAuth) configuration parameters.

### Phase 2: The Architectural Blueprint (Specification Engine)
The human governance team translates the assembled context into the formal blueprint utilizing the 13 discrete canonical entities.
*   **Structured Transaction Payload (STP):** `TASK-SPEC-INIT-004` is the formal, machine-parseable blueprint authored using YAML-frontmatter and Markdown, serving as the definitive intent for the system.
*   **Formal Approval Gates:** Documented sign-offs from the Human Governance Team to transition the blueprint to the "Autonomous-Ready" level.
    *   *IT Architects:* Approved the API definition (`INT-001`) and Entra ID deployment.
    *   *Security Validators:* Approved CJIS encryption (`VAL-002`) and PII soft-deletion rules (`VAL-004`).
    *   *Chief Data Officer / Court Auditors:* Approved interoperability mappings to the NCSC `Psj_Court_Canonical` schema.
*   **Version-Controlled Commit:** The parsed blueprint is committed to the Artifact Repository to establish the authoritative architectural baseline.

### Phase 3: Generated Implementation Artifacts (.NET / C#)
The Implementation Generator agent autonomously produces the concrete source code for the local-first, containerized .NET REST service based on the STP.
*   **Canonical Base Interface:** `INodsAuditableEntity` (Enforces multi-tenancy and soft-deletion/PII masking).
*   **NODS Canonical Data Entities (`ENT-NODS-*`):** Immutable C# records mapped exactly to the NCSC schema:
    *   `CaseDetail` (`ENT-NODS-CASE`)
    *   `PartyDetail` (`ENT-NODS-PARTY`)
    *   `ChargeDetail` (`ENT-NODS-CHARGE`)
    *   `FilingEvent` (`ENT-NODS-FILING`)
    *   `HearingDetail` (`ENT-NODS-HEARING`)
*   **Intake Payload Contract:** `NodsCasePayload` (The structured JSON wrapper for external submissions).
*   **Case Submission API:** `CaseSubmissionController` (`INT-001`) (The RESTful communication boundary enforcing Entra ID authorization).
*   **Intake Workflow Orchestrator:** `IntakeRestService` (`SRV-INTAKE-01`) (The subsystem handling state transitions, schema validation routing, and PII notifications).

### Phase 4: Deterministic Validation Suites (Tool Plugins)
The Test Generator agent outputs deterministic test cases mapped to the Tool Plugin Architecture to objectively verify the generated code.
*   **`VAL-001` (NODS Schema Validation):** Tests ensuring the API payloads strictly bind to the `Psj_Court_Canonical` data structures.
*   **`VAL-002` (Encryption Verification):** Static analysis security scanner configurations ensuring data encryption satisfies `POL-CJIS-001`.
*   **`VAL-003` (Entra ID Authentication):** Identity Provider tests verifying that `INT-001` rejects unauthenticated payloads without a valid OAuth token.
*   **`VAL-004` (PII Scanning & Masking):** Tests executing against the Intake Service to verify that PII triggers the required soft-deletion notification per `POL-PII-001`.

### Phase 5: The Autonomous Execution & Continuous Evolution Loop
The platform's Execution Runtime drives the continuous autonomous development lifecycle (SDLC).
*   **Task Graph Generation:** The Planning Engine translates the canonical semantic model into sequential construction tasks.
*   **Automated Repair Cycles:** If the deterministic tests (Phase 4) fail, the Execution Runtime triggers the Repair Analyst agent to diagnose and regenerate the code iteratively until the system converges.
*   **Human-Machine Escalation (The "Andon Cord"):** The formal exception pathway triggered if an architectural conflict cannot be repaired, requiring human intervention.
*   **Watchtower Observability:** Real-time telemetry dashboards (Execution, Agent Activity, Tool Interaction, and Governance) monitored by human teams.
*   **Review & Deployment Packaging:** The Review Agent performs a final architectural assessment, and the Deployment Preparer outputs Dockerfiles and container manifests (`INF-001`) for the runtime environment.
*   **Traceability Audit & Impact Analysis ("Day 2" Operations):** The bidirectional Traceability Graph mathematically linking the generated code back to the STP, allowing Court Auditors to verify compliance and enabling the platform to perform targeted reconstruction when business requirements change.

