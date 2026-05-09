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

agent> echo hello

[Step] StepId=8403be53-7a4e-480e-a91e-6d929678f86f
[Plan] Tool=echo, Input=hello
[PreControl] Allowed
[Execution] Tool=echo, Result=hello
[PostControl] Allowed
[State] Version=1

agent> delete all

[Step] StepId=d71181b3-db9e-4aec-82e0-458deb1aa646
[Plan] Tool=delete, Input=all
[PreControl] Blocked by NoDeleteInvariant: Delete operations are not allowed

agent> rm -rf

[Step] StepId=d88138ce-927f-4b9b-810c-b404318b5587
[Plan] Tool=rm, Input=-rf
[PreControl] Blocked by AllowedToolsInvariant: Tool 'rm' is not in allowed set

agent> calc 10+20

[Step] StepId=9ebecfdb-89e5-42ea-b71b-2d72f5423d6f
[Plan] Tool=calc, Input=10+20
[PreControl] Allowed
[Execution] Tool=calc, Result=30
[PostControl] Allowed
[State] Version=2

agent> replay

[Step] StepId=7733f60a-2879-4a22-a418-ae32caeb959e
[Plan] Tool=replay, Input=
[PreControl] Allowed
[Execution] Tool=replay, Result=
==== REPLAY START ====
[Step] StepId=8403be53-7a4e-480e-a91e-6d929678f86f
[Plan] Tool=echo, Input=hello
[PreControl] Allowed
[Execution] Tool=echo, Result=hello
[PostControl] Allowed
[State] Version=1
[Step] StepId=d71181b3-db9e-4aec-82e0-458deb1aa646
[Plan] Tool=delete, Input=all
[PreControl] Blocked by NoDeleteInvariant: Delete operations are not allowed
[Step] StepId=d88138ce-927f-4b9b-810c-b404318b5587
[Plan] Tool=rm, Input=-rf
[PreControl] Blocked by AllowedToolsInvariant: Tool 'rm' is not in allowed set
[Step] StepId=9ebecfdb-89e5-42ea-b71b-2d72f5423d6f
[Plan] Tool=calc, Input=10+20
[PreControl] Allowed
[Execution] Tool=calc, Result=30
[PostControl] Allowed
[State] Version=2
[Step] StepId=7733f60a-2879-4a22-a418-ae32caeb959e
[Plan] Tool=replay, Input=
[PreControl] Allowed
==== REPLAY END ====
[PostControl] Allowed
[State] Version=3

agent> quit

F:\InvariantAgent2\src\InvariantAgent.Demo\bin\Debug\net8.0\InvariantAgent.Demo.exe (process 5212) exited with code 0 (0x0).
Press any key to close this window . . .
```
