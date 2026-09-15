## Summary

Describe the coherent change block and the gameplay/engine invariant it affects.

## Validation

- [ ] Branch was created from `develop`
- [ ] PR targets `develop`
- [ ] `dotnet build CardEngine.sln --configuration Release` passes
- [ ] `dotnet test CardEngine.sln --configuration Release` passes
- [ ] `VERSION` was bumped according to SemVer
- [ ] `HANDOFF.md` was updated if code architecture, roadmap, process or current project state changed
- [ ] New/changed gameplay behavior has regression coverage when it can be reproduced headlessly

## Architecture check

- [ ] Gameplay facts still have a single authoritative owner
- [ ] Core remains independent from Godot/UI and AI implementation details
- [ ] Gameplay randomness remains explicit and deterministic
- [ ] AI/UI use the canonical legal-action/mutation path instead of duplicating rule logic
- [ ] Any async result has a clear stale-result/revision policy
- [ ] No new abstraction exists only because two pieces of code look superficially similar

## Runtime evidence

If this changes Godot presentation or behavior that cannot be proven headlessly, describe the runtime evidence used to validate it. Otherwise write `N/A`.
