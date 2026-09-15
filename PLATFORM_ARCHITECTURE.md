# CardEngine Platform Architecture

`card-engine` is a reusable, headless platform library for building deterministic card games. It must not contain the authored content of a particular game.

## Ownership boundary

The library owns **mechanics and runtime invariants**. A consuming game owns **authored content and game-specific presentation**.

The library owns:

- match lifecycle and authoritative state transitions;
- turns and turn revisions;
- player runtime state;
- heroes and hero-power runtime behavior;
- deck, hand, board and discard zones;
- card instances and stable instance identity;
- card-type mechanical behavior;
- legal actions and action validation;
- effect execution primitives;
- deterministic randomization;
- state snapshots and player information views;
- content validation;
- optional AI contracts and implementations;
- optional serialization adapters.

The consuming project owns:

- which heroes exist;
- which hero powers exist;
- which cards exist;
- which card types exist;
- decks and game content files;
- numeric tuning and ruleset values;
- localization, artwork, animation and audio;
- game-specific UI/Godot scenes;
- game-specific AI strategy when the stock agents are insufficient.

## Data-driven contract

Authored definitions enter the engine through `GameContent`. `GameContent` is immutable after construction and is the authoritative catalog for a match.

Definitions are referenced through typed stable IDs:

- `CardId`;
- `CardTypeId`;
- `HeroId`;
- `HeroPowerId`.

A consuming project may create `GameContent` from JSON, a database, Godot resources, generated code, a remote service, or any other source. Core must not depend on the source format.

`CardEngine.Serialization` is an optional JSON adapter, not the owner of content semantics.

## Definitions vs instances

A card definition describes authored behavior. A card instance represents one concrete copy inside a match.

Every runtime copy has a deterministic `CardInstanceId`. Runtime zones contain instances, not copied definitions. Definitions are resolved through the catalog by `CardId`.

This permits duplicate cards, future per-instance state, replay, targeting and efficient snapshots without duplicating immutable authored data.

## Zone ownership

The match runtime owns all card movement between zones:

`Deck -> Hand -> Board/Discard`

Consumers may request actions but cannot mutate zone collections directly.

Additional zones and movement rules should be added as cohesive engine concepts when a real game requires them. Do not expose writable lists as an extension mechanism.

## Card types

Card types are data-driven definitions, not a compile-time enum of game-specific names.

The engine owns the finite set of **mechanical capabilities** a type can configure. For example, the current platform lets a type declare whether a played card persists on the board or moves to discard.

A game may define IDs such as `spell`, `unit`, `skill`, `equipment`, or anything else without changing Core, provided the desired behavior can be expressed by engine mechanics.

When a new game requires a genuinely new mechanical invariant, add that invariant to the platform rather than hardcoding the game's type name.

## Effects

Cards and hero powers are compositions of effect definitions. The engine executes effect semantics; content only supplies effect type and parameters.

Initial primitives:

- damage opponent hero;
- heal friendly hero.

Future primitives should remain small and composable: draw, discard, summon, destroy, modify stat, apply status, create card, conditional, repeat, choose target, random choice, etc.

Do not create one engine class per authored card.

## Match setup and rules

A match is created from:

- immutable `GameContent`;
- `MatchSetup` supplied by the host;
- host-supplied `MatchRules`;
- deterministic seed.

`MatchSetup` references hero/card IDs and is validated before runtime state is created.

Rules such as starting hand, maximum hand, board size and turn limit are host-configurable data. Rules remain engine-owned semantics even though values come from the host.

## Actions are the mutation API

All authoritative gameplay mutation flows through `MatchAction`.

Current platform actions:

- `PlayCardAction`;
- `UseHeroPowerAction`;
- `EndTurnAction`.

UI, AI, tests and headless simulations all use the same legal-action and apply path.

Actions may carry an expected state revision. A stale action must be rejected rather than applied to a different authoritative state.

## State and information views

`MatchState` is an immutable snapshot of authoritative match state.

`MatchView` is a player-scoped information projection. It deliberately hides the opponent's hand contents while retaining public information and counts.

AI agents consume player views rather than mutable runtime objects. Future hidden-information rules must be expressed in the view boundary, not delegated to agent goodwill.

## Determinism

Match randomness is provided by a library-owned deterministic algorithm, not `System.Random` or ambient global randomness.

Same content + setup + seed + ordered actions must produce the same authoritative result for a given engine version.

Candidate ordering must be explicit whenever ordering influences a random or rule outcome.

## Package boundaries

The repository may publish multiple packages because their dependency/lifecycle responsibilities differ:

- `CardEngine.Core`: required platform library;
- `CardEngine.Serialization`: optional JSON adapter depending on Core;
- `CardEngine.AI`: optional agents depending on Core.

`CardEngine.Runner` is a development/sample executable and is never a package dependency of consuming games.

Core must never depend on Serialization, AI, Runner, Godot or a particular game project.

## Public API policy

Public API is a compatibility commitment. Expose only concepts a consuming game needs to configure, inspect, or invoke.

Runtime implementation classes, mutable collections and internal coordination remain private/internal.

Prefer stable typed IDs, immutable definitions, snapshots and actions as the public surface.

## Extension policy

A consumer customizes a game primarily by **data**, not by subclassing engine internals.

Add extension interfaces only when multiple implementations genuinely need host-provided code. An extension must have:

- a narrow responsibility;
- deterministic semantics when it affects gameplay;
- an explicit state/ownership contract;
- no direct mutation access to unrelated match internals.

Avoid generic callback bags or service locators as customization mechanisms.

## Performance model

The primary performance workload is high-volume headless simulation. Therefore:

- definitions are stored once in immutable catalogs;
- runtime instances reference definitions by ID;
- state snapshots contain compact runtime facts;
- revisions enable change-driven consumers and stale-result rejection;
- Godot/frame lifecycle never participates in authoritative simulation;
- hot-path optimizations must be measured and remain inside the owner of the cost.

When search AI needs faster cloning, introduce a purpose-built simulation state representation rather than allowing it to mutate the live match.

## Current scope and next platform primitives

The initial vertical slice intentionally proves the library boundary with two-player matches, heroes, hero powers, deck/hand/board/discard, data-driven card types, effects, legal actions, deterministic RNG and player-scoped views.

Likely next platform primitives, driven by actual consumer requirements:

1. generic target selection and target legality;
2. draw/discard/summon/destroy effects;
3. per-card-instance mutable state;
4. status/modifier system;
5. resource/cost system without hardcoding a specific game's mana model;
6. explicit turn phases and phase hooks;
7. deterministic event/replay log;
8. extensible terminal/victory rules;
9. batch simulation APIs;
10. search-oriented snapshot/clone representation.
