\# InvariantAgent



A reference implementation of the Invariant-Bounded Agent Alignment Model (IBAAM).



\## Key Idea



LLMs are treated as \*\*untrusted generators\*\*.



All actions must pass through a \*\*Control Gate\*\* enforcing invariants.



\## Architecture



\- Core: contracts + models

\- Control: invariant enforcement

\- Execution: LLM + tools

\- Adaptive: prompts + memory

\- Observability: logs + replay



\## Run



```bash

dotnet run --project src/InvariantAgent.Api

