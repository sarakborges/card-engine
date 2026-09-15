# card-engine

A headless-first game engine for single-player card games against AI opponents.

## Stack

- C# / .NET 8 for the engine core and AI
- xUnit for automated tests
- System.Text.Json for card data
- Godot .NET as the future presentation layer

The core must remain independent from Godot so matches can run headlessly for testing, AI evaluation, balancing, and deterministic bug reproduction.

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

## Commands

```bash
dotnet restore CardEngine.sln
dotnet build CardEngine.sln --no-restore
dotnet test CardEngine.sln --no-build
dotnet run --project src/CardEngine.Runner/CardEngine.Runner.csproj
```

## Initial vertical slice

The first slice provides a deterministic two-player card battle loop, legal actions, seeded deck shuffling, a random AI agent, a headless runner, JSON card serialization, and tests. It is intentionally small so rules and effect systems can evolve without coupling to presentation code.
