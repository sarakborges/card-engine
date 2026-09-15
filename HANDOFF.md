# HANDOFF — card-engine

Repo: `sarakborges/card-engine`  
Integration branch: `develop`  
Stack: C# / .NET 8; Godot is a future consuming presentation layer

## Product direction

`card-engine` is a reusable library/platform for deterministic card games, not a specific game.

The library owns card-game mechanics and runtime invariants. Consuming projects own authored content and presentation. Heroes, hero powers, cards, card-type definitions, decks and tuning/ruleset values are supplied by the host project as data and validated before a match starts.

Canonical platform contract: `PLATFORM_ARCHITECTURE.md`.  
Canonical code/engineering rules: `ARCHITECTURE.md`.

## Package boundaries

- `CardEngine.Core`: required reusable platform package.
- `CardEngine.Serialization`: optional JSON data adapter.
- `CardEngine.AI`: optional AI package.
- `CardEngine.Runner`: repository-only headless example.

Core must not depend on AI, Serialization, Runner, Godot, UI or a specific card game.

## Current platform model

The platform now models:

- typed IDs for cards, card types, heroes and hero powers;
- deterministic per-match `CardInstanceId` values;
- immutable validated `GameContent` catalogs;
- host-provided `MatchRules` and `MatchSetup`;
- two-player authoritative matches;
- hero health and data-driven hero-power references;
- deck, hand, board and discard zones;
- data-driven card types that determine post-play destination;
- composable effect definitions with initial damage/heal primitives;
- legal actions for card play, hero power and end turn;
- revision-checked mutation to reject stale decisions;
- immutable `MatchState` snapshots;
- player-scoped `MatchView` that hides opponent hand contents;
- deterministic library-owned RNG;
- optional AI agents over the player-view/action boundary.

## JSON content layout

`CardEngine.Serialization` uses a per-entity filesystem layout. A consuming project chooses the content root (recommended: `data/`) and loads it with `GameDataDirectoryLoader.Load(root)`.

Canonical layout:

```text
data/
  rules.json
  card-types/
    {id}.json
  cards/
    {id}.json
  heroes/
    {id}.json
  hero-powers/
    {id}.json
```

Rules:

- each entity JSON contains exactly one definition;
- the entity ID remains explicit inside the JSON;
- `{id}.json` must match the declared `id` exactly using ordinal comparison;
- files are loaded in deterministic ordinal filename order;
- cross-reference validation happens only after all definitions are composed into `GameContent`;
- Core remains unaware of filesystem and JSON concerns.

## Next platform work

Prioritize primitives required by the first consuming game rather than speculative generality. Expected next candidates:

1. target model and target legality;
2. draw/discard/summon/destroy effects;
3. per-instance card state;
4. status/modifier model;
5. generic resource/cost mechanics;
6. explicit turn phases;
7. deterministic event/replay log;
8. victory/terminal policy extension;
9. batch simulation and AI search snapshots.

## Development rules

- Work branches originate from `develop`.
- Pull requests target `develop`.
- One authoritative owner per gameplay fact.
- Consumer customization is data-first; do not expose writable runtime collections.
- Reuse invariants, not superficial code similarity.
- Core remains headless and framework-free.
- Gameplay randomness and ordering remain deterministic.
- Background AI/search uses immutable/task-owned state and revision validation.
- Performance optimization is driven by measured headless simulation costs.
- Tests protect content validation, zone semantics, legal actions, deterministic replay and hidden-information boundaries.

## Validation

CI restores, builds and tests the full .NET 8 solution. Warnings are errors.

`VERSION`: `0.2.1`

Current work branch: `chore/initial-scaffold`  
Current PR: `#1` -> `develop`
