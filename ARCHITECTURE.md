# Card Engine Architecture Canon

This document is the canonical code architecture for `card-engine`. New code and refactors must preserve these boundaries unless this file is intentionally changed in the same coherent update.

The engine is headless-first, deterministic, data-driven, simulation-friendly, and presentation-agnostic. The main design goal is not merely clean code: it is to make game rules easy to reason about, test, reuse, simulate at scale, and integrate with Godot without coupling gameplay to rendering.

## 1. Architectural priorities

In order of importance:

1. Correctness of gameplay rules.
2. Deterministic and reproducible simulation.
3. Clear ownership and single responsibility.
4. Small, composable modules with narrow dependencies.
5. Reuse of real domain invariants instead of duplicated rule logic.
6. Testability without Godot.
7. Predictable performance under large headless simulation workloads.
8. Presentation quality and runtime integration.
9. Optimization complexity only when supported by evidence.

Performance work must not create a second source of truth or weaken determinism.

## 2. Project dependency graph

Dependencies point inward toward Core.

```text
CardEngine.Godot  ─┐
CardEngine.Runner ─┼──> CardEngine.AI ───────┐
                  ├──> CardEngine.Serialization ─┐
                  └───────────────────────────────┴──> CardEngine.Core
```

Rules:

- `CardEngine.Core` depends only on the .NET base/runtime libraries required by the domain.
- Core never references Godot, AI implementations, JSON DTOs, file-system concerns, UI, scenes, timers, frame lifecycle, or platform APIs.
- `CardEngine.AI` depends on Core and chooses actions; it never owns gameplay mutation.
- `CardEngine.Serialization` translates external data to validated Core definitions/configuration; serialized shape must not dictate runtime shape.
- `CardEngine.Runner` is composition and diagnostics for headless execution.
- Future `CardEngine.Godot` is an adapter/presentation project. It projects Core state and submits actions.
- Circular project references are forbidden.

## 3. Internal Core organization

Core is organized by domain responsibility, not by technical convenience.

Expected domains as the engine grows:

```text
CardEngine.Core/
  Actions/        commands/intents and action identity
  Cards/          card IDs, immutable definitions, catalog
  Effects/        reusable effect primitives and resolution
  Game/           match orchestration and lifecycle
  Rules/          legality, targeting, turn/phase rules
  State/          immutable external snapshots/read models
  Runtime/        internal mutable match-owned state
  Random/         deterministic random primitives
  Events/         immutable domain events/results
  Collections/    reusable collections justified by domain/perf needs
  Simulation/     cloning/replay primitives owned by Core, if needed
```

Folder names may evolve, but responsibilities must remain separated.

### 3.1 Orchestration is not domain ownership

A match/game object may coordinate a turn, but it must not become a giant class containing every rule.

As complexity grows:

- legal-action generation belongs in cohesive rule components;
- target validation belongs with targeting rules;
- card effect execution belongs in the effect resolver;
- turn/phase transitions belong in a turn/phase state machine;
- deck operations belong in a deck/runtime primitive;
- terminal/winner evaluation belongs in a terminal-condition rule;
- orchestration calls these components in a deterministic order.

A class that owns unrelated invariants should be split before new behavior is added to it.

## 4. Single responsibility

Each type should have one reason to change at the domain level.

Good examples:

- `CardCatalog`: owns immutable card definitions and ID lookup/validation.
- `DeckState`: owns deck ordering and draw/remove semantics.
- `TurnState`: owns active player, phase and turn progression.
- `ActionValidator`: determines whether a submitted action is valid for the current state.
- `EffectResolver`: resolves composable gameplay effects.
- `RandomSource`: provides deterministic random decisions.

Bad examples:

- a `GameManager` that loads JSON, mutates health, updates Godot nodes and chooses AI moves;
- a generic `Utils` class containing unrelated helpers;
- a shared mutable context bag passed everywhere because dependencies became inconvenient.

Private helper methods are not sufficient separation when they represent independent domain responsibilities.

## 5. Componentization and module boundaries

Prefer small components that expose a minimal, intention-revealing API.

- Keep implementation details `internal` unless another project truly needs the contract.
- Expose read-only interfaces/snapshots at boundaries.
- Prefer domain methods over exposing mutable collections.
- Group dependencies into a context only when they form one cohesive domain concept.
- Do not create service-locator-style contexts containing unrelated services.
- Keep composition roots (`Runner`, Godot bootstrap) responsible for wiring, not rules.
- Module entrypoints may orchestrate subcomponents, but each subcomponent owns its own invariant.

A component is justified when it owns state, an invariant, a lifecycle, or a reusable domain operation. Do not create components only to reduce line count.

## 6. Reuse policy: reuse invariants, not shapes

Code reuse is required when multiple mechanics share the same semantic rule.

Examples of valid reusable primitives:

- draw N cards;
- deal damage;
- heal;
- discard;
- move a card between zones;
- choose/validate a target;
- apply/remove a status;
- repeat/conditional effect composition;
- deterministic weighted/random choice;
- action legality predicates;
- deduplicated work queues where the semantics genuinely match.

Avoid abstractions created only because two methods currently look similar.

Before extracting shared code, ask:

1. Is the invariant the same?
2. Must both callers evolve together when that invariant changes?
3. Can the shared API have a domain name rather than a generic technical name?

If not, duplication may be safer than a false abstraction.

## 7. Strong domain identities

Important identities must not remain raw strings throughout runtime code.

Use value types such as:

- `CardId`
- future `EffectId`, `StatusId`, `PlayerId`, `ZoneId` where warranted

Rules:

- validate identity at construction/load boundaries;
- use ordinal/stable equality semantics;
- runtime state stores IDs/references to definitions instead of copying full immutable definitions;
- string formatting/parsing belongs at boundaries, not in hot gameplay logic;
- do not introduce ID types for ephemeral local variables that gain no safety or clarity.

## 8. Immutable definitions and authoritative registries

Authored content is immutable after successful load.

- `CardCatalog` is the authoritative owner of card definitions.
- Runtime deck/hand/discard state stores card identity and only genuine mutable instance state.
- Duplicate definition IDs are rejected.
- References between definitions are validated before a match starts.
- Range/required-field validation happens at the owning catalog/definition boundary.
- Derived immutable metadata is computed once by the owner where useful.
- Consumers query the catalog; they do not build private competing dictionaries.

Content loading follows this order:

```text
read external data
  -> deserialize DTOs
  -> construct domain definitions
  -> build registries/catalogs
  -> validate all local constraints
  -> validate cross-references globally
  -> expose immutable validated content
```

Invalid content should fail early with actionable diagnostics.

## 9. Authoritative runtime state

Every gameplay fact has one authoritative owner.

Examples include health, active player, turn, phase, deck order, hand contents, statuses, card ownership and terminal state.

- Do not mirror authoritative state for UI or AI convenience.
- Derived state is computed or cached with explicit invalidation.
- Mutable runtime collections stay encapsulated inside the owning runtime component.
- Public state is exposed through immutable/read-only snapshots or narrow queries.
- UI and AI never receive a mutable reference to the live match.

## 10. Actions are the mutation boundary

All player/AI gameplay mutation flows through actions/commands.

Canonical flow:

```text
intent/action
  -> verify revision/state identity
  -> validate legality
  -> resolve targets
  -> resolve effects/rules
  -> mutate authoritative runtime state
  -> advance state machine if required
  -> increment revision
  -> emit immutable result/events
```

Rules:

- legal-action enumeration and action application share the same underlying rule primitives;
- an action must be revalidated when applied even if it was previously legal;
- stale actions are rejected using state revision/turn identity when needed;
- Godot and AI use exactly the same mutation path;
- no UI shortcut or AI optimization may directly edit live state.

## 11. Effects as composable domain primitives

Cards are primarily data plus reusable effects, not one class per card.

Initial effect vocabulary is expected to include primitives such as:

- damage;
- heal;
- draw;
- discard;
- create/move card;
- stat modification;
- status application/removal;
- conditional;
- repeat;
- sequence;
- target selection;
- random choice.

Effects should be deterministic given state, inputs and random source.

A custom effect implementation is justified only when it introduces a genuinely new invariant that cannot be represented cleanly by existing primitives.

Presentation may animate effect results but may not reinterpret their gameplay outcome.

## 12. State machines over scattered booleans

Turn, phase and multi-step interactions should use explicit state machines rather than collections of loosely related booleans.

Prefer:

```text
TurnState
  Start
  Main
  Resolving
  End
  Finished
```

or game-specific equivalents.

Benefits required from the design:

- impossible combinations are structurally harder to represent;
- transition ownership is obvious;
- legal-action generation can depend on one canonical state;
- replays and debugging become easier.

## 13. Determinism

Determinism is a Core invariant.

- Gameplay randomness enters only through explicit deterministic RNG abstractions.
- Do not depend on `System.Random` implementation stability for replay contracts that may cross runtime versions.
- Prefer an engine-owned PRNG algorithm with explicit state when replay compatibility matters.
- Candidate ordering must be deterministic before random sampling.
- Do not depend on unspecified hash/dictionary enumeration order for gameplay decisions.
- Same validated configuration + same initial seed + same ordered actions must produce the same authoritative outcome.
- Deterministic helpers belong in one well-tested module rather than being reimplemented by systems.

## 14. Snapshots, revisions and read models

The authoritative match exposes a monotonically increasing revision.

- Increment revision after each successful authoritative mutation block.
- Snapshots include the revision they represent.
- Reuse a snapshot while authoritative inputs have not changed where this materially reduces allocations.
- AI decisions computed asynchronously carry the source revision.
- A decision whose revision no longer matches the live match is stale and must be discarded.
- Read models should expose only information allowed by the game mode; hidden information must not leak to agents.

## 15. AI architecture

`IAgent` is a strategy boundary.

- Agents consume immutable observation/state plus legal actions.
- Agents return an action; they do not apply it.
- Random/greedy/heuristic/search/MCTS agents share Core simulation primitives.
- No agent duplicates gameplay resolution logic.
- Search operates on isolated simulation state or clones, never the authoritative live match.
- Information visibility is controlled by the observation boundary, not by agent convention.

When heavy search becomes asynchronous:

- take an immutable/task-owned snapshot;
- bound concurrent work;
- attach revision/turn identity;
- apply only if still current.

## 16. Serialization architecture

Serialization is an adapter.

- DTOs may mirror JSON; domain models do not need to.
- Convert DTOs into validated domain types.
- Keep file paths, JSON naming and compatibility migrations outside Core.
- Cross-reference validation happens after the full content set is available.
- Persisted/replay formats receive explicit versions when compatibility becomes a product requirement.

## 17. Presentation/Godot architecture

Godot is a projection and input adapter.

Responsibilities:

- render current snapshots/events;
- collect player intent;
- translate intent into Core actions;
- play animations/audio/effects;
- manage scenes and presentation lifecycle.

Non-responsibilities:

- deciding legal moves;
- calculating damage;
- owning deck/hand truth;
- resolving effects;
- mutating Core collections;
- using frame order as a gameplay rule.

Presentation updates should be change-driven. If a snapshot revision did not change, avoid rebuilding the same view unnecessarily.

## 18. Performance architecture

The engine must support large batches of headless simulations. Performance is designed at ownership boundaries rather than patched with global shortcuts.

### 18.1 Measure the correct layer

Benchmark Core independently from Godot. Separate:

- rules/effect resolution throughput;
- snapshot creation cost;
- AI evaluation/search cost;
- serialization/loading cost;
- Godot rendering/presentation cost.

Do not optimize presentation to hide a slow Core algorithm.

### 18.2 Change-driven work

Do not recompute derived values when authoritative inputs are unchanged.

Use revisions/change identity for:

- snapshots;
- legal-action caches if profiling justifies them;
- AI observations;
- presentation projection;
- expensive derived metadata.

Cache only when ownership and invalidation are obvious.

### 18.3 Hot-path allocation discipline

For measured simulation hot paths:

- reuse stable immutable definitions;
- avoid copying full card definitions into runtime state;
- avoid unnecessary temporary collections;
- prefer compact value types for IDs/state where appropriate;
- pre-size collections when size is predictable;
- avoid LINQ or abstraction overhead only when profiling shows it is material;
- do not sacrifice readability in cold paths for hypothetical allocation savings.

### 18.4 Specialized collections

A custom collection is justified when it captures important semantics or eliminates measured cost.

Examples that may become useful:

- deduplicated queues for pending triggers/updates;
- stable priority queues for deterministic resolution;
- indexed registries for high-frequency definition lookup;
- reusable simulation buffers.

Such types must own their invariants, expose a narrow API and have focused tests.

### 18.5 Bounded asynchronous work

Async/background work must be bounded.

- Never spawn unbounded AI/search tasks.
- Use a clear maximum in-flight policy when concurrency is introduced.
- Task inputs are immutable snapshots.
- Task outputs carry revision identity.
- Cancellation/stale-result behavior is explicit.

### 18.6 Work budgets

Presentation-only expensive work may use time/item budgets to avoid frame spikes.

A work budget must not change authoritative gameplay ordering or outcomes. Headless simulation should remain independent of frame timing.

### 18.7 Cache lifecycle

Caches are derived state, not truth.

Every non-trivial cache must answer:

1. Who owns it?
2. What authoritative inputs form its key?
3. What invalidates it?
4. How is memory bounded/retained?
5. Is concurrent access required?
6. Can a stale entry affect gameplay correctness?

If these answers are unclear, do not add the cache.

## 19. Error handling and invariants

Differentiate expected invalid input from internal invariant failure.

- External content/config errors should produce explicit validation errors with IDs/paths/context.
- Illegal submitted gameplay actions should be rejected as domain errors/results, not silently ignored.
- Internal impossible states may use assertions/exceptions with precise invariant messages.
- Do not catch broad exceptions merely to continue with partially invalid gameplay state.
- Fail before starting a match when authored content is invalid.

## 20. Testing architecture

Tests are first-class architecture.

### Core unit/regression coverage

Prioritize:

- catalog validation and cross-reference failures;
- legal-action generation;
- illegal/stale action rejection;
- effect semantics and ordering;
- phase/turn transitions;
- terminal/winner rules;
- deterministic RNG vectors;
- same-seed replay;
- same action-log replay;
- snapshot/revision behavior;
- custom collection invariants.

### AI tests

- agent chooses only from supplied legal actions;
- deterministic agents reproduce decisions with equal inputs/seeds;
- search uses isolated state;
- hidden information is not exposed.

### Integration tests

Use headless composition to exercise complete matches without Godot.

A gameplay bug that can be reproduced headlessly should gain a regression test.

## 21. API and visibility discipline

Public API surface is a maintenance cost.

- default to `internal` for implementation types;
- expose only contracts required by another project/consumer;
- prefer immutable records/value types for boundary data;
- avoid public setters on authoritative state;
- avoid returning mutable concrete collections;
- do not expose implementation-specific infrastructure through Core APIs.

## 22. Naming and code organization

Names should communicate domain responsibility.

Prefer:

- `CardCatalog`, `EffectResolver`, `TurnState`, `LegalActionGenerator`, `DeckState`;

Avoid vague names such as:

- `Manager`, `Helper`, `Utils`, `Common`, `Processor`, `Service` unless the domain responsibility is otherwise explicit.

A source file should normally contain one primary type or one tightly cohesive family of tiny related types.

Large source files are a signal to review responsibility boundaries, not an automatic refactor trigger.

## 23. Refactoring triggers

Refactor before extending when one of these becomes true:

- a type owns multiple unrelated invariants;
- the same rule is implemented in more than one layer;
- callers need broad mutable state because APIs are too weak;
- a method requires many unrelated dependencies;
- a cache cannot explain invalidation;
- AI needs to know internal mutation details;
- Godot code starts reproducing Core rules;
- new effects require editing an ever-growing central conditional for unrelated mechanics;
- performance fixes require touching unrelated systems because ownership is unclear.

Do not refactor only because of aesthetic line-count thresholds.

## 24. Decision checklist for new features

Before adding a mechanic/system/helper:

1. What gameplay fact or invariant does it own?
2. Is there already an owner for that invariant?
3. Which project/domain should contain it?
4. Can it be a pure/value-oriented component?
5. Am I reusing a real invariant or abstracting superficial similarity?
6. Does mutation flow through the canonical action path?
7. Is authored data immutable and validated before runtime?
8. Are important IDs strongly typed where useful?
9. Is deterministic ordering/randomness preserved?
10. Does public state remain read-only/immutable?
11. Does async work use snapshots, revisions and bounded concurrency?
12. Is derived work change-driven or cacheable with explicit invalidation?
13. Will this create allocations or repeated work in a measured hot path?
14. Does the abstraction reduce coupling rather than merely move code?
15. Which focused tests prove the invariant?

## 25. Current implementation direction

The scaffold is intentionally small. The next code evolution should move toward:

```text
CardCatalog + CardId
        |
        v
validated GameDefinition/GameConfig
        |
        v
Match runtime aggregate
  |- Turn/phase state
  |- Player/deck/zone runtime state
  |- Legal-action rules
  |- Effect resolver
  |- deterministic RNG
        |
        +--> immutable GameSnapshot + revision
        |         |
        |         +--> AI observation
        |         +--> Godot projection
        |
        +--> domain events / replay log
```

The current monolithic parts of the scaffold are allowed only as bootstrap code. New gameplay complexity should be added by extracting cohesive owners rather than extending one central `Game` class indefinitely.

## 26. Versioning and architecture changes

The root `VERSION` is canonical.

- Patch: compatible fix/refactor/tooling/documentation architecture clarification.
- Minor: backward-compatible engine capability.
- Major: incompatible public contract/serialization/architecture break.

If a change intentionally violates or replaces a rule in this document, update `ARCHITECTURE.md` in the same coherent commit and explain why in the PR.

When in doubt: keep one owner, one canonical rule path, narrow dependencies, immutable boundaries, deterministic ordering and small components. Optimize only the layer that owns the measured cost.
