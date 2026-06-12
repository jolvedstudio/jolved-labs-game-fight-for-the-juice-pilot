using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;

/// <summary>
/// Builds a multi-floor, fully functional Corgi Engine level from 2D_Pack art.
///
/// Conventions (matching the 2D_Pack Demo scene):
///   - All art at z=0 on the "Default" sorting layer; depth via sortingOrder bands:
///       -6 far background wall, -4 background accents, -3 pipes,
///       -2 doors / bulkheads, 0 walkable platforms, +2 on-floor props, +3 edge trim.
///   - Everything tiles on a 5.12 grid; floors are spaced FloorSpacing = 10.24
///     (= two panels) so platform decks, background panels and 5.12-tall doorways
///     all tile FLUSH (a doorway tucks exactly under the deck of the floor above).
///
/// Inter-floor movement:
///   - A static RAMP CHAIN (two Platform_Ramp_45 corner-to-corner) bridges Floor 0 -> 1.
///   - A MOVING ELEVATOR (Hover_Platform + Corgi MovingPlatform) bridges Floor 1 -> 2.
///   - An EXPRESS ELEVATOR spans TWO floors (Floor 0 -> 2) through the shaft on the
///     left where Floor 1 does not extend.
///
/// Variety: a seeded System.Random varies ground-tile variants, doorway types,
/// pipe layouts and prop selection/placement per floor (no "copy up").
///
/// This script is also the reference "SceneAssembler" for the level generator.
/// </summary>
public class BuildCorgiLevel
{
    const int LayerPlatforms = 8;
    const int LayerMovingPlatforms = 18;
    const string Folder = "Assets/_CorgiPlayground";
    const string ScenePath = "Assets/_CorgiPlayground/Scenes/CorgiPlayground_Level1.unity";

    const float Grid = 5.12f;          // base tile size in world units
    const float FloorSpacing = 10.24f; // two panels -> flush platform/panel/door tiling
    const int Seed = 20260612;

    static System.Random _rng;

    static string P(string rel) => $"Assets/2D_Pack/!Prefabs/{rel}.prefab";
    static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);
    static T Pick<T>(IList<T> list) => list[_rng.Next(list.Count)];
    static bool Chance(float p) => _rng.NextDouble() < p;

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform) SetLayerRecursive(t.gameObject, layer);
    }

    static void SetSortingOrderRecursive(GameObject go, int order)
    {
        foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            sr.sortingOrder = order;
    }

    // Top-surface local offset of a platform prefab from its EdgeCollider2D.
    static float TopOffset(GameObject inst)
    {
        var edge = inst.GetComponent<EdgeCollider2D>();
        if (edge == null) return 0f;
        float maxY = float.MinValue;
        foreach (var p in edge.points) maxY = Mathf.Max(maxY, p.y);
        return edge.offset.y + maxY;
    }

    // ---- A floor's content description (the generator's FloorPlanner output) ----
    class FloorConfig
    {
        public int Index;
        public float SurfaceY;
        public float StartX;
        public int GroundTiles;
        public string[] GroundVariants;
        public float Right => StartX + (GroundTiles - 1) * Grid;
        public float Width => GroundTiles * Grid;
    }

    public static string Execute()
    {
        _rng = new System.Random(Seed);
        var sb = new StringBuilder();

        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "_CorgiPlayground");
        if (!AssetDatabase.IsValidFolder(Folder + "/Scenes")) AssetDatabase.CreateFolder(Folder, "Scenes");

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
        camInstance.transform.position = new Vector3(8, 8, -10);
        FixUrpCameraStack(camInstance, uiInstance, sb);

        // ---------------- Scene roots ----------------
        var level      = new GameObject("Level");
        var background = new GameObject("Background"); background.transform.SetParent(level.transform);
        var floorsRoot = new GameObject("Floors");     floorsRoot.transform.SetParent(level.transform);
        var decorRoot  = new GameObject("Decoration"); decorRoot.transform.SetParent(level.transform);
        var moversRoot = new GameObject("Movers");     moversRoot.transform.SetParent(level.transform);

        var foreground = new List<GameObject>();

        // ---------------- Floor layout (variable lengths + offsets, no copy-up) ----------------
        // Floor 1 deliberately starts at x=10.24 so the left shaft (x 0..10.24) is open
        // for the express elevator that spans Floor 0 -> Floor 2.
        var floors = new List<FloorConfig>
        {
            new FloorConfig{ Index=0, SurfaceY=0f,                StartX=0f,     GroundTiles=12, GroundVariants=new[]{"Platforms/Platform_1","Platforms/Platform_2","Platforms/Platform_3"} },
            new FloorConfig{ Index=1, SurfaceY=FloorSpacing,      StartX=Grid*2f, GroundTiles=8,  GroundVariants=new[]{"Platforms/Platform_3","Platforms/Platform_4"} },
            new FloorConfig{ Index=2, SurfaceY=FloorSpacing*2f,   StartX=Grid,    GroundTiles=11, GroundVariants=new[]{"Platforms/Platform_5","Platforms/Platform_2","Platforms/Platform_4"} },
        };

        float worldLeft = 0f, worldRight = float.MinValue;
        foreach (var f in floors) worldRight = Mathf.Max(worldRight, f.Right + Grid);

        // ---------------- Background (flush, grid-aligned) ----------------
        float bgYMin = -Grid;                                  // one row below floor 0
        float bgYMax = floors[floors.Count - 1].SurfaceY + Grid * 2f;
        BuildBackground(background.transform, worldLeft - Grid, worldRight + Grid, bgYMin, bgYMax);

        // ---------------- Floors + decoration ----------------
        foreach (var floor in floors)
        {
            var floorGO = new GameObject($"Floor_{floor.Index}");
            floorGO.transform.SetParent(floorsRoot.transform);

            for (int i = 0; i < floor.GroundTiles; i++)
            {
                string variant = floor.GroundVariants[i % floor.GroundVariants.Length];
                var g = PlaceBySurface(P(variant), floor.StartX + i * Grid, floor.SurfaceY, floorGO.transform, $"Ground_{i}");
                SetLayerRecursive(g, LayerPlatforms);
                SetSortingOrderRecursive(g, 0);
                foreground.Add(g);
            }

            DecorateFloor(floor, decorRoot.transform);
        }

        // ---------------- Intra-floor challenge on Floor 2 (floating steps) ----------------
        {
            var f = floors[2];
            float y = f.SurfaceY;
            float sx = f.StartX + Grid * 4f;
            var s1 = PlaceBySurface(P("Platforms/Platform_9"), sx,          y + 2.0f, floorsRoot.transform, "Step_F2_A"); SetLayerRecursive(s1, LayerPlatforms); SetSortingOrderRecursive(s1,0); foreground.Add(s1);
            var s2 = PlaceBySurface(P("Platforms/Platform_9"), sx + Grid,   y + 3.6f, floorsRoot.transform, "Step_F2_B"); SetLayerRecursive(s2, LayerPlatforms); SetSortingOrderRecursive(s2,0); foreground.Add(s2);
        }

        // ---------------- Inter-floor connectors ----------------
        // (A) Static RAMP CHAIN: Floor 0 -> Floor 1 (two Ramp_45 corner-to-corner)
        BuildRampChain(floorsRoot.transform, floors[0], floors[1], floors[1].StartX + Grid * 1.0f, foreground);

        // (B) MOVING ELEVATOR: Floor 1 -> Floor 2 (single-floor travel)
        var elevB = BuildElevator(moversRoot.transform, "Elevator_F1_F2",
            new Vector3(floors[1].Right - Grid * 0.5f, floors[1].SurfaceY, 0f), FloorSpacing);
        foreground.Add(elevB);

        // (C) EXPRESS ELEVATOR spanning TWO floors: Floor 0 -> Floor 2 through the left shaft
        var elevExpress = BuildElevator(moversRoot.transform, "Elevator_Express_F0_F2",
            new Vector3(Grid * 1.0f, floors[0].SurfaceY, 0f), FloorSpacing * 2f);
        foreground.Add(elevExpress);

        // ---------------- Spawn + LevelManager ----------------
        var spawnInstance = (GameObject)PrefabUtility.InstantiatePrefab(levelStart);
        spawnInstance.name = "LevelStart";
        spawnInstance.transform.SetParent(level.transform);
        spawnInstance.transform.position = new Vector3(Grid * 4f, floors[0].SurfaceY + 4f, 0f);
        var checkPoint = spawnInstance.GetComponent<CheckPoint>();

        var levelManagerGO = new GameObject("LevelManager");
        var levelManager = levelManagerGO.AddComponent<LevelManager>();
        levelManager.PlayerPrefabs = new Character[] { rectangle.GetComponent<Character>() };
        levelManager.AutoAttributePlayerIDs = true;
        levelManager.DebugSpawn = checkPoint;
        levelManager.BoundsMode = LevelManager.BoundsModes.TwoD;

        // ---------------- Level bounds ----------------
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

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        sb.AppendLine($"Scene saved={saved} at {ScenePath}");
        sb.AppendLine($"Floors={floors.Count} (lengths {floors[0].GroundTiles}/{floors[1].GroundTiles}/{floors[2].GroundTiles}), foreground objects={foreground.Count}");
        sb.AppendLine($"Connectors: Ramp(0->1), Elevator(1->2), Express Elevator(0->2)");
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

    static GameObject PlaceDecorByBottom(string prefabPath, float centerX, float bottomY, Transform parent, int sortingOrder, string name = null)
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

    static GameObject PlaceDecorByCenter(string prefabPath, float centerX, float centerY, Transform parent, int sortingOrder, string name = null)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(Load(prefabPath));
        inst.transform.SetParent(parent);
        inst.transform.position = new Vector3(centerX, centerY, 0f);
        SetSortingOrderRecursive(inst, sortingOrder);
        if (!string.IsNullOrEmpty(name)) inst.name = name;
        return inst;
    }

    // ----------------------------------------------------------------
    // Background wall (grid-aligned so floors/doors are flush)
    // ----------------------------------------------------------------
    static void BuildBackground(Transform parent, float xMin, float xMax, float yMin, float yMax)
    {
        string[] farPanels = { "Panels/Panel_Back_UnLit", "Panels/Panel_Back_Lit", "Panels/Panel_Back_Lit_3" };
        // snap to grid
        xMin = Mathf.Floor(xMin / Grid) * Grid;
        yMin = Mathf.Floor(yMin / Grid) * Grid;
        int col = 0;
        for (float x = xMin; x < xMax; x += Grid, col++)
        {
            int row = 0;
            for (float y = yMin; y < yMax; y += Grid, row++)
            {
                string panel = farPanels[(col * 3 + row) % farPanels.Length];
                PlaceDecorByCenter(P(panel), x + Grid * 0.5f, y + Grid * 0.5f, parent, -6, "BG");
            }
        }
        // near accent band of tech panels, randomized
        string[] accents = { "Panels/Panel_Tech_6", "Panels/Panel_Back_Lit_2", "Panels/Panel_3_Clear" };
        for (float x = xMin + Grid; x < xMax - Grid; x += Grid * 2f)
        {
            if (!Chance(0.6f)) continue;
            string a = accents[_rng.Next(accents.Length)];
            float y = yMin + Grid * (1 + _rng.Next(3));
            PlaceDecorByCenter(P(a), x, y + Grid * 0.5f, parent, -4, "BG_Accent");
        }
    }

    // ----------------------------------------------------------------
    // Per-floor decoration (varied per floor via RNG, all collider-free)
    // ----------------------------------------------------------------
    static void DecorateFloor(FloorConfig floor, Transform parent)
    {
        var floorDecor = new GameObject($"Decor_Floor_{floor.Index}");
        floorDecor.transform.SetParent(parent);
        var t = floorDecor.transform;
        float surface = floor.SurfaceY;
        float left = floor.StartX;
        float right = floor.Right;
        float headroom = surface + FloorSpacing * 0.5f; // visible mid-band of the floor

        // --- Doorways: vary type per floor, placed flush on the surface (tuck under deck above) ---
        string[] doorTypes = { "Doors/Doorway_Front_Large", "Doors/Doorway_Front_Small", "Doors/Doorway_Side" };
        string doorL = doorTypes[_rng.Next(doorTypes.Length)];
        string doorR = doorTypes[_rng.Next(doorTypes.Length)];
        PlaceDecorByBottom(P(doorL), left + Grid * 0.5f, surface, t, -2, "Doorway_Left");
        PlaceDecorByBottom(P(doorR), right - Grid * 0.5f, surface, t, -2, "Doorway_Right");

        // --- Bulkhead dividers: random count/positions ---
        int bulkheads = 1 + _rng.Next(2);
        for (int i = 0; i < bulkheads; i++)
        {
            float bx = Mathf.Lerp(left + Grid * 2f, right - Grid * 2f, (i + 0.5f) / bulkheads);
            PlaceDecorByBottom(P("Props/Bulkhead_4_B"), bx, surface, t, -2, $"Bulkhead_{i}");
        }

        // --- Pipes: placed in the OPEN HEADROOM band so they are clearly visible ---
        string[] pipeTypes = { "Pipes/Pipe_1", "Pipes/Pipe_Small_Straight", "Pipes/Pipe_Angle" };
        int pipeRuns = 2 + _rng.Next(2);
        for (int i = 0; i < pipeRuns; i++)
        {
            string pipe = pipeTypes[_rng.Next(pipeTypes.Length)];
            float px = Mathf.Lerp(left + Grid, right - Grid, (i + 0.5f) / pipeRuns) + (float)(_rng.NextDouble() - 0.5) * Grid;
            float py = headroom - 0.6f + (float)(_rng.NextDouble() - 0.5f) * 1.0f; // mid-band, stays below the deck above
            PlaceDecorByCenter(P(pipe), px, py, t, -3, $"Pipe_{i}");
        }
        if (Chance(0.7f))
            PlaceDecorByCenter(P("Pipes/Pipe_Valve_1"), Mathf.Lerp(left, right, 0.5f), headroom - 0.6f, t, -3, "Pipe_Valve");

        // --- On-floor props: random selection + positions across the floor ---
        string[] propTypes = { "Props/Console_1", "Props/Console_2", "Props/Barrel_Red", "Props/Barrel_Blue",
                               "Props/Box", "Props/Box_B", "Props/Machine" };
        int props = 3 + _rng.Next(4);
        var usedX = new List<float>();
        for (int i = 0; i < props; i++)
        {
            string prop = propTypes[_rng.Next(propTypes.Length)];
            float px = left + Grid * (0.8f + (float)_rng.NextDouble() * (floor.GroundTiles - 1.6f));
            PlaceDecorByBottom(P(prop), px, surface, t, 2, $"Prop_{i}");
        }

        // --- Foreground trim: border strip along the whole edge + occasional railing ---
        for (int i = 0; i < floor.GroundTiles; i++)
            PlaceDecorByBottom(P("Borders/Border_4"), left + i * Grid, surface, t, 3, $"Border_{i}");
        if (Chance(0.8f))
            PlaceDecorByBottom(P("Props/Railing_Top"), left + Grid * (1 + _rng.Next(Mathf.Max(1, floor.GroundTiles - 2))), surface, t, 3, "Railing");
    }

    // ----------------------------------------------------------------
    // Ramp chain: two Platform_Ramp_45 connected corner-to-corner to climb a full floor.
    // ----------------------------------------------------------------
    static void BuildRampChain(Transform parent, FloorConfig from, FloorConfig to, float baseX, List<GameObject> foreground)
    {
        // Ramp_45 collider spans corner (-2.56,-2.56) -> (2.56,2.56) => rises Grid over Grid.
        float half = Grid * 0.5f;
        float midY = from.SurfaceY + half; // first ramp center: bottom at floor 'from' surface
        var r1 = (GameObject)PrefabUtility.InstantiatePrefab(Load(P("Platforms/Platform_Ramp_45")));
        r1.name = "Ramp_F0_F1_A";
        r1.transform.SetParent(parent);
        r1.transform.position = new Vector3(baseX, midY, 0f);
        SetLayerRecursive(r1, LayerPlatforms); SetSortingOrderRecursive(r1, 0);
        foreground.Add(r1);

        var r2 = (GameObject)PrefabUtility.InstantiatePrefab(Load(P("Platforms/Platform_Ramp_45")));
        r2.name = "Ramp_F0_F1_B";
        r2.transform.SetParent(parent);
        // second ramp bottom-left corner coincides with first ramp top-right corner
        r2.transform.position = new Vector3(baseX + Grid, midY + Grid, 0f);
        SetLayerRecursive(r2, LayerPlatforms); SetSortingOrderRecursive(r2, 0);
        foreground.Add(r2);
    }

    // ----------------------------------------------------------------
    // Elevator / moving platform built from 2D_Pack art + Corgi MovingPlatform
    // ----------------------------------------------------------------
    static GameObject BuildElevator(Transform parent, string name, Vector3 bottomSurfacePos, float travel)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(Load(P("Platforms/Hover_Platform")));
        inst.name = name;
        inst.transform.SetParent(parent);
        float top = TopOffset(inst);
        inst.transform.position = new Vector3(bottomSurfacePos.x, bottomSurfacePos.y - top, 0f);
        SetLayerRecursive(inst, LayerMovingPlatforms);
        SetSortingOrderRecursive(inst, 1);

        // Moving platforms must not be static.
        foreach (var tr in inst.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(tr.gameObject, 0);

        var rb = inst.GetComponent<Rigidbody2D>();
        if (rb == null) rb = inst.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var mp = inst.GetComponent<MovingPlatform>();
        if (mp == null) mp = inst.AddComponent<MovingPlatform>();
        mp.CycleOption = MMPathMovement.CycleOptions.BackAndForth;
        mp.MovementSpeed = 3f;
        mp.PathElements = new List<MMPathMovementElement>
        {
            new MMPathMovementElement{ PathElementPosition = new Vector3(0, 0, 0), Delay = 1f },
            new MMPathMovementElement{ PathElementPosition = new Vector3(0, travel, 0), Delay = 1f },
        };
        return inst;
    }

    // ----------------------------------------------------------------
    // URP camera stack (game camera Base + UI camera Overlay)
    // ----------------------------------------------------------------
    static void FixUrpCameraStack(GameObject cameraRig, GameObject uiCamera, StringBuilder sb)
    {
        Camera regular = null, ui = null;
        foreach (var cam in cameraRig.GetComponentsInChildren<Camera>(true))
            if (cam.name == "Regular Camera") regular = cam;
        ui = uiCamera.GetComponent<Camera>();
        if (regular == null || ui == null) { sb.AppendLine("WARN: cameras not found for URP stack fix."); return; }

        var regularData = regular.GetUniversalAdditionalCameraData();
        var uiData = ui.GetUniversalAdditionalCameraData();
        regularData.renderType = CameraRenderType.Base;
        uiData.renderType = CameraRenderType.Overlay;
        regularData.cameraStack.Clear();
        regularData.cameraStack.Add(ui);
        sb.AppendLine("URP camera stack configured (Base + UI Overlay).");
    }
}
