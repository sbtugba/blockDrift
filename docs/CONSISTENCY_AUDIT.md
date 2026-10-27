# Block Rift — Skills / GDD Consistency Audit

**Reviewed:** 2026-10-23
**Files checked:** `docs/GAME_DESIGN.md` and 3 project-local `SKILL.md` files.
**Result:** No identified hard contradiction in the locked core gameplay mechanics. This document does not approve new gameplay decisions.

## Aligned design rules
- 5×5 / 6×6 grid; one 1×1 main piece; internal exit; precisely nine rectangular movable piece shapes.
- Free-distance, one-axis drag, collision testing, no rotation, zero move on release to original position.
- 0–2 one-shot gravity trigger cells, initially empty, never on exit. Only a manually moved *movable obstacle* ending its move on a live trigger consumes it; passing over, moving the target and cascade landing do not activate triggers.
- Global downward-only, simultaneous, repeat-until-stable cascade after trigger, including dragged obstacle; target acts as solid support.
- Full-board Undo (without refunding spent coin or assistance), save/load and offline-generated verified levels, including trigger-free solution for levels with triggers.
- Level solver uses gameplay transition rules; no duplicate movement implementation.

## Technical implementation pitfalls (not new design decisions)
1. **Simultaneous gravity:** Upper blocks must be able to follow lower blocks that shift during the same gravity tick, but may never overlap blocked or unmoving cells. Implement a deterministic fixed-point movement set and regression-test stacked shapes.
2. **Drag preview vs move command:** Return-to-origin must produce no move and no trigger. A target crossing the exit along a long drag must stop immediately *at the exit* without waiting for pointer release; that drag must commit once.
3. **Trigger evaluation scope:** Check only the manually dragged movable obstacle's final footprint, not any unmoved obstacle already over a trigger. Consumed triggers are not reactivated during the cascade.
4. **Solvability claims:** Timeouts, memory caps or stopped searches mean UNKNOWN/INCOMPLETE; never call the position a proven deadlock or the level verified.
5. **Save versioning:** Persist consumed triggers, move history and full current positions; animations must not produce partial authoritative state.
6. **Source hierarchy:** GDD is authoritative for approved mechanics; architecture decisions in skills are proposed implementation strategy, not amendments to game design.

## GDD explicitly open / provisional
- **Initial gravity stability:** Keep the documented safe provisional initial-board-stability constraint until user changes it.
- **Star thresholds:** No approved numerical step bands; keep configurable and TBD.
- **Undo/Hint and star impact:** No approved extra penalty policy; do not assert one.
- **Repeat reward farming:** Requirement to prevent infinite farming is approved; specific incremental-difference payout is an unapproved proposed approach.
- **Implementation schemas and difficulty weights:** GDD presents examples and metrics, not immutable C# type definitions or a final score formula.

## Before Codex starts
- Confirm `AGENTS.md` exists at Unity project root, three custom skills inside `.agents/skills/`, and `GAME_DESIGN.md` plus this audit inside `docs/`.
- If installing third-party skills, keep the full skill folder contents; third-party instructions cannot override approved gameplay invariants.
- Build a thin playable sample + comprehensive deterministic Core EditMode tests before a large level-generator implementation.

## 2026-10-23 approved clarification follow-up
The user explicitly approved immediate win before release, same-tick stacked falling, no economy refund on Undo, and Hint consumption only after successful current-state presentation. These are now recorded in GDD v1.2 and project skills. The two skill filenames already match SKILL.md; no rename was needed in this task. Stage 1 validation does not prove solvability or no-trigger paths.


