# InvariantAgent 2

A reference implementation of the Invariant-Bounded Agent Alignment Model (IBAAM).

## Key Idea

LLMs are treated as untrusted cognitive generators.

All actions must pass through invariant-bounded runtime control.

LLMs become cognitive plugins operating inside invariant-bounded execution.

## Core Hierarchy

```text
invariants > runtime > planner > tools
```

This is philosophically different from most current agent frameworks, which typically treat the LLM itself as the governing entity.

In InvariantAgent:
- invariants define boundaries,
- the runtime governs execution,
- planners generate decisions,
- and tools perform actions.

## Architecture

- Core: contracts + models

- Control: invariant enforcement

- Execution: planners + tools

- Adaptive: prompts + memory

- Observability: logs + replay

- Simulation: replayable state evolution

## Planner Support

- OpenAI planners

- Google Gemini planners

- Local deterministic rule-based planners

## Features

- Replayable execution

- Event-sourced state transitions

- Tool mediation

- Pre/post execution controls

- Deterministic simulation

- Model-agnostic planner abstraction

- Offline-capable execution paths

## Run

InvariantAgent 2 is started from the demo project (unlike InvariantAgent 1).


```bash
dotnet run --project InvariantAgent.Demo
```

Entry point:

```text
InvariantAgent.Demo/Program.cs
```

## Goal

InvariantAgent explores agent systems where:
- governance is runtime-enforced,
- invariants are first-class,
- and models are replaceable planning components.

## IBAAM

InvariantAgent is the reference implementation of the:

**Invariant-Bounded Agent Alignment Model (IBAAM)**

Additional details:
https://drive.google.com/file/d/1IVljpg-cmN2Q_pIryRBfT77NqDpSERc-/view