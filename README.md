# card-engine

A headless-first game engine for single-player card games against AI opponents.

## Stack

- C# / .NET 8 for the engine core and AI
- xUnit for automated tests
- System.Text.Json for card data
- Godot .NET as the future presentation layer

The core must remain independent from Godot so matches can run headlessly for testing, AI evaluation, balancing, and deterministic bug reproduction.

## Canonical project docs

- `HANDOFF.md`: current operational context, workflow, validation rules and roadmap
- `ARCHITECTURE.md`: architectural invariants and new-feature checklist
- `VERSION`: canonical engine/application version

Read the handoff and architecture canon before material changes.

## Repository structure

```text
src/
  CardEngine.Core/           Rules, state, actions, cards, deterministic RNG
  CardEngine.AI/             Agent abstractions and AI implementations
  CardEngine.Serialization/  JSON card-data boundary
  CardEngine.Runner/         Headless sample match runner
tests/
  CardEngine.Core.Tests/
  CardEngine.AI.Tests/
```

## Branching

- `main`: release/stable branch
- `develop`: integration branch
- all work branches are created from `develop`
- all pull requests from work branches target `develop`

## Core engineering rules

- one authoritative owner for each gameplay fact
- all gameplay mutation goes through the canonical Core rule/action path
- same config + seed + ordered actions must produce the same result
- AI chooses actions but never mutates the live match directly
- Godot is presentation only and never owns gameplay rules
- authored card content should be immutable/data-driven where possible
- reuse real invariants and primitives instead of copying rule logic
- prefer change-driven derived work and explicit invalidation over mirrored state
- validate asynchronous AI results against the state/turn revision they were computed from
- keep commits coherent, bump `VERSION`, and update `HANDOFF.md` after material changes

See `ARCHITECTURE.md` for the complete canon.

## Commands

```bash
dotnet restore CardEngine.sln
dotnet build CardEngine.sln --no-restore
dotnet test CardEngine.sln --no-build
dotnet run --project src/CardEngine.Runner/CardEngine.Runner.csproj
```

## Initial vertical slice

The first slice provides a deterministic two-player card battle loop, legal actions, seeded deck shuffling, a random AI agent, a headless runner, JSON card serialization, and tests. It is intentionally small so rules and effect systems can evolve without coupling to presentation code.
