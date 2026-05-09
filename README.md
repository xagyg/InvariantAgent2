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
- invariants define boundaries
- the runtime governs execution
- planners generate decisions
- tools perform actions

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

Additional details (original paper):
https://drive.google.com/file/d/1IVljpg-cmN2Q_pIryRBfT77NqDpSERc-/view

## Example Session

```text
=== InvariantAgent REPL ===

Commands:
  exit      - quit
  clear     - clear screen

agent> echo hi

INPUT: echo hi

[Step] StepId=b5e8e961-1597-4d89-a09c-ed890dbb7045
[Plan] Tool=echo, Input=hi
[PreControl] Allowed
[Execution] Tool=echo, Result=hi
[PostControl] Allowed
[State] Version=1

agent> something

INPUT: something

[Step] StepId=97b645ac-76c7-460f-bada-d8898b6bb292
[Plan] Tool=something, Input=
[PreControl] Allowed
[Execution] Tool=something, Result=
[PostControl] Blocked: Unknown tool

agent> calc 10+20

INPUT: calc 10+20

[Step] StepId=81d9dc0f-80bd-4d8d-ba9c-a4387baafee3
[Plan] Tool=calc, Input=10+20
[PreControl] Allowed
[Execution] Tool=calc, Result=30
[PostControl] Allowed
[State] Version=2

agent> replay

INPUT: replay

[Step] StepId=2f93bc02-e53a-4016-ac8c-8ad104c49900
[Plan] Tool=replay, Input=
[PreControl] Allowed
[Execution] Tool=replay, Result=

==== REPLAY START ====

[Step] StepId=b5e8e961-1597-4d89-a09c-ed890dbb7045
[Plan] Tool=echo, Input=hi
[PreControl] Allowed
[Execution] Tool=echo, Result=hi
[PostControl] Allowed
[State] Version=1

[Step] StepId=97b645ac-76c7-460f-bada-d8898b6bb292
[Plan] Tool=something, Input=
[PreControl] Allowed
[Execution] Tool=something, Result=
[PostControl] Blocked: Unknown tool

[Step] StepId=81d9dc0f-80bd-4d8d-ba9c-a4387baafee3
[Plan] Tool=calc, Input=10+20
[PreControl] Allowed
[Execution] Tool=calc, Result=30
[PostControl] Allowed
[State] Version=2

[Step] StepId=2f93bc02-e53a-4016-ac8c-8ad104c49900
[Plan] Tool=replay, Input=
[PreControl] Allowed

==== REPLAY END ====

[PostControl] Allowed
[State] Version=3

agent> quit

F:\InvariantAgent2\src\InvariantAgent.Demo\bin\Debug\net8.0\InvariantAgent.Demo.exe (process 31076) exited with code 0 (0x0).

Press any key to close this window . . .
```
