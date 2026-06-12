using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using CorgiPlayground;

/// <summary>
/// Builds a multi-floor, fully functional Corgi Engine level from 2D_Pack art.
///
/// Conventions (mirroring the 2D_Pack Demo/TEST scenes):
///   - All art at z=0; depth via sortingOrder bands:
///       -10 far background, -8 background accents, -4 pipes (behind structure),
///       -2 doors / bulkheads, 0 walkable platforms, +2 on-floor props,
///       +3 railings/trim, +6 ELEVATORS (always foreground so they occlude).
///   - Everything tiles on a 5.12 grid; FloorSpacing = 5.12 (ONE panel). Floor 0 is a
///     solid block deck; upper floors use THIN walkway decks (Platform_6..9) so the
///     one-panel gap stays walkable (headroom) instead of being filled by deck mass.
///
/// Improvements addressing review feedback:
///   (a) Pipes are built as CONNECTED RUNS (straight segments tiled end-to-end at a
///       constant Y, capped with Pipe_End / bends, valves on top) — they link things.
///   (b) Elevators sit on a high sortingOrder (foreground) and dwell at each end.
///   (c) Ramps get FLAT LANDING TILES at top and bottom and live in a clear vertical
///       band, so the player can walk on/off smoothly (no collider snag, no overhang).
///   (d) Lights mode: Fake (baked-in "Lit" panels) OR Real (swap to UnLit panels +
///       URP Light2D, rendered through a dedicated 2D renderer on the level cameras).
///   (e) Panel types alternate BETWEEN and WITHIN floors (TEST-style variety).
///   (f) Adds ramp railings + vertical stairs; leaves random gaps in the background.
///
/// Config is supplied via BuildConfig (the EditorWindow sets PendingConfig).
/// </summary>
public class BuildCorgiLevel
{
    const int LayerPlatforms = 8;
    const int LayerMovingPlatforms = 18;
    const string Folder = "Assets/_CorgiPlayground";
    const string SceneFolder = "Assets/_CorgiPlayground/Scenes";
    const string Renderer2DPath = "Assets/_CorgiPlayground/PlaygroundRenderer2D.asset";

    // Each generation writes a NEW scene file (never overwrites an existing one).
    static string _scenePath;

    const float Grid = 5.12f;
    // (c/d) One panel between floor surfaces (was 10.24 = two panels, which read as an
    // extra phantom panel-row per floor). Upper floors use thin walkway decks so this
    // single-panel gap remains walkable.
    const float FloorSpacing = 5.12f;

    public class BuildConfig
    {
        public int Seed = 20260612;
        public bool UseRealLights = false;   // false = fake baked lights; true = URP Light2D
        // NOTE: background is ALWAYS fully filled now (no gaps) so no dark voids show through.

        // ---- Floors ----
        public int NumberOfFloors = 3;            // how many stacked floors to generate (2..6)
        public bool FloorGaps = true;             // leave jumpable gaps in the platform decks

        // ---- Props ----
        public bool Pipes = true;                 // build decorative pipe networks

        // ---- Lighting ----
        public bool LightIntensityVariation = true; // randomize per-light intensity a bit
        public bool LightFlicker = false;         // SOME lights flicker (not all)
        public float GlobalLightIntensity = 0.55f;// 0 = no global fill light (real-lights mode)
        public Color PointLightColor = new Color(0.75f, 0.88f, 1f, 1f);  // per-floor point lights
        public Color GlobalLightColor = new Color(0.7f, 0.78f, 0.95f, 1f); // ambient fill (real lights)

        // ---- Effects ----
        public bool Fog = false;                  // atmospheric haze overlay
        public float FogIntensity = 0.5f;         // 0..1 -> haze density / alpha
        public bool Particles = false;            // ambient steam/spark particles

        // ---- Collectibles ----
        public bool HealthItems = false;          // Corgi stimpack health pickups
        public bool Coins = false;                // Corgi coin / power-unit pickups

        // ---- Characters: Player ----
        // Which player character the LevelManager spawns. Picked from the Player catalog.
        public string PlayerType = "Mech Trooper";

        // ---- Characters: Enemies / NPCs ----
        public bool SpawnEnemies = false;         // master y/n toggle for the enemy pass
        // Multi-select: which enemy/NPC prefabs (from Assets/Game/Prefabs/Enemies) to use.
        // Empty + SpawnEnemies==true falls back to ALL available types.
        public List<string> EnemyTypes = new List<string>();
        public int EnemiesPerFloor = 2;           // approx enemies placed per floor
    }

    // The PLAYER prefabs in Assets/Game/Prefabs/Players, keyed by the display name shown
    // in the generator's Characters tab.
    public static readonly Dictionary<string, string> PlayerCatalog = new Dictionary<string, string>
    {
        ["Mech Trooper"]  = "Assets/Game/Prefabs/Players/MechTrooperPlayer.prefab",
        ["Sci-Fi Human"]  = "Assets/Game/Prefabs/Players/SciFiHumanPlayer.prefab",
    };

    // The enemy/NPC prefabs developed for Assets/Game, wired for Corgi AI. Keyed by the
    // display name shown in the generator's multi-select picklist.
    public static readonly Dictionary<string, string> EnemyCatalog = new Dictionary<string, string>
    {
        ["Mech Gunner (enemy)"] = "Assets/Game/Prefabs/Enemies/MechGunnerEnemy.prefab",
        ["Lava Bot (enemy)"]    = "Assets/Game/Prefabs/Enemies/LavaBotEnemy.prefab",
        ["Mech NPC (Dude)"]     = "Assets/Game/Prefabs/Enemies/MechNPC_Dude.prefab",
    };

    public static BuildConfig PendingConfig;
    static BuildConfig _cfg;
    static System.Random _rng;

    static string P(string rel) => $"Assets/2D_Pack/!Prefabs/{rel}.prefab";
    static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);
    static bool Chance(float p) => _rng.NextDouble() < p;
    static T Pick<T>(IList<T> list) => list[_rng.Next(list.Count)];

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
    }

    static void SetSortingOrderRecursive(GameObject go, int order)
    {
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingOrder = order;
    }

    static float TopOffset(GameObject inst)
    {
        var edge = inst.GetComponent<EdgeCollider2D>();
        if (edge == null) return 0f;
        float maxY = float.MinValue;
        foreach (var p in edge.points) maxY = Mathf.Max(maxY, p.y);
        return edge.offset.y + maxY;
    }

    class FloorConfig
    {
        public int Index;
        public float SurfaceY;
        public float StartX;
        public int GroundTiles;
        public string[] GroundVariants;
        public string[] BackPanels;     // per-floor background palette (variety BETWEEN floors)
        public float Right => StartX + (GroundTiles - 1) * Grid;
        public float Width => GroundTiles * Grid;
    }

    // An elevator shaft: a clear vertical column from a board floor up to a drop floor.
    class Shaft
    {
        public float X;
        public int Board;   // floor index the player boards from
        public int Drop;    // floor index the player is delivered to
        public string Name;
    }

    static float GetMaxShaftX(List<Shaft> shafts)
    {
        float m = float.MinValue;
        foreach (var s in shafts) m = Mathf.Max(m, s.X);
        return m;
    }

    // Snap an X to the 5.12 grid so shafts align with ground tiles (clean carve).
    static float SnapX(float x) => Mathf.Round(x / Grid) * Grid;

    // True if any shaft passes THROUGH this floor at ~tileX (carve a gap there).
    static bool ShaftPassesThrough(List<Shaft> shafts, int floorIndex, float tileX)
    {
        foreach (var s in shafts)
            if (floorIndex > s.Board && floorIndex <= s.Drop && Mathf.Abs(s.X - tileX) < Grid * 0.5f)
                return true;
        return false;
    }

    // Logs a simple connectivity proof: every floor above 0 must be reachable.
    static string VerifyConnectivity(List<FloorConfig> floors, List<Shaft> shafts)
    {
        var reachable = new HashSet<int> { 0 };
        bool changed = true;
        // Ramp connects 0->1 explicitly.
        reachable.Add(1);
        while (changed)
        {
            changed = false;
            foreach (var s in shafts)
                if (reachable.Contains(s.Board) && !reachable.Contains(s.Drop)) { reachable.Add(s.Drop); changed = true; }
        }
        var missing = new List<int>();
        for (int i = 0; i < floors.Count; i++) if (!reachable.Contains(i)) missing.Add(i);
        return missing.Count == 0
            ? $"Connectivity OK: all {floors.Count} floors reachable (ramp 0->1, shafts {shafts.Count})."
            : $"Connectivity WARNING: floors not reachable: {string.Join(",", missing)}";
    }

    // Entry point used by execute_script (uses PendingConfig if set).
    public static string Execute()
    {
        return Build(PendingConfig ?? new BuildConfig());
    }

    public static string Build(BuildConfig cfg)
    {
        _cfg = cfg;
        _rng = new System.Random(_cfg.Seed);
        var sb = new StringBuilder();

        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "_CorgiPlayground");
        if (!AssetDatabase.IsValidFolder(SceneFolder)) AssetDatabase.CreateFolder(Folder, "Scenes");

        // Always create a NEW scene file; never overwrite an existing one.
        _scenePath = AssetDatabase.GenerateUniqueAssetPath($"{SceneFolder}/CorgiPlayground_Level.unity");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------------- Managers ----------------
        var gameManagers = Load("Assets/CorgiEngine/Common/Prefabs/LevelManagers/GameManagers.prefab");
        var soundManager = Load("Assets/CorgiEngine/Common/Prefabs/LevelManagers/MMSoundManager.prefab");
        var uiCamera     = Load("Assets/CorgiEngine/Common/Prefabs/GUI/UICamera.prefab");
        var cameraRig    = Load("Assets/CorgiEngine/Demos/Minimal/Prefabs/Camera/MinimalCameraRig.prefab");
        var levelStart   = Load("Assets/CorgiEngine/Common/Prefabs/LevelManagers/LevelStart.prefab");
        var rectangle    = Load("Assets/CorgiEngine/Demos/Minimal/Prefabs/PlayableCharacters/Rectangle.prefab");

        PrefabUtility.InstantiatePrefab(gameManagers);
        PrefabUtility.InstantiatePrefab(soundManager);
        var uiInstance = (GameObject)PrefabUtility.InstantiatePrefab(uiCamera);
        var camInstance = (GameObject)PrefabUtility.InstantiatePrefab(cameraRig);
        camInstance.transform.position = new Vector3(20, 10, -10);
        var regularCam = ConfigureCameras(camInstance, uiInstance, sb);

        // ---------------- Roots ----------------
        var level      = new GameObject("Level");
        var background = new GameObject("Background"); background.transform.SetParent(level.transform);
        var floorsRoot = new GameObject("Floors");     floorsRoot.transform.SetParent(level.transform);
        var decorRoot  = new GameObject("Decoration"); decorRoot.transform.SetParent(level.transform);
        var pipesRoot  = new GameObject("Pipes");      pipesRoot.transform.SetParent(level.transform);
        var moversRoot = new GameObject("Movers");     moversRoot.transform.SetParent(level.transform);
        var lightsRoot = new GameObject("Lights");     lightsRoot.transform.SetParent(level.transform);

        var foreground = new List<GameObject>();

        // ---------------- Floor layout (dynamic: NumberOfFloors) ----------------
        // Floor 0 is the longest "ground" floor and starts at x=0. Upper floors start at
        // x=Grid*3 so the RAMP (which occupies x[0..15.36] on the left) has OPEN AIR above
        // its base — no upper deck overhangs it (fixes ramp "approach from underneath").
        int floorCount = Mathf.Clamp(_cfg.NumberOfFloors, 2, 6);
        // Floor 0 is the solid foundation: full 5.12 block decks (Platform_1..5).
        var groundFoundation = new[]
        {
            new[]{"Platforms/Platform_1","Platforms/Platform_2","Platforms/Platform_3"},
            new[]{"Platforms/Platform_2","Platforms/Platform_3","Platforms/Platform_5"},
        };
        // (c/d) Upper floors use THIN walkway decks (Platform_6..9, 1.28-2.56 tall) so a
        // single-panel (5.12) gap above each floor stays walkable instead of being filled
        // by 5.12-tall block mass. This is the convention the 2D_Pack TEST scene uses.
        var groundWalkway = new[]
        {
            new[]{"Platforms/Platform_7","Platforms/Platform_8"},
            new[]{"Platforms/Platform_8","Platforms/Platform_6"},
            new[]{"Platforms/Platform_7","Platforms/Platform_9"},
            new[]{"Platforms/Platform_6","Platforms/Platform_8","Platforms/Platform_7"},
            new[]{"Platforms/Platform_8","Platforms/Platform_7","Platforms/Platform_9"},
        };
        var panelPalettes = new[]
        {
            new[]{"Panels/Panel_Back_UnLit","Panels/Panel_Back_Lit","Panels/Panel_Back_Lit_3"},
            new[]{"Panels/Panel_Back_Lit","Panels/Panel_Back_UnLit","Panels/Panel_Tech_6"},
            new[]{"Panels/Panel_Back_Lit_3","Panels/Panel_Back_UnLit","Panels/Panel_Tech_5"},
            new[]{"Panels/Panel_Tech_6","Panels/Panel_Back_Lit","Panels/Panel_Back_UnLit"},
            new[]{"Panels/Panel_Tech_5","Panels/Panel_Back_Lit_3","Panels/Panel_Back_Lit"},
            new[]{"Panels/Panel_Back_UnLit","Panels/Panel_Tech_6","Panels/Panel_Back_Lit_3"},
        };
        var floors = new List<FloorConfig>();
        for (int i = 0; i < floorCount; i++)
        {
            int tiles = i == 0 ? 14 : 8 + ((i * 2) % 5); // ground floor longest; uppers vary 8..12
            floors.Add(new FloorConfig
            {
                Index = i,
                SurfaceY = FloorSpacing * i,
                StartX = i == 0 ? 0f : Grid * 3f,
                GroundTiles = tiles,
                GroundVariants = i == 0
                    ? groundFoundation[0]
                    : groundWalkway[(i - 1) % groundWalkway.Length],
                BackPanels = panelPalettes[i % panelPalettes.Length],
            });
        }

        float worldLeft = 0f, worldRight = float.MinValue;
        foreach (var f in floors) worldRight = Mathf.Max(worldRight, f.Right + Grid);

        // ---------------- Elevator SHAFTS (one per upper floor, spread across X) ----------------
        // The ramp connects 0->1. For every floor i>=2 we add a shaft from floor (i-1)->i,
        // plus one EXPRESS shaft (0 -> top) so connectivity is guaranteed for any count.
        // X positions are spread across each floor's span so elevators aren't clustered.
        var shafts = new List<Shaft>();
        // 0 -> 1 elevator as an alternative to the ramp.
        shafts.Add(new Shaft{ X = SnapX(floors[1].StartX + (floors[1].Right - floors[1].StartX) * 0.30f),
                              Board = 0, Drop = 1, Name = "Elevator_F0_F1" });
        for (int i = 2; i < floorCount; i++)
        {
            float span = floors[i].Right - floors[i].StartX;
            float frac = 0.30f + 0.45f * ((i - 1) % 2); // alternate left-of-centre / right-of-centre
            shafts.Add(new Shaft{ X = SnapX(floors[i].StartX + span * frac),
                                  Board = i - 1, Drop = i, Name = $"Elevator_F{i-1}_F{i}" });
        }
        // Express 0 -> top, parked at the far right edge.
        shafts.Add(new Shaft{ X = SnapX(floors[floorCount - 1].Right - Grid),
                              Board = 0, Drop = floorCount - 1, Name = "Elevator_Express" });
        foreach (var f in floors) worldRight = Mathf.Max(worldRight, GetMaxShaftX(shafts) + Grid * 2f);

        // ---------------- Background (grid-aligned, per-floor palette) ----------------
        // Extend background 3 tiles beyond the level on each side so end caps / edge
        // decoration always have panels behind them (never a dark void).
        BuildBackground(background.transform, floors, worldLeft - Grid * 3f, worldRight + Grid * 3f);

        // ---------------- Floors + decoration ----------------
        foreach (var floor in floors)
        {
            var floorGO = new GameObject($"Floor_{floor.Index}");
            floorGO.transform.SetParent(floorsRoot.transform);

            // (e) Choose ONE interior tile to leave as a JUMPABLE GAP (a single 5.12 gap
            // is comfortably clearable). Never at the first/last 2 tiles, never on a shaft
            // tile, and never adjacent to a shaft carve (so edges stay landable).
            int gapTile = -1;
            if (_cfg.FloorGaps && floor.GroundTiles >= 7)
            {
                var candidates = new List<int>();
                for (int i = 2; i < floor.GroundTiles - 2; i++)
                {
                    float tx = floor.StartX + i * Grid;
                    if (ShaftPassesThrough(shafts, floor.Index, tx)) continue;
                    if (ShaftPassesThrough(shafts, floor.Index, tx - Grid)) continue;
                    if (ShaftPassesThrough(shafts, floor.Index, tx + Grid)) continue;
                    candidates.Add(i);
                }
                if (candidates.Count > 0) gapTile = candidates[_rng.Next(candidates.Count)];
            }

            for (int i = 0; i < floor.GroundTiles; i++)
            {
                float tileX = floor.StartX + i * Grid;
                // Carve a gap where a shaft passes through this floor.
                if (ShaftPassesThrough(shafts, floor.Index, tileX)) continue;
                // (e) Leave the chosen jumpable gap open.
                if (i == gapTile) continue;
                string variant = floor.GroundVariants[i % floor.GroundVariants.Length];
                var g = PlaceBySurface(P(variant), tileX, floor.SurfaceY, floorGO.transform, $"Ground_{i}");
                SetLayerRecursive(g, LayerPlatforms); SetSortingOrderRecursive(g, 0);
                foreground.Add(g);
            }

            // (b) PLATFORM END caps: finish each deck's outer left/right edges (flush).
            // (c) EXCEPTION: floor 1 is where the RAMP lands. Its left endcap is a tall
            // solid block that hangs down into the ramp's climb path and blocks the player.
            // The ramp top corner joins floor 1's first deck tile directly, so we SKIP the
            // left cap there (the ramp itself terminates that edge). Fix-for-good.
            bool skipLeftCap = floor.Index == 1;
            AddFloorEndCaps(floor, floorGO.transform, skipLeftCap);

            // (e) Cap BOTH exposed edges of the jumpable gap too, so the gap reads as two
            // platform ends facing each other (not a raw void). The tile to the LEFT of the
            // gap gets a right-cap; the tile to the RIGHT of the gap gets a left-cap.
            if (gapTile >= 0)
            {
                float gapLeftDeckX = floor.StartX + (gapTile - 1) * Grid;  // last deck tile before gap
                float gapRightDeckX = floor.StartX + (gapTile + 1) * Grid; // first deck tile after gap
                PlaceRightCap(gapLeftDeckX, floor.SurfaceY, floorGO.transform, "GapCap_Left");
                PlaceLeftCap(gapRightDeckX, floor.SurfaceY, floorGO.transform, "GapCap_Right");
            }

            DecorateFloor(floor, decorRoot.transform, pipesRoot.transform, lightsRoot.transform);
        }

        // ---------------- Connectors ----------------
        // (A) RAMP chain 0->1 in the clear left band, with flat landings + railings.
        BuildRampConnector(floorsRoot.transform, decorRoot.transform, floors[0], floors[1], foreground);

        // (B) Vertical STAIRCASE (functional, Platform_10 steps) on floor 1.
        BuildStaircase(floorsRoot.transform, decorRoot.transform, floors[1], floors[1].StartX + Grid * 4f, foreground);

        // (C/D) ELEVATORS in their shafts, with boarding + drop-off lips so the player
        //       can always step on at the bottom and off at the top — for any floor lengths.
        foreach (var shaft in shafts)
        {
            var lower = floors[shaft.Board];
            var upper = floors[shaft.Drop];
            // Boarding lip on the board floor at the shaft X.
            var boardLip = PlaceBySurface(P("Platforms/Platform_7"), shaft.X, lower.SurfaceY, floorsRoot.transform, $"{shaft.Name}_BoardLip");
            SetLayerRecursive(boardLip, LayerPlatforms); SetSortingOrderRecursive(boardLip, 0); foreground.Add(boardLip);
            // Drop-off lip on the drop floor at the shaft X.
            var dropLip = PlaceBySurface(P("Platforms/Platform_7"), shaft.X, upper.SurfaceY, floorsRoot.transform, $"{shaft.Name}_DropLip");
            SetLayerRecursive(dropLip, LayerPlatforms); SetSortingOrderRecursive(dropLip, 0); foreground.Add(dropLip);
            // The elevator itself.
            float travel = upper.SurfaceY - lower.SurfaceY;
            var elev = BuildElevator(moversRoot.transform, shaft.Name,
                new Vector3(shaft.X, lower.SurfaceY, 0f), travel);
            foreground.Add(elev);
        }
        sb.AppendLine(VerifyConnectivity(floors, shafts));

        // (g) Decorative CRANE (Prop_1_Crane) mounted on the top floor's ceiling band,
        //     used as the art pack intends (hanging structure prop).
        var topFloor = floors[floors.Count - 1];
        var crane = PlaceByCenter(P("Platforms/Prop_1_Crane"),
            topFloor.StartX + (topFloor.Right - topFloor.StartX) * 0.4f,
            topFloor.SurfaceY + FloorSpacing * 0.5f, decorRoot.transform, -1, "Crane");

        // ---------------- Spawn + LevelManager ----------------
        var spawnInstance = (GameObject)PrefabUtility.InstantiatePrefab(levelStart);
        spawnInstance.name = "LevelStart";
        spawnInstance.transform.SetParent(level.transform);
        spawnInstance.transform.position = new Vector3(Grid * 4f, floors[0].SurfaceY + 4f, 0f);
        var checkPoint = spawnInstance.GetComponent<CheckPoint>();

        // Resolve the chosen player prefab from the catalog (fall back to Corgi's Rectangle).
        Character playerCharacter = null;
        if (PlayerCatalog.TryGetValue(_cfg.PlayerType ?? "", out var playerPath))
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPath);
            if (playerPrefab != null) playerCharacter = playerPrefab.GetComponent<Character>();
            if (playerCharacter == null)
                sb.AppendLine($"WARN: player prefab '{_cfg.PlayerType}' has no Character component; using Rectangle.");
        }
        if (playerCharacter == null) playerCharacter = rectangle.GetComponent<Character>();

        var levelManagerGO = new GameObject("LevelManager");
        var levelManager = levelManagerGO.AddComponent<LevelManager>();
        levelManager.PlayerPrefabs = new Character[] { playerCharacter };
        levelManager.AutoAttributePlayerIDs = true;
        levelManager.DebugSpawn = checkPoint;
        levelManager.BoundsMode = LevelManager.BoundsModes.TwoD;

        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (var g in foreground)
        {
            var r = g.GetComponentInChildren<Renderer>();
            if (r == null) continue;
            if (first) { b = r.bounds; first = false; } else b.Encapsulate(r.bounds);
        }
        b.Expand(new Vector3(4f, 12f, 10f));
        levelManager.LevelBounds = new Bounds(b.center, b.size);

        // ---------------- Lights mode ----------------
        if (_cfg.UseRealLights)
        {
            SetupRealLights(regularCam, uiInstance, sb);
            AssignLitMaterial(level, sb); // 2D lights only affect sprites with a Lit material
        }

        // ---------------- Atmosphere (fog + particles) ----------------
        if (_cfg.Fog) BuildFog(level.transform, b, sb);
        if (_cfg.Particles) BuildParticles(level.transform, b, floors, sb);

        // ---------------- Collectibles (Corgi pickables) ----------------
        if (_cfg.Coins || _cfg.HealthItems)
            BuildCollectibles(level.transform, floors, shafts, sb);

        // ---------------- Enemies / NPCs ----------------
        if (_cfg.SpawnEnemies)
            BuildEnemies(level.transform, floors, shafts, sb);

        // ---------------- Level Exit (one top-floor door -> next level) ----------------
        BuildLevelExit(level.transform, floors, sb);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, _scenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        sb.AppendLine($"Scene saved={saved} at {_scenePath}");
        sb.AppendLine($"Seed={_cfg.Seed}  Lights={(_cfg.UseRealLights ? "REAL Light2D" : "FAKE baked")}  BG=full-fill");
        sb.AppendLine($"Floors={floors.Count} (lengths {string.Join("/", floors.Select(f => f.GroundTiles))}), foreground objects={foreground.Count}");
        sb.AppendLine($"Connectors: Ramp(0->1)+railings, Staircase(floor1), {shafts.Count} Elevator shafts with carved gaps + board/drop lips");
        sb.AppendLine($"LevelBounds center={levelManager.LevelBounds.center} size={levelManager.LevelBounds.size}");
        return sb.ToString();
    }

    // ----------------------------------------------------------------
    // Placement helpers
    // ----------------------------------------------------------------
    static GameObject PlaceBySurface(string prefabPath, float centerX, float surfaceY, Transform parent, string name)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(Load(prefabPath));
        float top = TopOffset(inst);
        inst.transform.SetParent(parent);
        inst.transform.position = new Vector3(centerX, surfaceY - top, 0f);
        if (!string.IsNullOrEmpty(name)) inst.name = name;
        return inst;
    }

    static GameObject PlaceByBottom(string prefabPath, float centerX, float bottomY, Transform parent, int sortingOrder, string name = null)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(Load(prefabPath));
        var sr = inst.GetComponentInChildren<SpriteRenderer>();
        float halfH = sr != null && sr.sprite != null ? sr.sprite.bounds.extents.y : 0f;
        inst.transform.SetParent(parent);
        inst.transform.position = new Vector3(centerX, bottomY + halfH, 0f);
        SetSortingOrderRecursive(inst, sortingOrder);
        if (!string.IsNullOrEmpty(name)) inst.name = name;
        return inst;
    }

    static GameObject PlaceByCenter(string prefabPath, float centerX, float centerY, Transform parent, int sortingOrder, string name = null)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(Load(prefabPath));
        inst.transform.SetParent(parent);
        inst.transform.position = new Vector3(centerX, centerY, 0f);
        SetSortingOrderRecursive(inst, sortingOrder);
        if (!string.IsNullOrEmpty(name)) inst.name = name;
        return inst;
    }

    static float SpriteWidth(string prefabPath)
    {
        var go = Load(prefabPath);
        var sr = go.GetComponentInChildren<SpriteRenderer>();
        return sr != null && sr.sprite != null ? sr.sprite.bounds.size.x : Grid;
    }

    // ----------------------------------------------------------------
    // (b) Platform END caps. Platform_End_1_Big is 5.12x5.12 with its deck edge at y=+2.57
    // (PlaceBySurface aligns that edge with the deck surface, so caps are vertically FLUSH
    // for both block and thin decks).
    //
    // Cap orientation (from the collider geometry):
    //   Platform_End_1_Big          deck/solid mass on the LEFT  (collider x[-2.57..0.59])
    //                               -> terminates a deck on its RIGHT edge.
    //   Platform_End_1_Big_Flipped  deck/solid mass on the RIGHT (collider x[-0.61..2.55])
    //                               -> terminates a deck on its LEFT edge.
    // (Previously these were swapped, so left looked like right and vice-versa.)
    // ----------------------------------------------------------------
    static GameObject PlaceLeftCap(float deckLeftEdgeCenterX, float surfaceY, Transform parent, string name)
    {
        // Sits one tile to the LEFT of the first deck tile; uses the Flipped (mass-right)
        // art so its solid side abuts the deck and the open side faces out (flush join).
        var cap = PlaceBySurface(P("Platforms/Platform_End_1_Big_Flipped"), deckLeftEdgeCenterX - Grid, surfaceY, parent, name);
        // (b) Caps carry the prefab's EdgeCollider2D; put them on the Platforms layer so the
        // collider is actually walkable/standable (was Default -> player fell through).
        SetLayerRecursive(cap, LayerPlatforms);
        SetSortingOrderRecursive(cap, 0);
        return cap;
    }

    static GameObject PlaceRightCap(float deckRightEdgeCenterX, float surfaceY, Transform parent, string name)
    {
        // Sits one tile to the RIGHT of the last deck tile; uses the default (mass-left)
        // art so its solid side abuts the deck and the open side faces out (flush join).
        var cap = PlaceBySurface(P("Platforms/Platform_End_1_Big"), deckRightEdgeCenterX + Grid, surfaceY, parent, name);
        // (b) See above — Platforms layer so the cap's collider is walkable.
        SetLayerRecursive(cap, LayerPlatforms);
        SetSortingOrderRecursive(cap, 0);
        return cap;
    }

    static void AddFloorEndCaps(FloorConfig floor, Transform parent, bool skipLeftCap = false)
    {
        if (!skipLeftCap)
            PlaceLeftCap(floor.StartX, floor.SurfaceY, parent, "EndCap_Left");
        PlaceRightCap(floor.Right, floor.SurfaceY, parent, "EndCap_Right");
    }

    // ----------------------------------------------------------------
    // Background (per-floor palette, fake/real-light aware). ALWAYS fully filled —
    // every cell gets a panel so there are never dark voids behind the action.
    // ----------------------------------------------------------------
    static void BuildBackground(Transform parent, List<FloorConfig> floors, float xMin, float xMax)
    {
        xMin = Mathf.Floor(xMin / Grid) * Grid;
        xMax = Mathf.Ceil(xMax / Grid) * Grid;
        // (alignment) CRITICAL: yBottom must be an INTEGER multiple of Grid so the panel
        // cell BOUNDARIES land exactly on the floor surfaces (which are at FloorSpacing*i =
        // multiples of 5.12). The row loop steps by Grid from yBottom, and each panel center
        // is rowY + Grid*0.5, so cell seams sit at yBottom + n*Grid. With yBottom = -2*Grid
        // the seams are at ..., -5.12, 0, 5.12, 10.24, ... -> every deck fills exactly ONE
        // panel cell with its support FOOT flush on the panel's bottom seam (no mid-panel
        // float). A half-multiple (the old -1.5*Grid) phase-shifted every deck by 2.56.
        float yBottom = -Grid * 2f;                                  // one margin cell below floor 0
        float yTop = floors[floors.Count - 1].SurfaceY + Grid * 2f;  // one margin cell above top floor

        // Single global grid pass (no per-floor overlap, no gaps). Palette is chosen by the
        // nearest floor below the row, giving per-floor variety without doubling up.
        int col = 0;
        for (float x = xMin; x <= xMax; x += Grid, col++)
        {
            int row = 0;
            for (float y = yBottom; y <= yTop; y += Grid, row++)
            {
                float rowCenterY = y + Grid * 0.5f;
                var floor = NearestFloorBelow(floors, rowCenterY);
                string panel = floor.BackPanels[(col + row) % floor.BackPanels.Length];
                PlaceByCenter(P(LightAwarePanel(panel)), x + Grid * 0.5f, rowCenterY, parent, -10, "BG");
            }
        }

        // Accent panels: sparse, per-floor, for variety within each floor.
        string[] accents = { "Panels/Panel_Tech_6", "Panels/Panel_Tech_9", "Panels/Panel_3_Clear", "Panels/Panel_Vent" };
        foreach (var floor in floors)
            for (float x = xMin + Grid; x < xMax - Grid; x += Grid * 2f)
            {
                if (!Chance(0.3f)) continue;
                PlaceByCenter(P(LightAwarePanel(Pick(accents))), x, floor.SurfaceY + Grid * 0.5f, parent, -8, "BG_Accent");
            }
    }

    static FloorConfig NearestFloorBelow(List<FloorConfig> floors, float y)
    {
        FloorConfig best = floors[0];
        foreach (var f in floors)
            if (f.SurfaceY <= y + 0.01f && f.SurfaceY >= best.SurfaceY) best = f;
        return best;
    }

    // Swap "Lit" baked panels for unlit variants when using real lights.
    static string LightAwarePanel(string panel)
    {
        if (!_cfg.UseRealLights) return panel;
        switch (panel)
        {
            case "Panels/Panel_Back_Lit":   return "Panels/Panel_Back_UnLit";
            case "Panels/Panel_Back_Lit_2": return "Panels/Panel_Back_UnLit";
            case "Panels/Panel_Back_Lit_3": return "Panels/Panel_Back_Lit_3_UnLit";
            case "Panels/Panel_Back_Lit_4": return "Panels/Panel_Back_UnLit";
            case "Panels/Panel_Tech_2_Lit": return "Panels/Panel_Tech_2_UnLit";
            default: return panel;
        }
    }

    // ----------------------------------------------------------------
    // Per-floor decoration (varied), pipe runs, and lights
    // ----------------------------------------------------------------
    static void DecorateFloor(FloorConfig floor, Transform decorParent, Transform pipesParent, Transform lightsParent)
    {
        var t = new GameObject($"Decor_Floor_{floor.Index}").transform;
        t.SetParent(decorParent);
        float surface = floor.SurfaceY;
        float left = floor.StartX;
        float right = floor.Right;

        // Doorways: vary type per floor, flush on surface (tuck under deck above).
        string[] doorTypes = { "Doors/Doorway_Front_Large", "Doors/Doorway_Front_Small", "Doors/Doorway_Side" };
        PlaceByBottom(P(Pick(doorTypes)), left + Grid * 0.5f, surface, t, -2, "Doorway_Left");
        PlaceByBottom(P(Pick(doorTypes)), right - Grid * 0.5f, surface, t, -2, "Doorway_Right");

        // Bulkhead dividers: random count/positions/type.
        string[] bulks = { "Props/Bulkhead_4_B", "Props/Bulkhead_2", "Props/Bulkhead_5" };
        int bulkheads = 1 + _rng.Next(2);
        for (int i = 0; i < bulkheads; i++)
        {
            float bx = Mathf.Lerp(left + Grid * 2f, right - Grid * 2f, (i + 0.5f) / bulkheads);
            PlaceByBottom(P(Pick(bulks)), bx, surface, t, -2, $"Bulkhead_{i}");
        }

        // (f) CONNECTED PIPE NETWORK across the headroom band — horizontal runs with
        // bends, valves, T-junctions and vertical risers/drops (only if Pipes enabled).
        if (_cfg.Pipes)
            BuildPipeNetwork(pipesParent, floor, left + Grid, right - Grid, $"PipeRun_F{floor.Index}");

        // (b) On-floor OBSTACLE props. These have BoxCollider2Ds; we move them to the
        // Platforms layer so the Corgi controller actually collides -> the player must
        // jump over them. They are spaced along the walkable path, and some are STACKED
        // (box on box) to force a real jump.
        string[] obstacles = { "Props/Box", "Props/Box_B", "Props/Barrel_Red", "Props/Barrel_Blue" };
        int propCount = 2 + _rng.Next(3);
        int placedProps = 0;
        for (int i = 0; i < propCount; i++)
        {
            // Spread evenly across the middle of the floor (avoid the very edges/shaft).
            float px = Mathf.Lerp(left + Grid * 1.5f, right - Grid * 1.5f, (i + 0.5f) / propCount);
            var p0 = PlaceByBottom(P(Pick(obstacles)), px, surface, t, 2, $"Prop_{placedProps++}");
            SetLayerRecursive(p0, LayerPlatforms);
            // ~40% of the time stack a second crate on top so it's a taller hurdle.
        if (Chance(0.4f))
            {
                float h0 = p0.GetComponentInChildren<SpriteRenderer>().sprite.bounds.size.y;
                var p1 = PlaceByBottom(P(Pick(obstacles)), px, surface + h0, t, 2, $"Prop_{placedProps++}");
                SetLayerRecursive(p1, LayerPlatforms);
            }
        }

        // Decorative consoles/machine against the back wall (NOT obstacles, stay on
        // default layer behind the action) so floors still feel populated.
        string[] decoProps = { "Props/Console_1", "Props/Console_2", "Props/Console_3", "Props/Machine" };
        int decoCount = 1 + _rng.Next(2);
        for (int i = 0; i < decoCount; i++)
        {
            float dx = left + Grid * (1.2f + (float)_rng.NextDouble() * Mathf.Max(0.1f, floor.GroundTiles - 2.4f));
            PlaceByBottom(P(Pick(decoProps)), dx, surface, t, -1, $"Deco_{i}");
        }

        // (c) Borders REMOVED — they were stacking on the deck and reading as clutter.

        // (d) Continuous RAILING RUN (so it's clearly visible, not a lone tile).
        // Railing_Top is 2.56 wide -> tile a contiguous span near one end of the floor.
        float railW = SpriteWidth(P("Props/Railing_Top"));
        int railTiles = Mathf.Clamp(2 + _rng.Next(3), 2, Mathf.Max(2, floor.GroundTiles - 2));
        float railStart = left + Grid * (1f + _rng.Next(Mathf.Max(1, floor.GroundTiles - railTiles - 1)));
        for (int i = 0; i < railTiles; i++)
            PlaceByBottom(P("Props/Railing_Top"), railStart + i * railW, surface, t, 4, $"Railing_{i}");

        // Lights: fake = baked glow sprite; real = URP Light2D point light.
        // - LightIntensityVariation: each light gets a randomized brightness multiplier.
        // - LightFlicker: ~35% of lights get an LightFlicker2D component (some, not all).
        int lightCount = 2 + _rng.Next(2);
        for (int i = 0; i < lightCount; i++)
        {
            float lx = Mathf.Lerp(left + Grid, right - Grid, (i + 0.5f) / lightCount);
            float ly = surface + FloorSpacing * 0.62f;
            float mult = _cfg.LightIntensityVariation ? Mathf.Lerp(0.55f, 1.25f, (float)_rng.NextDouble()) : 1f;
            bool flicker = _cfg.LightFlicker && Chance(0.35f);
            if (_cfg.UseRealLights)
            {
                CreateLight2D(lightsParent, lx, ly, $"Light2D_F{floor.Index}_{i}", mult, flicker);
            }
            else
            {
                var fl = PlaceByCenter(P("Lights&Shadows/Light_1"), lx, ly, t, -7, $"FakeLight_{i}");
                ApplyFakeLight(fl, mult, flicker);
            }
        }
    }

    // Apply intensity (sprite alpha) variation and optional flicker to a fake glow sprite.
    static void ApplyFakeLight(GameObject fakeLight, float mult, bool flicker)
    {
        var sr = fakeLight.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            // Tint by the configured point-light colour, keep the original alpha * mult.
            var c = _cfg.PointLightColor;
            c.a = Mathf.Clamp01(sr.color.a * mult);
            sr.color = c;
        }
        if (flicker)
        {
            var f = fakeLight.AddComponent<LightFlicker2D>();
            f.BaseMultiplier = mult;
        }
    }

    // ----------------------------------------------------------------
    // (a) Connected pipe RUN. CRITICAL: every piece in a run comes from ONE coherent
    // family (all regular OR all small) — we never mix a thick straight with thin
    // connectors or vice-versa. Each run is a clean horizontal pipe living in the
    // one-panel headroom band, terminated with matching End caps and dressed with
    // matching valves from the SAME family.
    //
    // Family geometry (centred at origin, 5.12 wide):
    //   REGULAR: straight Pipe_1 (5.12x1.28), end Pipe_End_1 (2.56x2.56),
    //            valve Pipe_Valve_1 (2.56x2.56).
    //   SMALL:   straight Pipe_Small_Straight_2 (5.12x1.28), end Pipe_End_2 (2.56x2.56),
    //            valve Pipe_Valve_2 (1.28x1.28).
    // Pipes are wall dressing: order -5 (behind deck/doors >= -2, in front of bg).
    // ----------------------------------------------------------------
    const int PipeOrder = -5;
    const float PipeHalf = 2.56f; // half of a 5.12 segment

    class PipeFamily
    {
        public string Straight;
        public string End;     // end cap (opening faces +X at rot 0)
        public string Valve;
        public float ValveHalfH; // half height of the valve sprite (sits flush on top)
    }

    static readonly PipeFamily[] PipeFamilies =
    {
        // REGULAR family — all parts thick.
        new PipeFamily{ Straight = "Pipe_1", End = "Pipe_End_1", Valve = "Pipe_Valve_1", ValveHalfH = 1.28f },
        // SMALL family — all parts thin.
        new PipeFamily{ Straight = "Pipe_Small_Straight_2", End = "Pipe_End_2", Valve = "Pipe_Valve_2", ValveHalfH = 0.64f },
    };

    static GameObject PipePiece(string n, float x, float y, float rot, Transform parent, string name)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(Load(P("Pipes/" + n)));
        inst.transform.SetParent(parent);
        inst.transform.position = new Vector3(x, y, 0f);
        inst.transform.rotation = Quaternion.Euler(0, 0, rot);
        SetSortingOrderRecursive(inst, PipeOrder);
        inst.name = name;
        return inst;
    }

    static void BuildPipeNetwork(Transform parent, FloorConfig floor, float xStart, float xEnd, string name)
    {
        var run = new GameObject(name).transform;
        run.SetParent(parent);

        // (a) Pick ONE family for this whole run — no mixing of small and regular parts.
        var fam = PipeFamilies[floor.Index % PipeFamilies.Length];

        // Sit the run in the one-panel headroom band, just under the deck above so it
        // never clips a walkway. Pipes are 1.28 tall -> centre ~0.7 below the ceiling.
        float yMain = floor.SurfaceY + FloorSpacing - 1.4f;
        float w = 5.12f; // every straight is 5.12 wide

        int n = Mathf.Max(2, Mathf.RoundToInt((xEnd - xStart) / w));
        float runLeft = xStart + w * 0.5f; // centre of first straight segment

        // Straights tiled flush end-to-end. (c) Valves are placed INLINE on the pipe
        // CENTERLINE (same Y as the straights) at the SEAM between two segments, so they
        // read as a fitting that's part of the route — never a piece floating above it.
        // Drawn just in front of the straights so the inline fitting reads on top of them.
        for (int i = 0; i < n; i++)
        {
            float cx = runLeft + i * w;
            PipePiece(fam.Straight, cx, yMain, 0f, run, $"Pipe_{i}");
        }
        for (int i = 1; i < n; i++) // seams between consecutive straights
        {
            if (!Chance(0.4f)) continue;
            float seamX = runLeft + i * w - w * 0.5f;       // centre of the seam
            var v = PipePiece(fam.Valve, seamX, yMain, 0f, run, $"Pipe_Valve_{i}"); // inline, on centerline
            SetSortingOrderRecursive(v, PipeOrder + 1);
        }

        // Matching END caps from the SAME family, flush against each outer straight.
        // Pipe_End faces +X at rot 0 (cap opening to the right); rotate 180 for the left.
        float leftEndX = runLeft - w * 0.5f - PipeHalf * 0.5f;
        float rightEndX = runLeft + (n - 1) * w + w * 0.5f + PipeHalf * 0.5f;
        PipePiece(fam.End, leftEndX, yMain, 180f, run, "Pipe_End_Left");
        PipePiece(fam.End, rightEndX, yMain, 0f, run, "Pipe_End_Right");
    }

    // ----------------------------------------------------------------
    // (c) Ramp connector. With one-panel floor spacing (5.12) a SINGLE Ramp_45 climbs a
    // full floor (rise 5.12 over 5.12 horizontal) — the ramp corners are exactly the two
    // floor surfaces, so walk-on/off is flush with no extra landing tiles.
    // ----------------------------------------------------------------
    static void BuildRampConnector(Transform floorsParent, Transform decorParent, FloorConfig from, FloorConfig to, List<GameObject> foreground)
    {
        // Place so the ramp TOP corner lands exactly at the upper floor's left edge.
        // Ramp_45 edge: (-2.55,-2.56)->(2.56,2.56). Top corner = center + (2.56, 2.56).
        float half = Grid * 0.5f;
        float topCornerX = to.StartX;        // land at upper floor start
        float cx = topCornerX - half;        // ramp center x
        float cy = to.SurfaceY - half;       // ramp center y (top corner at to.SurfaceY)

        var rampGrp = new GameObject("Ramp_F0_F1").transform; rampGrp.SetParent(floorsParent);

        var ramp = (GameObject)PrefabUtility.InstantiatePrefab(Load(P("Platforms/Platform_Ramp_45")));
        ramp.name = "Ramp_A"; ramp.transform.SetParent(rampGrp); ramp.transform.position = new Vector3(cx, cy, 0f);
        SetLayerRecursive(ramp, LayerPlatforms); SetSortingOrderRecursive(ramp, 0); foreground.Add(ramp);

        // The floor decks themselves are the flat landings: floor 0's deck extends under
        // the ramp base and floor 1's deck starts at the ramp top corner -> flush join.
        float bottomCornerX = cx - half;

        // (f) Ramp RAILINGS running up the slope (decorative).
        for (int i = 0; i <= 3; i++)
        {
            float fx = Mathf.Lerp(bottomCornerX, topCornerX, i / 3f);
            float fy = Mathf.Lerp(from.SurfaceY, to.SurfaceY, i / 3f) + 0.8f;
            PlaceByCenter(P("Props/Railing_yellow"), fx, fy, decorParent, 3, $"RampRail_{i}");
        }
    }

    // ----------------------------------------------------------------
    // (b/f) Functional staircase from Platform_10 steps (each has a collider),
    // plus decorative Stair sprites alongside.
    // ----------------------------------------------------------------
    static void BuildStaircase(Transform floorsParent, Transform decorParent, FloorConfig floor, float baseX, List<GameObject> foreground)
    {
        var grp = new GameObject($"Staircase_F{floor.Index}").transform; grp.SetParent(floorsParent);
        int steps = 3;
        // Keep the total rise (steps*stepRise) under the one-panel headroom (5.12).
        float stepRise = 1.1f, stepRun = 1.4f;
        for (int i = 1; i <= steps; i++)
        {
            var s = PlaceBySurface(P("Platforms/Platform_10"), baseX + i * stepRun, floor.SurfaceY + i * stepRise, grp, $"Step_{i}");
            SetLayerRecursive(s, LayerPlatforms); SetSortingOrderRecursive(s, 0); foreground.Add(s);
            // decorative stair sprite under each step
            PlaceByCenter(P("Props/Stair"), baseX + i * stepRun, floor.SurfaceY + i * stepRise - 0.6f, decorParent, 2, $"StairDeco_{i}");
        }
        PlaceByCenter(P("Props/Stair_Top"), baseX + steps * stepRun, floor.SurfaceY + steps * stepRise + 0.3f, decorParent, 2, "StairTop");
    }

    // ----------------------------------------------------------------
    // (b) Elevator: foreground sortingOrder, dwell at ends, kinematic RB2D.
    // ----------------------------------------------------------------
    static GameObject BuildElevator(Transform parent, string name, Vector3 bottomSurfacePos, float travel)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(Load(P("Platforms/Hover_Platform")));
        inst.name = name;
        inst.transform.SetParent(parent);
        // (b) Scale the elevator DOWN so its deck is narrower -> harder to board.
        float elevScale = 0.62f;
        inst.transform.localScale = inst.transform.localScale * elevScale;
        float top = TopOffset(inst) * elevScale;
        inst.transform.position = new Vector3(bottomSurfacePos.x, bottomSurfacePos.y - top, 0f);
        SetLayerRecursive(inst, LayerMovingPlatforms);
        SetSortingOrderRecursive(inst, 6); // (b) always in front so it occludes structure

        foreach (var tr in inst.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(tr.gameObject, 0);

        var rb = inst.GetComponent<Rigidbody2D>();
        if (rb == null) rb = inst.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var mp = inst.GetComponent<MovingPlatform>();
        if (mp == null) mp = inst.AddComponent<MovingPlatform>();
        mp.CycleOption = MMPathMovement.CycleOptions.BackAndForth;
        mp.MovementSpeed = 2.5f;
        mp.PathElements = new List<MMPathMovementElement>
        {
            new MMPathMovementElement{ PathElementPosition = new Vector3(0, 0, 0), Delay = 2.5f },       // (b) dwell so the player can board/leave
            new MMPathMovementElement{ PathElementPosition = new Vector3(0, travel, 0), Delay = 2.5f },
        };
        return inst;
    }

    // ----------------------------------------------------------------
    // (d) Real URP Light2D + dedicated 2D renderer on the playground cameras.
    // ----------------------------------------------------------------
    static Material _litMat;
    static void AssignLitMaterial(GameObject level, StringBuilder sb)
    {
        var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
        if (shader == null) { sb.AppendLine("WARN: Sprite-Lit-Default shader not found; sprites stay unlit."); return; }
        if (_litMat == null)
        {
            string matPath = Folder + "/SpriteLit.mat";
            _litMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (_litMat == null)
            {
                _litMat = new Material(shader);
                AssetDatabase.CreateAsset(_litMat, matPath);
                AssetDatabase.SaveAssets();
            }
        }
        int count = 0;
        foreach (var sr in level.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sharedMaterial = _litMat;
            count++;
        }
        sb.AppendLine($"Assigned Sprite-Lit-Default to {count} sprite renderers.");
    }

    static void CreateLight2D(Transform parent, float x, float y, string name, float mult = 1f, bool flicker = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, y, 0f);
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.intensity = 2.2f * mult;
        light.color = _cfg.PointLightColor;
        light.pointLightOuterRadius = 9f;
        light.pointLightInnerRadius = 2f;
        if (flicker)
        {
            var f = go.AddComponent<LightFlicker2D>();
            f.BaseMultiplier = mult;
        }
    }

    // A reusable soft, alpha-blended particle/fog material (prevents the magenta
    // "no material" look on runtime ParticleSystems / sprites).
    static Material _softMat;
    static Material GetSoftParticleMaterial()
    {
        if (_softMat != null) return _softMat;
        string matPath = Folder + "/SoftParticle.mat";
        _softMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (_softMat == null)
        {
            // Particles/Standard Unlit is the modern soft particle shader; fall back to
            // the legacy additive / sprite shaders if it isn't present.
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                        ?? Shader.Find("Particles/Standard Unlit")
                        ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended")
                        ?? Shader.Find("Sprites/Default");
            _softMat = new Material(shader);
            // Use the soft round glow texture so puffs/steam are soft, not boxy.
            var glow = Load(P("Lights&Shadows/Light_Foggy"));
            var sr = glow != null ? glow.GetComponentInChildren<SpriteRenderer>() : null;
            if (sr != null && sr.sprite != null)
            {
                var tex = sr.sprite.texture;
                if (_softMat.HasProperty("_BaseMap")) _softMat.SetTexture("_BaseMap", tex);
                if (_softMat.HasProperty("_MainTex")) _softMat.SetTexture("_MainTex", tex);
            }
            // Force alpha-blend surface where the property exists (URP particle shader).
            if (_softMat.HasProperty("_Surface")) _softMat.SetFloat("_Surface", 1f);
            if (_softMat.HasProperty("_Blend")) _softMat.SetFloat("_Blend", 0f);
            AssetDatabase.CreateAsset(_softMat, matPath);
            AssetDatabase.SaveAssets();
        }
        return _softMat;
    }

    // ----------------------------------------------------------------
    // Fog: soft, semi-transparent haze sprites drifting in front of the background
    // (behind the action) to add atmospheric depth. Density scales with FogIntensity.
    // ----------------------------------------------------------------
    static void BuildFog(Transform levelParent, Bounds levelBounds, StringBuilder sb)
    {
        var fogRoot = new GameObject("Fog").transform;
        fogRoot.SetParent(levelParent);
        // Use the dedicated foggy light sprite for a soft haze look.
        var glow = Load(P("Lights&Shadows/Light_Foggy")) ?? Load(P("Lights&Shadows/Light_1"));
        var sprite = glow != null ? glow.GetComponentInChildren<SpriteRenderer>()?.sprite : null;
        float intensity = Mathf.Clamp01(_cfg.FogIntensity);
        int puffs = Mathf.RoundToInt(Mathf.Lerp(6f, 26f, intensity)); // density scales
        float alpha = Mathf.Lerp(0.05f, 0.22f, intensity);            // opacity scales
        for (int i = 0; i < puffs; i++)
        {
            float x = Mathf.Lerp(levelBounds.min.x, levelBounds.max.x, (float)_rng.NextDouble());
            float y = Mathf.Lerp(levelBounds.min.y, levelBounds.max.y, (float)_rng.NextDouble());
            var go = new GameObject($"FogPuff_{i}");
            go.transform.SetParent(fogRoot);
            go.transform.position = new Vector3(x, y, 0f);
            float s = Mathf.Lerp(10f, 20f, (float)_rng.NextDouble());
            go.transform.localScale = new Vector3(s, s * 0.5f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.6f, 0.65f, 0.75f, alpha);
            sr.sortingOrder = -6; // in front of background panels, behind gameplay
        }
        sb.AppendLine($"Fog: {puffs} haze puffs (intensity {intensity:F2}, alpha {alpha:F2}).");
    }

    // ----------------------------------------------------------------
    // Particles: ambient steam/spark emitters placed along the floors so the
    // scene feels alive. Uses Unity's built-in ParticleSystem (no extra assets).
    // ----------------------------------------------------------------
    static void BuildParticles(Transform levelParent, Bounds levelBounds, List<FloorConfig> floors, StringBuilder sb)
    {
        var root = new GameObject("Particles").transform;
        root.SetParent(levelParent);
        int count = 0;
        foreach (var f in floors)
        {
            int emitters = 2;
            for (int i = 0; i < emitters; i++)
            {
                float x = Mathf.Lerp(f.StartX + Grid, f.Right - Grid, (i + 0.5f) / emitters);
                var go = new GameObject($"Steam_F{f.Index}_{i}");
                go.transform.SetParent(root);
                go.transform.position = new Vector3(x, f.SurfaceY + 0.2f, 0f);
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.startLifetime = 3.5f;
                main.startSpeed = 0.8f;
                main.startSize = 1.2f;
                main.startColor = new Color(0.8f, 0.85f, 0.9f, 0.25f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 40;
                var emission = ps.emission; emission.rateOverTime = 6f;
                var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 8f; shape.radius = 0.4f;
                go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // emit upward in 2D
                var col = ps.colorOverLifetime; col.enabled = true;
                var grad = new Gradient();
                grad.SetKeys(
                    new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.3f, 0.3f), new GradientAlphaKey(0f, 1f) });
                col.color = grad;
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.sortingOrder = 5;
                renderer.sharedMaterial = GetSoftParticleMaterial(); // FIX: was magenta (no material)
                count++;
            }
        }
        sb.AppendLine($"Particles: {count} steam emitters added.");
    }

    // ----------------------------------------------------------------
    // Collectibles: Corgi Engine pickable items (coin / stimpack). These prefabs carry
    // a trigger CircleCollider2D + PickableItem (+ CoinPicker / HealthPicker), so the
    // Rectangle character picks them up and triggers feedbacks at runtime.
    // ----------------------------------------------------------------
    static void BuildCollectibles(Transform levelParent, List<FloorConfig> floors, List<Shaft> shafts, StringBuilder sb)
    {
        var coinPrefab   = Load("Assets/CorgiEngine/Demos/Corgi2D/Prefabs/Items/coin.prefab");
        var healthPrefab = Load("Assets/CorgiEngine/Demos/Corgi2D/Prefabs/Items/stimpack.prefab");

        var root = new GameObject("Collectibles").transform;
        root.SetParent(levelParent);

        int coins = 0, healths = 0;
        const float walkY = 1.6f; // height above the deck surface (player reach)

        foreach (var floor in floors)
        {
            float left = floor.StartX + Grid * 1.2f;
            float right = floor.Right - Grid * 1.2f;

            if (_cfg.Coins && coinPrefab != null)
            {
                // (e) Place coins in SMALL CLUSTERS of at most TWO, with a clear gap between
                // clusters (no long rows of 3-5). Each cluster is a single coin or a tight
                // pair; clusters are spread along the floor so pickups feel deliberate.
                float span = right - left;
                int clusters = Mathf.Clamp(Mathf.RoundToInt(span / (Grid * 1.6f)), 1, 5);
                for (int cIdx = 0; cIdx < clusters; cIdx++)
                {
                    // Cluster centre, evenly spaced with a little jitter, well apart.
                    float baseX = Mathf.Lerp(left, right, (cIdx + 0.5f) / clusters);
                    float jitter = (float)(_rng.NextDouble() - 0.5) * Grid * 0.4f;
                    float cx0 = baseX + jitter;
                    int inCluster = Chance(0.5f) ? 2 : 1;   // never more than two together
                    for (int k = 0; k < inCluster; k++)
                    {
                        float cx = cx0 + k * 1.2f;
                        if (ShaftPassesThrough(shafts, floor.Index, cx)) continue;
                        var c = (GameObject)PrefabUtility.InstantiatePrefab(coinPrefab);
                        c.transform.SetParent(root);
                        c.transform.position = new Vector3(cx, floor.SurfaceY + walkY, 0f);
                        c.name = $"Coin_F{floor.Index}_{cIdx}_{k}";
                        coins++;
                    }
                }
            }

            if (_cfg.HealthItems && healthPrefab != null)
            {
                // One health pack per floor, placed away from the edges.
                float hx = Mathf.Lerp(left, right, 0.35f + 0.3f * (float)_rng.NextDouble());
                if (!ShaftPassesThrough(shafts, floor.Index, hx))
                {
                    var h = (GameObject)PrefabUtility.InstantiatePrefab(healthPrefab);
                    h.transform.SetParent(root);
                    h.transform.position = new Vector3(hx, floor.SurfaceY + walkY, 0f);
                    h.name = $"Health_F{floor.Index}";
                    healths++;
                }
            }
        }
        sb.AppendLine($"Collectibles: {coins} coins, {healths} health items (Corgi pickables).");
    }

    // ----------------------------------------------------------------
    // Enemies / NPCs. Instantiates the Corgi-wired character prefabs from
    // Assets/Game/Prefabs/Enemies, GROUNDED on each floor deck (using the prefab's own
    // collider so feet rest on the surface) and spaced apart. Honors the multi-select
    // EnemyTypes list (empty -> all). Avoids shaft carves and deck gaps.
    // ----------------------------------------------------------------
    static void BuildEnemies(Transform levelParent, List<FloorConfig> floors, List<Shaft> shafts, StringBuilder sb)
    {
        // Resolve the selected prefab paths (fall back to the whole catalog if none picked).
        var selected = new List<string>();
        if (_cfg.EnemyTypes != null && _cfg.EnemyTypes.Count > 0)
        {
            foreach (var key in _cfg.EnemyTypes)
                if (EnemyCatalog.TryGetValue(key, out var path)) selected.Add(path);
        }
        if (selected.Count == 0) selected.AddRange(EnemyCatalog.Values);

        // Load + measure each prefab's foot offset (distance from transform origin down to
        // the collider bottom) so we can rest it exactly on the deck surface.
        var prefabs = new List<GameObject>();
        var footOffset = new List<float>();
        foreach (var path in selected)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) { sb.AppendLine($"Enemies: MISSING prefab {path}"); continue; }
            prefabs.Add(go);
            footOffset.Add(ColliderFootOffset(go));
        }
        if (prefabs.Count == 0) { sb.AppendLine("Enemies: no valid prefabs; skipped."); return; }

        var root = new GameObject("Enemies").transform;
        root.SetParent(levelParent);

        int placed = 0;
        int perFloor = Mathf.Clamp(_cfg.EnemiesPerFloor, 1, 6);
        foreach (var floor in floors)
        {
            float left = floor.StartX + Grid * 1.5f;
            float right = floor.Right - Grid * 1.5f;
            for (int i = 0; i < perFloor; i++)
            {
                float ex = Mathf.Lerp(left, right, (i + 0.5f) / perFloor);
                ex += (float)(_rng.NextDouble() - 0.5) * Grid * 0.5f;
                // Skip if over a shaft carve or a deck gap (no ground to stand on).
                if (ShaftPassesThrough(shafts, floor.Index, ex)) continue;
                if (!HasDeckAt(floor, shafts, ex)) continue;

                int t = _rng.Next(prefabs.Count);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[t]);
                inst.transform.SetParent(root);
                inst.transform.position = new Vector3(ex, floor.SurfaceY + footOffset[t], 0f);
                inst.name = $"{prefabs[t].name}_F{floor.Index}_{i}";
                placed++;
            }
        }
        sb.AppendLine($"Enemies: {placed} placed from {prefabs.Count} type(s) [{string.Join(", ", selected.Select(System.IO.Path.GetFileNameWithoutExtension))}].");
    }

    // ----------------------------------------------------------------
    // (d) LEVEL EXIT. Picks ONE doorway on the TOP floor at RANDOM and turns it into a
    // working level-exit using Corgi's FinishLevel (a ButtonActivated trigger). The player
    // walks up to it and presses Interact to go to the next level. We make it OBVIOUS:
    // a bright green glow + "EXIT" prompt so it's easy to spot.
    // LevelName is set to this scene's own name so the exit is testable immediately
    // (it reloads the level); change LevelName to chain to a different scene.
    // ----------------------------------------------------------------
    static void BuildLevelExit(Transform levelParent, List<FloorConfig> floors, StringBuilder sb)
    {
        var top = floors[floors.Count - 1];
        // Randomly choose the LEFT or RIGHT doorway of the top floor.
        bool useRight = _rng.NextDouble() < 0.5;
        float doorX = useRight ? top.Right - Grid * 0.5f : top.StartX + Grid * 0.5f;
        float doorY = top.SurfaceY;

        var exit = new GameObject("LevelExit_Door");
        exit.transform.SetParent(levelParent);
        exit.transform.position = new Vector3(doorX, doorY + 1.6f, 0f);

        // Trigger collider covering the doorway opening.
        var box = exit.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2.4f, 3.6f);

        // Corgi FinishLevel: button-activated, prompts the player, then loads the level.
        var finish = exit.AddComponent<MoreMountains.CorgiEngine.FinishLevel>();
        finish.LevelName = System.IO.Path.GetFileNameWithoutExtension(_scenePath); // self -> testable
        finish.TriggerFade = true;
        finish.ButtonActivatedRequirement =
            MoreMountains.CorgiEngine.ButtonActivated.ButtonActivatedRequirements.Either;
        finish.RequiresButtonActivationAbility = false; // any player can use it
        finish.UseVisualPrompt = true;
        finish.ButtonPromptText = "EXIT";
        finish.ButtonPromptColor = new Color(0.2f, 1f, 0.2f, 1f);
        finish.ShowPromptWhenColliding = true;
        finish.AlwaysShowPrompt = true;   // floating green EXIT sign always visible (any light mode)
        finish.PromptRelativePosition = new Vector3(0f, 2.6f, 0f);

        // Bright green glow so the exit is unmistakable.
        var glowGO = new GameObject("ExitGlow");
        glowGO.transform.SetParent(exit.transform);
        glowGO.transform.localPosition = Vector3.zero;
        var light = glowGO.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.color = new Color(0.3f, 1f, 0.35f, 1f);
        light.intensity = 2.2f;
        light.pointLightOuterRadius = 4.5f;
        light.pointLightInnerRadius = 0.5f;

        sb.AppendLine($"Level Exit: top-floor {(useRight ? "RIGHT" : "LEFT")} door at x={doorX:F1} " +
                      $"-> FinishLevel('{finish.LevelName}'), green glow + EXIT prompt.");
    }

    // Distance from a prefab's transform origin UP to where it should sit so its collider
    // bottom rests on y=0. Returns +value (collider bottom is below origin by this much).
    static float ColliderFootOffset(GameObject go)
    {
        var box = go.GetComponentInChildren<BoxCollider2D>();
        if (box != null) return -(box.offset.y - box.size.y * 0.5f) * box.transform.lossyScale.y;
        var cap = go.GetComponentInChildren<CapsuleCollider2D>();
        if (cap != null) return -(cap.offset.y - cap.size.y * 0.5f) * cap.transform.lossyScale.y;
        var circ = go.GetComponentInChildren<CircleCollider2D>();
        if (circ != null) return -(circ.offset.y - circ.radius) * circ.transform.lossyScale.y;
        return 1f;
    }

    // True if floor has a solid deck tile at x (not a shaft carve / jump gap).
    static bool HasDeckAt(FloorConfig floor, List<Shaft> shafts, float x)
    {
        if (x < floor.StartX - Grid * 0.5f || x > floor.Right + Grid * 0.5f) return false;
        return !ShaftPassesThrough(shafts, floor.Index, x);
    }

    static Camera ConfigureCameras(GameObject cameraRig, GameObject uiCamera, StringBuilder sb)
    {
        Camera regular = null, ui = null;
        foreach (var cam in cameraRig.GetComponentsInChildren<Camera>(true))
            if (cam.name == "Regular Camera") regular = cam;
        ui = uiCamera.GetComponent<Camera>();
        if (regular == null || ui == null) { sb.AppendLine("WARN: cameras not found for stack fix."); return regular; }

        var regularData = regular.GetUniversalAdditionalCameraData();
        var uiData = ui.GetUniversalAdditionalCameraData();
        regularData.renderType = CameraRenderType.Base;
        uiData.renderType = CameraRenderType.Overlay;
        regularData.cameraStack.Clear();
        regularData.cameraStack.Add(ui);
        sb.AppendLine("URP camera stack configured (Base + UI Overlay).");
        return regular;
    }

    static void SetupRealLights(Camera regularCam, GameObject uiInstance, StringBuilder sb)
    {
        // Create (or load) a 2D Renderer asset and add it to the active URP asset's list,
        // then point the playground cameras at it so Light2D renders WITHOUT changing the
        // main game's default forward renderer (index 0).
        var rp = UniversalRenderPipeline.asset;
        if (rp == null) { sb.AppendLine("WARN: no active URP asset; real lights skipped."); return; }

        var renderer2D = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(Renderer2DPath);
        if (renderer2D == null)
        {
            renderer2D = ScriptableObject.CreateInstance<Renderer2DData>();
            AssetDatabase.CreateAsset(renderer2D, Renderer2DPath);
            AssetDatabase.SaveAssets();
        }

        int index = AddRendererToPipeline(rp, renderer2D);
        if (index < 0) { sb.AppendLine("WARN: could not register 2D renderer; real lights may not show."); return; }

        var regData = regularCam.GetUniversalAdditionalCameraData();
        regData.SetRenderer(index);
        if (uiInstance != null)
        {
            var uiData = uiInstance.GetComponent<Camera>().GetUniversalAdditionalCameraData();
            uiData.SetRenderer(index);
        }
        // global light so the scene isn't pitch black (0 = none, configurable)
        if (_cfg.GlobalLightIntensity > 0f)
        {
            var gl = new GameObject("GlobalLight2D");
            var global = gl.AddComponent<Light2D>();
            global.lightType = Light2D.LightType.Global;
            global.intensity = _cfg.GlobalLightIntensity;
            global.color = _cfg.GlobalLightColor;
        }
        sb.AppendLine($"Real lights: 2D renderer registered at index {index}, cameras switched, global={_cfg.GlobalLightIntensity:F2}.");
    }

    // Adds a renderer to the URP asset's m_RendererDataList via SerializedObject; returns its index.
    static int AddRendererToPipeline(UniversalRenderPipelineAsset rp, ScriptableRendererData data)
    {
        var so = new SerializedObject(rp);
        var list = so.FindProperty("m_RendererDataList");
        if (list == null) return -1;
        // already present?
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == data) return i;
        int idx = list.arraySize;
        list.InsertArrayElementAtIndex(idx);
        list.GetArrayElementAtIndex(idx).objectReferenceValue = data;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(rp);
        AssetDatabase.SaveAssets();
        return idx;
    }
}
