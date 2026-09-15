# Card Engine Architecture Canon

This document defines the architectural rules that new code and refactors in `card-engine` must preserve. Prefer extending an existing owner or primitive over creating a second path for the same gameplay fact.

## 1. Single authoritative owner

A gameplay fact must have one authoritative owner.

- Do not mirror game state, turn state, legal-action state, card ownership, deck contents, health, status effects, or target selection into a second persistent model merely to simplify another layer.
- Derived read models may combine authoritative sources, but they must not become a second source of truth.
- If a value can be recomputed cheaply and deterministically, recompute it or cache it locally with explicit invalidation instead of persisting a duplicate.
- The runtime game aggregate owns mutation. Presentation and AI consume snapshots/intents and never mutate authoritative collections directly.

## 2. Determinism is a core invariant

The engine is designed for reproducible single-player matches and large headless simulations.

- Gameplay randomness must enter through an explicit random-source abstraction.
- Do not use ambient/global randomness for gameplay decisions.
- Same configuration + same seed + same ordered actions must produce the same authoritative outcome.
- Random choices that affect gameplay must have a deterministic ordering of candidates before sampling.
- Simulation/replay tests should assert deterministic outcomes whenever a bug depends on randomness.

## 3. Dependency direction

Dependencies point toward the core.

- `CardEngine.Core` contains game rules, state transitions, legal actions, card/effect semantics and deterministic primitives.
- `CardEngine.AI` depends on Core and chooses actions; Core never depends on AI.
- `CardEngine.Serialization` may depend on Core contracts to load/save content, but Core must not depend on serialization infrastructure.
- `CardEngine.Runner` composes Core, AI and serialization for headless execution.
- The future Godot integration may depend on Core/AI/Serialization, but Core must never reference Godot types, nodes, scenes, resources or frame lifecycle.
- Avoid circular ownership between gameplay, UI, serialization and AI.

## 4. Actions are the mutation boundary

Gameplay changes should flow through explicit actions/commands and authoritative rule evaluation.

- The core exposes legal actions for the current authoritative state.
- Agents and UI choose/submit an action; they do not execute rule mutations themselves.
- Applying an action must validate legality against the current state, even if it came from a previously generated legal-action list.
- Prefer one canonical rule path for human, AI and automated simulation players.
- Do not add presentation-only shortcuts that bypass action legality.

## 5. Immutable definitions vs runtime state

Keep authored content separate from mutable match state.

- Card definitions, effect definitions and catalog metadata are immutable after validated load whenever possible.
- Runtime card instances may reference definition IDs and store only mutable per-instance state that genuinely exists in gameplay.
- Do not copy full definitions into every runtime object unless profiling proves that indirection is a problem.
- Derived immutable metadata should be computed by the catalog/definition owner at load time instead of rediscovered in simulation hot paths.
- Replacing a definition ID must rebuild its derived metadata from the new authoritative definition.

## 6. Effects are composable primitives

Shared card behavior should be represented by composable effect semantics rather than one custom class per card.

- Examples include damage, heal, draw, discard, create-card, stat modification, status application, conditional execution, repetition and target selection.
- A custom implementation is justified when it represents a genuinely new gameplay invariant, not merely a different parameter set.
- Effects own gameplay semantics; presentation may visualize an effect result but may not reinterpret its rule outcome.
- Reuse a lower-level primitive when multiple mechanics share the same invariant. Do not merge mechanics only because their code looks similar.

## 7. AI boundary and information policy

`IAgent` is an action-selection boundary, not a privileged mutation API.

- Agents receive only the information the engine intentionally exposes for the game mode.
- Hidden information must not leak through shared mutable runtime objects.
- An agent returns a legal action candidate; the authoritative game applies it.
- Search agents should operate on isolated snapshots/clones or purpose-built simulation state, never on the live authoritative match.
- Heuristics, search and Monte Carlo implementations should share real simulation/evaluation primitives rather than copy rule logic.

## 8. Async and stale-result safety

Heavy AI/search work may run outside the presentation thread when its inputs are immutable.

- Background work must operate on immutable snapshots or task-owned simulation state.
- Inputs that may change while work is in flight should carry a revision, turn number or equivalent identity.
- A completed asynchronous decision is valid only for the authoritative state revision it was computed from.
- Stale decisions must be discarded and recomputed if still needed.
- Do not let background work mutate the live match, Godot scene tree or UI state.

## 9. Presentation is a projection

Godot is a presentation/integration layer.

- UI derives from authoritative game state and domain events/snapshots.
- Shared UI behavior belongs in the Godot/UI layer rather than being duplicated across screens.
- Prefer change-driven presentation updates instead of rebuilding labels, cards, materials or scenes every frame when inputs did not change.
- Animation state may lag gameplay state visually, but it must never become the rule owner.
- Visual/gameplay fixes that depend on Godot runtime behavior require runtime evidence before being declared resolved.

## 10. State snapshots and derived views

Snapshots should make ownership obvious.

- Public game state exposed to AI/UI should not grant uncontrolled mutation of authoritative collections.
- Derived views should be small and domain-specific rather than one generic context containing everything.
- As dependency lists grow, review cohesion before creating a broad `GameContext`/service-locator style bag.
- Prefer explicit domain contexts and composition over hidden global access.

## 11. Performance expectations

Correctness and determinism come first, but the engine must support high-volume headless simulation.

- Avoid repeated work when authoritative inputs have not changed.
- Prefer change-driven updates, stable caches and precomputed immutable metadata where the lifecycle is clear.
- Avoid unnecessary large state copies or temporary allocations in measured hot paths.
- Do not introduce object pools, unsafe code, custom allocators or complex caches without evidence that they address a real bottleneck.
- Batch simulations should be benchmarkable independently from Godot.
- Optimize the smallest authoritative layer that owns the cost rather than hiding it behind presentation shortcuts.

## 12. Serialization boundary

Serialization represents content and persistence, not gameplay ownership.

- Validate IDs, required fields, ranges and references when loading content.
- Invalid content should fail with actionable diagnostics before a match begins when possible.
- Serialization DTO shape should not force the internal runtime state model to mirror JSON structure.
- Version serialized formats when backward compatibility becomes a product requirement.

## 13. Testing expectations

Tests are part of the architecture because the engine is headless-first.

Core regression coverage should prioritize:

- legal-action generation;
- rejection of illegal/stale actions;
- deterministic seed behavior;
- deterministic replay of an ordered action sequence;
- terminal/winner conditions;
- effect semantics and ordering;
- card catalog validation;
- AI contract behavior without duplicating game rules.

A bug fix should add or strengthen a regression test when the failing behavior can be reproduced headlessly.

## 14. Versioning and commits

The root `VERSION` file is the canonical application/engine version.

- Every completed coherent update block bumps `VERSION` using semantic versioning.
- Patch: compatible bug fix, refactor, documentation/process or tooling block.
- Minor: backward-compatible engine/gameplay feature block.
- Major: incompatible public contract, serialization or architecture change.
- Keep commits coherent enough that architecture/process changes can be reviewed and reverted independently.
- Work branches start from `develop` and pull requests target `develop`.

## 15. New-feature checklist

Before adding a new mechanic, system or helper, answer:

1. What is the single authoritative owner of the gameplay fact?
2. Does this belong in Core, AI, Serialization, Runner or presentation?
3. Am I reusing a real invariant/state machine, or only code with a similar shape?
4. Does the feature preserve deterministic seed + action replay?
5. Does it mutate through the canonical action/rule path?
6. Is authored data immutable and validated at load time where possible?
7. Can derived work be change-driven or precomputed instead of repeated?
8. If work is asynchronous, how is stale-result validity checked?
9. Does AI receive only intended information and avoid mutating live state?
10. Is a regression/unit test appropriate for this behavior?
11. Has `VERSION` been bumped for the coherent update block?
12. Has `HANDOFF.md` been updated if architecture, roadmap, process or current state changed?

When in doubt, prefer one authoritative owner, deterministic transitions and small domain primitives over mirrored state, copied rule logic or oversized contexts.
