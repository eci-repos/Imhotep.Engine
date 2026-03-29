# Imhotep.Engine
**The Autonomous Software Construction Engine for Project IMHOTEP**

## Overview
**Imhotep.Engine** is the active, executable machinery of Project IMHOTEP, an open architecture designed for autonomous, specification-driven software construction. Despite decades of innovation, the core process of building software has remained largely a manual translation of architecture into code. Project IMHOTEP explores a new paradigm where software systems are constructed directly from structured architectural specifications through a coordinated platform of reasoning agents and deterministic engineering tools. 

This repository was born out of the need to transition Project IMHOTEP from a documented architectural framework into a living, operational construction pipeline known as the Minimal Autonomous Construction System (MACS). 

## Relationship to `Imhotep.Specifications`
This repository operates in direct partnership with the public open-source repository **`Imhotep.Specifications`**. 
*   **`Imhotep.Specifications`** serves as the architectural foundation, housing the IMHOTEP Specification Language (ISL), platform blueprints, reference documentation, and governance models. 
*   **`Imhotep.Engine`** is the physical realization of that design. It provides the runtime environment, agent orchestration, and deterministic tooling integrations required to actively read those ISL blueprints and autonomously generate the functioning software artifacts.

## Purpose and Goals
The primary goal of `Imhotep.Engine` is to execute the MACS Proof-of-Concept: autonomously generating a local-first, containerized .NET REST service from a formal Structured Transaction Payload. 

To achieve this, the repository is designed to build and operate the core platform subsystems:
*   **A Strict Zero-Trust Boundary:** The engine will securely ingest Structured Transaction Payloads, rigorously enforcing Markdown headers and YAML frontmatter while rejecting informal or unstructured prose. 
*   **Deep Semantic Normalization:** The engine will programmatically extract the parsed text and construct an in-memory Canonical Semantic Model, mapping the data exclusively to the 13 discrete canonical entities (Project, Context, Service, Policy, DataEntity, etc.).
*   **Bidirectional Traceability:** The engine will extract unique Traceability Identifiers (e.g., `REQ-001`, `POL-CJIS-001`) and enforce Explicit Edge Creation. This mathematically links all generated code and downstream artifacts back to their upstream constraints to support automated impact analysis and repair loops.
*   **Autonomous Orchestration:** Ultimately, this engine will orchestrate the Planning Engine, Reasoning Agents, and Execution Runtime to execute the autonomous development loop—generating implementation artifacts, deterministically validating them, and executing automated repair cycles until the software is stable.

## What You Will Find in This Repository
Following the official IMHOTEP Reference Repository Layout and Service Topology (ISL v3.1), `Imhotep.Engine` adopts a modular structure separating platform libraries, runtime services, and infrastructure concerns. 

Within the `src/` directory, you will find the foundational .NET core libraries:
*   **`Imhotep.Specification`**: The Specification Engine responsible for parsing, validating, and managing the intake of the ISL blueprints, verifying them against formal Specification Readiness Levels.
*   **`Imhotep.SemanticModel`**: The Semantic Model Engine that houses the in-memory graph representation of the 13 canonical entities. It serves as the single authoritative interpretation of the system blueprint.
*   **`Imhotep.Traceability`**: The Traceability Engine that maintains the bidirectional traceability graph, linking specifications, construction tasks, and generated artifacts to support impact analysis and governance audits.

As the engine matures through the MACS Proof-of-Concept, this repository will expand to include libraries for `Imhotep.Planning`, `Imhotep.Agents`, `Imhotep.Runtime`, and `Imhotep.Tools` to fully execute the autonomous software development lifecycle (SDLC).
