# Corgi Engine + 2D_Pack — Platformer Level Generator (Design Outline)

> Status: **design only, no code.** This document outlines a procedural/assisted
> level generator built from the proof-of-concept level created in
> `Assets/_CorgiPlayground/Scenes/CorgiPlayground_Level1.unity`.
>
> The proof-of-concept has since grown from a single flat run into a
> **multi-floor industrial environment** (mirroring the 2D_Pack `Demo` scene):
> N stacked floors, each with a traversable foreground, a tiled lit background,
> structural decor (doors, bulkheads, pipes), on-floor props, and **inter-floor
> movement** via ramps and elevator (moving) platforms. The reference assembler
> is `Assets/Editor/Coplay/BuildCorgiLevel.cs`.

---

## 1. What the proof-of-concept established

The script-built level proved the recipe for a **fully functional, visually
rich, multi-floor Corgi Engine level** assembled from **2D_Pack art**:

| Concern | Solution found |
|---|---|
| Managers | Instantiate `GameManagers`, `MMSoundManager`, `UICamera`, `MinimalCameraRig` prefabs from CorgiEngine/Common + Demos/Minimal |
| Spawn | `LevelStart` prefab (contains a `CheckPoint`) wired into `LevelManager.DebugSpawn` |
| Player | `Rectangle.prefab` `Character` assigned to `LevelManager.PlayerPrefabs[]` |
| Collision | 2D_Pack platform prefabs already carry an **EdgeCollider2D** on their top surface; they only needed to be moved to the **`Platforms` layer (8)** |
| World limits | `LevelManager.LevelBounds` computed from the union of placed renderers, padded for headroom + a fall-death floor |
| Surface alignment | Each platform's walkable Y = `transform.y + edgeCollider.offset.y + max(point.y)`. We place "by surface height" and back-solve the pivot position |
| URP rendering | The Corgi rig is built-in-pipeline; under URP the UICamera must be an **Overlay** in the game camera's **camera stack** (else a full-screen blue clear hides the game) |
| Multi-floor | Floors are stacked on a vertical pitch (`FloorSpacing ≈ 11u`); each floor is an independent ground run + decor band |
| Depth / layering | All art at z=0 on the **Default** sorting layer; depth via **sortingOrder** (background −6/−4, platforms 0, structural decor −2, props +2, trim +3) — exactly the 2D_Pack Demo convention |
| Background | A tiled wall of `Panel_Back_*` panels (5.12 grid) with a near accent band of `Panel_Tech_*` |
| Decoration | Doorways + bulkheads frame each floor; pipes run near ceilings; consoles/barrels/boxes/machines sit on the surface; `Border_4` strip + railings trim the floor edge |

### Key 2D_Pack platform metrics (PPU 100)
=======
| Inter-floor movement | `Hover_Platform` art + Corgi **`MovingPlatform`** (`MMPathMovement`) + **kinematic Rigidbody2D** on the **`MovingPlatforms` layer (18)**; non-static so it actually moves; plus static `Platform_Ramp_45` ramps |

### Key 2D_Pack platform metrics (PPU 100)
=======

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

### Multi-floor environment model (current build)

The scene is organized under a single `Level` root with intent-named children
that the generator's assembler produces in order:

```
Level
 ├─ Background   (tiled Panel_Back_* wall + Panel_Tech_* accents, sortingOrder -6 / -4)
 ├─ Floors       (Floor_0..N-1; each a ground run of Platform_* on the Platforms layer)
 ├─ Decoration   (Decor_Floor_i: doorways, bulkheads, pipes, consoles, barrels, boxes, borders, railings)
 └─ Movers       (Elevator_* hover platforms with MovingPlatform on the MovingPlatforms layer)
```

- **Floors** are described by a `FloorConfig { SurfaceY, StartX, GroundTiles,
  GroundVariants[] }` list — exactly the data a `LayoutPlanner` would emit per
  floor. Surfaces are spaced by `FloorSpacing` so a single elevator hop or ramp
  bridges adjacent floors within the player's jump budget.
- **Sorting-order bands** give the parallax-like depth without separate sorting
  layers: `-6` far wall, `-4` accent panels, `-3` ceiling pipes, `-2` doors /
  bulkheads, `0` walkable platforms, `+2` on-floor props, `+3` edge trim.
- **Decoration is cosmetic only** (no colliders) so it never interferes with
  traversal; only `Platforms`-layer and `MovingPlatforms`-layer objects collide.
- **Inter-floor movement** uses `Hover_Platform` art driven by Corgi's
  `MovingPlatform` (a `MMPathMovement` subclass) with a 2-point vertical path,
  a kinematic `Rigidbody2D`, interpolation, and **static flags cleared** so the
  platform actually moves and carries the player between floors.

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
 ├─ GenerationConfig         (seed, floors N, length, difficulty curve, theme)
 ├─ ReachabilityModel        (player jump height/width -> max gap & step rules)
 ├─ TilePalette              (2D_Pack prefab metadata: size, surface offset, role)
 ├─ ChunkLibrary             (parameterized building blocks: Run, Gap, Stairs, Ramp, Pit, Ledge)
 ├─ FloorPlanner             (stacks N floors; chooses ground variants + surface heights)
 ├─ LayoutPlanner            (sequences chunks along each floor's X axis using a grammar)
 ├─ ConnectorPlanner         (places ramps + elevators that bridge adjacent floors)
 ├─ BackgroundBuilder        (tiles Panel_Back_* wall + Panel_Tech_* accents behind floors)
 ├─ DecorationBuilder        (doors, bulkheads, pipes, consoles, props, borders, railings)
 ├─ SceneAssembler           (instantiates prefabs, sets layer + sortingOrder, aligns by surface)
 ├─ ManagerInjector          (adds GameManagers/UICamera/CameraRig/LevelManager/LevelStart + URP camera stack)
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

### 3.5 FloorPlanner & ConnectorPlanner (multi-floor)
- **FloorPlanner** emits a `FloorConfig[]`: for each of N floors a surface height
  (`f * FloorSpacing`), a horizontal extent, and a list of ground-tile variants.
  This is the data already driving the current build.
- **ConnectorPlanner** guarantees each floor is reachable from the one below by
  placing at least one **vertical connector** whose travel equals `FloorSpacing`:
  - **Elevator** — `Hover_Platform` + `MovingPlatform` (2-point vertical path,
    kinematic RB2D, non-static, `MovingPlatforms` layer). The boarding lip and the
    drop-off lip are validated against the floor surfaces.
  - **Ramp/Stairs** — static `Platform_Ramp_45` / `Stair` chains for shorter rises.
  Connectors are alternated left/right between floors so the path snakes upward.

### 3.6 BackgroundBuilder & DecorationBuilder (the "engaging environment")
- **BackgroundBuilder** tiles a wall of `Panel_Back_*` over the floors' bounding
  box on a 5.12 grid (sortingOrder −6), then sprinkles `Panel_Tech_*` /
  `Panel_Back_Lit_2` accents nearer the camera (−4). Purely cosmetic.
- **DecorationBuilder** runs per floor (`Decor_Floor_i`) and places, all without
  colliders so traversal is never blocked:
  - structural: `Doorway_Front_Large` at floor ends, `Bulkhead_4_B` dividers
    (sortingOrder −2), `Pipe_*` near the ceiling (−3);
  - on-floor: `Console_*`, `Barrel_*`, `Box*`, `Machine` placed by their bottom
    edge on the surface (+2);
  - trim: a `Border_4` strip along the whole floor edge and `Railing_*` accents (+3).
- A `ThemePalette` (ScriptableObject) selects which panel/prop/pipe sets to use so
  alternate themes (e.g. clean lab vs. rusty engine room) are a data swap.

### 3.7 SceneAssembler
Instantiates each placement via `PrefabUtility.InstantiatePrefab`, then:
- walkable geometry → `Platforms` layer (8), sortingOrder 0, aligned via the
  **surface-offset formula** (`pivotY = desiredSurfaceY − surfaceOffset`);
- movers → `MovingPlatforms` layer (18), static flags cleared, kinematic RB2D;
- decor/background → correct **sortingOrder band**, placed by bottom or center.
Objects are grouped under `Level/{Background,Floors,Decoration,Movers}` for a
clean, inspectable hierarchy. Also configures the **URP camera stack**
(game camera Base + UICamera Overlay).

### 3.8 Validator (close the loop)
=======
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

## 5. Editor tool

### 5.1 Current reference tool (`BuildCorgiLevel.cs`)
The working assembler lives at `Assets/Editor/Coplay/BuildCorgiLevel.cs`. It is the
data-driven SceneAssembler the generator will wrap, and already demonstrates the
full pipeline end-to-end:
- a **seeded** `System.Random` (`Seed` constant) drives all variety, so the same
  seed reproduces the same level;
- a `FloorConfig[]` list declares **variable-length floors** at flush-tiled
  heights (`FloorSpacing = 10.24` = two 5.12 panels);
- `BuildBackground`, `DecorateFloor`, `BuildRampChain`, `BuildElevator` and
  `FixUrpCameraStack` are independent, reusable steps;
- it writes only under `Assets/_CorgiPlayground/...` and never edits library assets.

To regenerate: run the script (or change `Seed` / the `floors` list first).

Key tuning constants: `Grid` (5.12), `FloorSpacing` (10.24), `Seed`, and the
sortingOrder bands inside each builder.

### 5.2 Proposed `EditorWindow` ("Corgi 2D_Pack Level Generator")
- Seed field (+ "randomize")
- Floors (N), per-floor length range, difficulty sliders, theme dropdown
- Connector mix (ramps vs. single-floor elevators vs. multi-floor express elevators)
- Player prefab picker (defaults to `Rectangle`)
- Buttons: **Generate Preview**, **Validate**, **Save Scene**, **Save as Prefab**
- Live count of objects + estimated traversal time
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
