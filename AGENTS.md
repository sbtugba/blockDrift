# Block Rift — Codex Project Instructions

## Project identity
- Project: **Block Rift** (former working name: Gravity Gate).
- Unity 6, C#, URP, portrait mobile, 2.5D presentation; gameplay is a deterministic grid simulation, not Unity physics.
- Primary source of **approved mechanics**: `docs/GAME_DESIGN.md`.

## Instruction hierarchy and routing
1. First read this `AGENTS.md` and the relevant sections of `docs/GAME_DESIGN.md` before changing gameplay.
2. Apply `.agents/skills/block-rift-architecture/SKILL.md` for C# architecture, class layout, responsibilities and dependencies.
3. Apply `.agents/skills/block-rift-gameplay/SKILL.md` whenever touching board rules, movement, collisions, triggers, cascade, Undo, Hint, win/deadlock, and gameplay state.
4. Additionally apply `.agents/skills/block-rift-level-generator/SKILL.md` for level generation, solver, difficulty metrics, replay, hints or no-trigger proofs.
5. Generic third-party Unity or puzzle skills are **supporting references only**; if they conflict with approved Block Rift gameplay, follow the GDD and report the conflict. Special skills must not silently reinterpret GDD.
6. `docs/CONSISTENCY_AUDIT.md` lists known technical risks and unresolved decisions. Its recommendations are **not** approved game-design rules.

## Non-negotiable gameplay invariants
- Board only 5x5 or 6x6; one 1x1 target; exit within board; exactly nine permitted movable rectangular shapes (see GDD).
- Drag one axis, no rotation; any traversable distance counts as one move. Return to origin at release => NO_OP.
- Gravity **only** after a movable obstacle ends its manual move covering an unused trigger; 0–2 single-use triggers per level. No automatic gravity after ordinary moves and no new trigger activation during cascade.
- Gravity moves only obstacles downward; target and fixed geometry never fall. Simultaneous cascade must be deterministic and collision-safe.
- Target touching exit completes immediately. Undo restores entire pre-move board and trigger state. All levels with triggers have at least one no-trigger winning solution.
- Level generation offline, exact validation; published levels precomputed and identical for all players.

## Implementation contract
- Before coding: inspect existing files, state relevant GDD sections, outline affected invariants and smallest reasonable change.
- **Core** rules must be pure C#, with no `UnityEngine`, scene or animation dependence. Presentation must display state, not mutate authoritative rules.
- Solver, Hint, deadlock detection and generator must use the same Core move transition function as live play. Do not duplicate collision/gravity code.
- Test deterministic movement, trigger activation, simultaneous cascade, no-trigger path, Win, Undo, serialization and exact solver replay.
- Prefer small cohesive types, explicit dependencies, versioned level/save data and Turkish explanatory comments on classes, public methods and non-obvious mechanics.
- Never invent approval for unclear design decisions. Mark as `TBD` or suggest a non-binding implementation option. Specifically unresolved: exact star thresholds; Hint/Undo penalty on stars; precise anti-farming payout policy.
- When computation is incomplete, report `INCOMPLETE`, never claim proven solvability or deadlock.
- Do not edit `docs/GAME_DESIGN.md` to fit generated code. Propose rules changes separately and await explicit approval.

## Done means verified
For each implementation task report: modified files, related GDD rules, key design choices, actual executed test commands, pass/fail results and remaining uncertainties. Never claim a test was run if it was not executed. Do not commit, deploy or publish without an explicit instruction.

## User-approved clarifications — 2026-10-23
- Win commits on target reaching exit during drag; never wait for pointer release.
- Stacked obstacles that can fall together move in the same tick, using cells vacated in that tick.
- Undo restores board/trigger/history state only; spent coins and consumed assistance are never refunded.
- Consume Hint only after successfully presenting a verified hint for the current board; failed/incomplete searches do not consume it.

