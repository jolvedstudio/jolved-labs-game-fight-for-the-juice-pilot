# Corgi Engine + 2D_Pack — Platformer Level Generator (Design Outline)

> Status: **design only, no code.** This document outlines a procedural/assisted
> level generator built from the proof-of-concept level created in
> `Assets/_CorgiPlayground/Scenes/CorgiPlayground_Level1.unity`.

---

## 1. What the proof-of-concept established

The hand-built (script-built) level proved the minimum viable recipe for a
**fully functional Corgi Engine level** assembled from **2D_Pack art**:

| Concern | Solution found |
|---|---|
| Managers | Instantiate `GameManagers`, `MMSoundManager`, `UICamera`, `MinimalCameraRig` prefabs from CorgiEngine/Common + Demos/Minimal |
| Spawn | `LevelStart` prefab (contains a `CheckPoint`) wired into `LevelManager.DebugSpawn` |
| Player | `Rectangle.prefab` `Character` assigned to `LevelManager.PlayerPrefabs[]` |
| Collision | 2D_Pack platform prefabs already carry an **EdgeCollider2D** on their top surface; they only needed to be moved to the **`Platforms` layer (8)** |
| World limits | `LevelManager.LevelBounds` computed from the union of placed renderers, padded for headroom + a fall-death floor |
| Surface alignment | Each platform's walkable Y = `transform.y + edgeCollider.offset.y + max(point.y)`. We place "by surface height" and back-solve the pivot position |

### Key 2D_Pack platform metrics (PPU 100)

| Prefab | Sprite size | Top-surface offset (local Y) | Collider |
|---|---|---|---|
| `Platform_1..5` | 5.12 × 5.12 | +2.57 | flat edge, ±2.56 wide |
| `Platform_7 / _8` | 5.12 × 2.56 | +1.28 | flat edge |
| `Platform_6 / _9` | 5.12 × 1.28 | +0.64 | flat edge (thin) |
| `Platform_10` | 2.56 × 2.56 | +1.28 | flat edge, ±1.28 |
| `Platform_End_1` | 3.18 × 2.01 | +1.01 | flat edge, ±1.59 |
| `Platform_Ramp_45` | 5.12 × 5.12 | diagonal | edge (-2.55,-2.56)->(2.56,2.56) |
| `Platform_Ramp_30` | 10.24 × 5.12 | diagonal+flat | 3-point edge |

These metrics are the generator's "tile palette" — fixed, known dimensions the
generator can reason about geometrically.

---

## 2. Generator goals

1. Produce **traversable** levels: every gap is reachable given the player's jump
   arc; every required surface is on the `Platforms` layer with a valid collider.
2. Reuse **only** 2D_Pack art + Corgi managers (no new art required).
3. Be **deterministic** (seeded) so a seed reproduces a level — essential for
   testing and sharing.
4. Output a **saved scene** + optional prefab, never overwriting library assets
   (write under `Assets/_CorgiPlayground/...`).

---

## 3. Architecture (conceptual modules)

```
LevelGenerator (orchestrator)
 ├─ GenerationConfig         (seed, length, difficulty curve, theme)
 ├─ ReachabilityModel        (player jump height/width -> max gap & step rules)
 ├─ TilePalette              (2D_Pack prefab metadata: size, surface offset, role)
 ├─ ChunkLibrary             (parameterized building blocks: Run, Gap, Stairs, Ramp, Pit, Ledge)
 ├─ LayoutPlanner            (sequences chunks along an X axis using a grammar)
 ├─ SceneAssembler           (instantiates prefabs, sets layer, aligns by surface)
 ├─ ManagerInjector          (adds GameManagers/UICamera/CameraRig/LevelManager/LevelStart)
 ├─ BoundsBuilder            (computes LevelBounds from placed renderers + fall floor)
 └─ Validator                (auto-playtest / reachability assertions)
```

### 3.1 ReachabilityModel (the math that keeps levels playable)
Derived from the `CharacterJump`/`CharacterHorizontalMovement` abilities on the
player prefab:
- `maxJumpHeight` (units) from jump force & gravity
- `maxJumpDistance` (units) from run speed × air time
Generator constraints:
- vertical step between consecutive surfaces ≤ `maxJumpHeight − safetyMargin`
- horizontal gap ≤ `maxJumpDistance − safetyMargin`
- never require a jump that also needs a ceiling the player can't clear

### 3.2 TilePalette
A data table (ScriptableObject) mapping each 2D_Pack platform prefab to:
`{ prefabPath, footprintWidth, surfaceY, role(GROUND|THIN|RAMP|END|DECOR) }`.
This is exactly the table in §1 above. Adding new art = adding rows, no code.

### 3.3 ChunkLibrary (vocabulary of the level grammar)
Each chunk is a function `(cursor, rng, difficulty) -> placements + new cursor`:
- **GroundRun(n)** — n ground tiles at current height (Platform_1..5)
- **Gap(width)** — advance cursor X, place nothing (creates a jump)
- **FloatingSteps(k)** — k thin platforms (Platform_6/9) ascending within jump limits
- **RampUp / RampDown** — Platform_Ramp_45/30 changing the height cursor
- **Stairs** — staggered Platform_10 blocks
- **Pit(width)** — gap with a fall-death (bounds floor) — only if a safe route exists
- **Ledge(height,n)** — raised run reached by prior ramp/steps
- **EndCap** — Platform_End_1 to visually terminate a section

### 3.4 LayoutPlanner (grammar + difficulty curve)
A weighted grammar walks left→right, picking chunks whose difficulty matches a
ramping curve (easy intro → harder middle → resolve before the goal). Rules:
- always start with `GroundRun` under the spawn
- never place two max-difficulty gaps back-to-back
- guarantee at least one valid path (the planner tracks the "height cursor" and
  only emits chunks the ReachabilityModel approves)
- end with a `GroundRun + EndCap` and a `GateToNextLevel`/finish checkpoint

### 3.5 SceneAssembler
Instantiates each placement via `PrefabUtility.InstantiatePrefab`, sets the
`Platforms` layer recursively, and aligns using the **surface-offset formula**
(`pivotY = desiredSurfaceY − surfaceOffset`). Groups objects under
`Level/Platforms` for clean hierarchy.

### 3.6 Validator (close the loop)
Two tiers:
- **Static**: re-run ReachabilityModel over the final placement graph; assert a
  connected path from spawn to goal.
- **Dynamic (optional)**: enter Play mode, drive the `Character` with a scripted
  input sequence (or Corgi's AI brain) and confirm it reaches the goal checkpoint
  within a time budget; otherwise reject the seed and regenerate.

---

## 4. Data flow

```
seed + GenerationConfig
      │
      ▼
LayoutPlanner ──uses──> ReachabilityModel + ChunkLibrary + TilePalette
      │ (ordered placement list with X, surfaceY, prefab, role)
      ▼
SceneAssembler ──> instantiates 2D_Pack prefabs on Platforms layer
      │
      ▼
ManagerInjector ──> GameManagers / UICamera / CameraRig / LevelManager / LevelStart
      │
      ▼
BoundsBuilder ──> LevelManager.LevelBounds (+ fall floor)
      │
      ▼
Validator ──> pass? save scene : regenerate
      │
      ▼
Assets/_CorgiPlayground/Scenes/Generated_<seed>.unity
```

---

## 5. Editor UX (proposed)

An `EditorWindow` ("Corgi 2D_Pack Level Generator") with:
- Seed field (+ "randomize")
- Length / difficulty sliders, theme dropdown
- Player prefab picker (defaults to `Rectangle`)
- Buttons: **Generate Preview**, **Validate**, **Save Scene**, **Save as Prefab**
- Live count of chunks + estimated traversal time
- "Open last generated scene"

A `[MenuItem]` quick-action and a CLI batch entry point would allow generating N
levels headlessly for testing.

---

## 6. Extension points / future work

- **Theming**: swap the TilePalette rows to use Back-lit panels, pipes, and props
  for parallax/decoration layers (purely cosmetic, no collider).
- **Hazards & pickups**: extend ChunkLibrary with Corgi prefabs (Jumper, moving
  platforms, coins, `GateToNextLevel`, enemies with AI brains).
- **Multi-room / vertical**: planner could branch into vertical shafts using
  Ladders and one-way platforms (layers already exist: `Ladders`,
  `OneWayPlatforms`).
- **Wave Function Collapse** alternative: replace the grammar planner with a WFC
  solver over a tile adjacency table for more organic results.

---

## 7. Why this is robust

The proof-of-concept showed the only "tricky" parts — **surface alignment math**
and **layer assignment** — are fully solvable from prefab metadata that already
exists in 2D_Pack. Everything else (managers, player, bounds) is a fixed,
repeatable recipe. That means a generator is mostly *sequencing + validation*,
not art or physics authoring, which keeps it reliable and maintainable.
