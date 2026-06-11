# Level 1 Build Plan — "Fight for the Juice"

> Status: **APPROVED** — execution proceeds one phase at a time, validating in Play
> mode and committing between phases.

## Guiding principles (agreed)

- **Never edit package/demo files in place.** Everything authored lives in `Assets/Game`.
- **Build is self-contained; the repo is not.** Repo tracks only `Assets/Game` (+ this
  doc). Dependencies (CorgiEngine, TheMech, 2D_Pack, Feel, Libraries) stay untracked.
- **One level at a time.** Commit between phases so nothing is ever lost again.

## Inputs / source material

| Need | Source |
|---|---|
| Player | `Assets/Game/Prefabs/MechTrooperPlayer.prefab` (variant of Corgi `spine-space-cat`, art from `TheMech/trooper`) |
| NPC / enemy art | `Assets/TheMech/png/{gunner, walker, destroyer, mite}` |
| Layout blocks | `Assets/2D_Pack/!Prefabs/{Platforms, Borders, Panels, Pipes, Props, Doors, Elevators, Lights&Shadows}` |
| Layout reference | `Assets/CorgiEngine/Demos/Corgi2D/Lava.unity` |
| Rendering | `Assets/Game/Rendering/URP2D_PipelineAsset.asset` + `Renderer2D.asset` |
| Sound | MM built-ins + `Assets/Libraries/Soundbits_freeSFX_2025` |
| Effects | MM Feel / MMFeedbacks |

## Requirements (from brief)

- a) Player = **MechTrooper**.
- b) NPCs = other TheMech robots; replace NPCs **and the "Dude"** without losing ANY
  abilities/features (dialogue zone, etc.).
- c) Environment = URP enabled from the start: 2D lights, fog, particles — Lava as reference.
- d) Sound = MM predefined + `Assets/Libraries/...`.
- e) Effects = MM Feel or equivalent.
- f) Layout = mimic Lava but built from `Assets/2D_Pack` assets.
- g) Store only game-specific work in the repo; build is self-contained, work need not be.

## Phases

### Phase 0 — Baseline & safety
- Confirm URP 2D pipeline active (Graphics/Quality → `URP2D_PipelineAsset.asset`).
- Commit rollback point: `chore: pre-Level1 baseline`.

### Phase 1 — Clean scene skeleton
- Fresh `Assets/Game/Scenes/Level1.unity` (replaces corrupted scene).
- Core Corgi managers instanced fresh (GameManager, LevelManager, InputManager,
  SoundManagers, GUIManager).
- Main Camera + UICamera stack for URP 2D.
- `LevelManager.PlayerPrefabs` → **MechTrooperPlayer**.
- LevelStart + LevelBounds.
- **Validate:** MechTrooper spawns, moves, jumps, fires.

### Phase 2 — Layout (mimic Lava, from 2D_Pack)
- Re-skin approach was attempted and **rolled back** (visually unacceptable). Existing
  Lava platforms + their lights are retained as-is for now. Layout revisit deferred.
- **Validate:** full traversal start→gate possible (already validated in Phase 1).

### Phase 3 — NPCs / enemies (TheMech)
- 2–3 enemy prefabs in `Assets/Game/Prefabs/Enemies/` as Corgi `Character` variants
  (AI brain, Health, weapon, DamageOnTouch) using gunner/walker/destroyer/mite + new
  controllers in `Assets/Game/Animators`.
- Replace "Dude" NPC with a TheMech-based NPC, **preserving all abilities/features**.
- **Validate:** enemies patrol, attack, take/deal damage, die.

### Phase 4 — Environment treatment (Lava reference)
- URP 2D Global Light + point lights (reuse `FlickerLight`/`LightFlicker2D`/`OverheadLightFollow`).
- Particles: ember/spark/mist/ash (authored under `Assets/Game`).
- Ground fog, dark camera grade, pickup glows (`glow_radial.png`).
- **Validate:** atmospheric read, performance OK.

### Phase 5 — Sound (MM + Libraries)
- MMSoundManager + ambient bed; SFX (jump/shoot/hit/pickup/death).
- **Validate:** events trigger correct sounds.

### Phase 6 — Effects (Feel / MMFeedbacks)
- MMF_Player feedbacks for shoot, hit, death, pickup, level-complete.
- **Validate:** feedbacks fire.

### Phase 7 — Items & objectives
- Coins/pickups, health item, ability pickups; points + HUD (health bar, jetpack bar,
  coin counter).

### Phase 8 — Final validation & commit
- Full playthrough start→gate, capture Game view, `feat: Level1 complete`.

**Per-phase rule:** validate in Play mode + commit before moving on.
