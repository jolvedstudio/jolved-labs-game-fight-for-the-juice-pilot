using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Builds a multi-floor, fully functional Corgi Engine level from 2D_Pack art.
/// Layers (by intent):
///   - Foreground walkable geometry -> Unity layer "Platforms" (8), Default sorting order 0
///   - Moving / elevator platforms   -> Unity layer "MovingPlatforms" (18)
///   - Background panels (far)        -> Default sorting order -6
///   - Background accents (near)      -> Default sorting order -4
///   - Structural decor (doors/bulkheads/pipes) -> order -2
///   - On-floor props (consoles/barrels/boxes)  -> order +2
///   - Foreground trim (railings/borders)       -> order +3
/// This script is also the reference "SceneAssembler" for the level generator:
/// the FloorConfig list below is the data the generator's LayoutPlanner would emit.
/// </summary>
public class BuildCorgiLevel
{
    const int LayerPlatforms = 8;
    const int LayerMovingPlatforms = 18;
    const string Folder = "Assets/_CorgiPlayground";
    const string ScenePath = "Assets/_CorgiPlayground/Scenes/CorgiPlayground_Level1.unity";

    const float Grid = 5.12f;       // base tile size in world units
    const float FloorSpacing = 11f; // vertical distance between floor surfaces

    static string P(string rel) => $"Assets/2D_Pack/!Prefabs/{rel}.prefab";
    static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

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

    // Returns the top-surface local offset of a platform prefab from its EdgeCollider2D.
    static float TopOffset(GameObject inst)
    {
        var edge = inst.GetComponent<EdgeCollider2D>();
        if (edge == null) return 0f;
        float maxY = float.MinValue;
        foreach (var p in edge.points) maxY = Mathf.Max(maxY, p.y);
        return edge.offset.y + maxY;
    }

    // ---- A floor's content description (generator would produce this) ----
    class FloorConfig
    {
        public float SurfaceY;
        public int GroundTiles;       // number of 5.12 ground tiles
        public float StartX;
        public string[] GroundVariants; // cycle through these platform prefabs
    }

    public static string Execute()
    {
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

        // ---- URP camera-stack fix (UICamera as Overlay on the game camera) ----
        FixUrpCameraStack(camInstance, uiInstance, sb);

        // ---------------- Scene roots ----------------
        var level      = new GameObject("Level");
        var background = new GameObject("Background"); background.transform.SetParent(level.transform);
        var floorsRoot = new GameObject("Floors");     floorsRoot.transform.SetParent(level.transform);
        var decorRoot  = new GameObject("Decoration"); decorRoot.transform.SetParent(level.transform);
        var moversRoot = new GameObject("Movers");     moversRoot.transform.SetParent(level.transform);

        var foreground = new List<GameObject>(); // used for bounds

        // ---------------- Floor layout (generator data) ----------------
        var floors = new List<FloorConfig>
        {
            new FloorConfig{ SurfaceY = 0f,               StartX = 0f, GroundTiles = 10, GroundVariants = new[]{"Platforms/Platform_1","Platforms/Platform_2","Platforms/Platform_3"} },
            new FloorConfig{ SurfaceY = FloorSpacing,      StartX = 0f, GroundTiles = 10, GroundVariants = new[]{"Platforms/Platform_3","Platforms/Platform_4","Platforms/Platform_1"} },
            new FloorConfig{ SurfaceY = FloorSpacing * 2f, StartX = 0f, GroundTiles = 10, GroundVariants = new[]{"Platforms/Platform_2","Platforms/Platform_5","Platforms/Platform_4"} },
        };
        float levelWidth = floors[0].GroundTiles * Grid;

        // ---------------- Background wall (tiled panels) ----------------
        BuildBackground(background.transform, -3f, levelWidth + 3f,
                        floors[0].SurfaceY - 4f, floors[floors.Count - 1].SurfaceY + Grid + 2f);

        // ---------------- Build each floor ----------------
        for (int f = 0; f < floors.Count; f++)
        {
            var floor = floors[f];
            var floorGO = new GameObject($"Floor_{f}");
            floorGO.transform.SetParent(floorsRoot.transform);

            // Ground run
            for (int i = 0; i < floor.GroundTiles; i++)
            {
                string variant = floor.GroundVariants[i % floor.GroundVariants.Length];
                var g = PlaceBySurface(P(variant), floor.StartX + i * Grid, floor.SurfaceY, floorGO.transform, $"Ground_{i}");
                SetLayerRecursive(g, LayerPlatforms);
                SetSortingOrderRecursive(g, 0);
                foreground.Add(g);
            }

            // Decorate the floor
            DecorateFloor(floor, f, decorRoot.transform);
        }

        // ---------------- Intra-floor traversal challenge (floating steps on floor 1) ----------------
        {
            float y = floors[1].SurfaceY;
            var s1 = PlaceBySurface(P("Platforms/Platform_9"), 24f, y + 2.2f, floorsRoot.transform, "Step_F1_A"); SetLayerRecursive(s1, LayerPlatforms); SetSortingOrderRecursive(s1,0); foreground.Add(s1);
            var s2 = PlaceBySurface(P("Platforms/Platform_9"), 30f, y + 4.0f, floorsRoot.transform, "Step_F1_B"); SetLayerRecursive(s2, LayerPlatforms); SetSortingOrderRecursive(s2,0); foreground.Add(s2);
        }

        // ---------------- Inter-floor movement ----------------
        // Elevator (moving platform) right side: Floor 0 -> Floor 1
        var elevA = BuildElevator(moversRoot.transform, "Elevator_F0_F1",
            new Vector3(levelWidth - Grid * 0.5f, floors[0].SurfaceY, 0f), FloorSpacing);
        foreground.Add(elevA);

        // Ramp on Floor 1 going up a bit then Elevator left side: Floor 1 -> Floor 2
        var elevB = BuildElevator(moversRoot.transform, "Elevator_F1_F2",
            new Vector3(Grid * 0.5f, floors[1].SurfaceY, 0f), FloorSpacing);
        foreground.Add(elevB);

        // A static ramp linking the lower portion of floor 0 up onto a small ledge (variety)
        var ramp = (GameObject)PrefabUtility.InstantiatePrefab(Load(P("Platforms/Platform_Ramp_45")));
        ramp.transform.SetParent(floorsRoot.transform);
        ramp.transform.position = new Vector3(levelWidth - Grid * 2.6f, floors[0].SurfaceY + 2.56f, 0f);
        ramp.name = "Ramp_F0";
        SetLayerRecursive(ramp, LayerPlatforms); SetSortingOrderRecursive(ramp, 0);
        foreground.Add(ramp);

        // ---------------- Spawn + LevelManager ----------------
        var spawnInstance = (GameObject)PrefabUtility.InstantiatePrefab(levelStart);
        spawnInstance.name = "LevelStart";
        spawnInstance.transform.SetParent(level.transform);
        spawnInstance.transform.position = new Vector3(3f, floors[0].SurfaceY + 4f, 0f);
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
        b.Expand(new Vector3(4f, 12f, 10f)); // headroom + fall-death floor + side padding
        levelManager.LevelBounds = new Bounds(b.center, b.size);

        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        sb.AppendLine($"Scene saved={saved} at {ScenePath}");
        sb.AppendLine($"Floors={floors.Count}, foreground objects={foreground.Count}");
        sb.AppendLine($"LevelBounds center={levelManager.LevelBounds.center} size={levelManager.LevelBounds.size}");
        return sb.ToString();
    }

    // ----------------------------------------------------------------
    // Placement helpers
    // ----------------------------------------------------------------

    // Places a platform prefab so its walkable top surface is at world Y = surfaceY.
    static GameObject PlaceBySurface(string prefabPath, float centerX, float surfaceY, Transform parent, string name)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(Load(prefabPath));
        float top = TopOffset(inst);
        inst.transform.SetParent(parent);
        inst.transform.position = new Vector3(centerX, surfaceY - top, 0f);
        if (!string.IsNullOrEmpty(name)) inst.name = name;
        return inst;
    }

    // Places a sprite-only decor prefab by its BOTTOM edge at world Y = bottomY.
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

    // Places a decor prefab by its CENTER.
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
    // Background wall
    // ----------------------------------------------------------------
    static void BuildBackground(Transform parent, float xMin, float xMax, float yMin, float yMax)
    {
        // far solid wall
        string[] farPanels = { "Panels/Panel_Back_UnLit", "Panels/Panel_Back_Lit", "Panels/Panel_Back_Lit_3" };
        int col = 0;
        for (float x = xMin; x < xMax; x += Grid, col++)
        {
            int row = 0;
            for (float y = yMin; y < yMax; y += Grid, row++)
            {
                string panel = farPanels[(col + row) % farPanels.Length];
                PlaceDecorByCenter(P(panel), x + Grid * 0.5f, y + Grid * 0.5f, parent, -6, "BG");
            }
        }
        // near accent band of tech panels every few columns
        int accent = 0;
        for (float x = xMin + Grid; x < xMax - Grid; x += Grid * 2f, accent++)
        {
            string tech = (accent % 2 == 0) ? "Panels/Panel_Tech_6" : "Panels/Panel_Back_Lit_2";
            float y = yMin + Grid * (1 + (accent % 3));
            PlaceDecorByCenter(P(tech), x, y, parent, -4, "BG_Accent");
        }
    }

    // ----------------------------------------------------------------
    // Per-floor decoration
    // ----------------------------------------------------------------
    static void DecorateFloor(FloorConfig floor, int index, Transform parent)
    {
        var floorDecor = new GameObject($"Decor_Floor_{index}");
        floorDecor.transform.SetParent(parent);
        var t = floorDecor.transform;
        float surface = floor.SurfaceY;
        float right = floor.StartX + (floor.GroundTiles - 1) * Grid;

        // Doorways at both ends (structural background)
        PlaceDecorByBottom(P("Doors/Doorway_Front_Large"), floor.StartX + Grid * 0.5f, surface, t, -2, "Doorway_Left");
        PlaceDecorByBottom(P("Doors/Doorway_Front_Large"), right - Grid * 0.5f, surface, t, -2, "Doorway_Right");

        // Vertical bulkhead dividers
        PlaceDecorByBottom(P("Props/Bulkhead_4_B"), floor.StartX + Grid * 3.5f, surface, t, -2, "Bulkhead_A");
        PlaceDecorByBottom(P("Props/Bulkhead_4_B"), floor.StartX + Grid * 6.5f, surface, t, -2, "Bulkhead_B");

        // Pipes running near the ceiling of the floor
        PlaceDecorByCenter(P("Pipes/Pipe_1"), floor.StartX + Grid * 2.0f, surface + FloorSpacing - 1.4f, t, -3, "Pipe_A");
        PlaceDecorByCenter(P("Pipes/Pipe_1"), floor.StartX + Grid * 5.0f, surface + FloorSpacing - 1.4f, t, -3, "Pipe_B");
        PlaceDecorByCenter(P("Pipes/Pipe_Valve_1"), floor.StartX + Grid * 3.5f, surface + FloorSpacing - 1.4f, t, -3, "Pipe_Valve");

        // On-floor props (sit on the surface, in front of platforms)
        PlaceDecorByBottom(P("Props/Console_1"), floor.StartX + Grid * 1.2f, surface, t, 2, "Console");
        PlaceDecorByBottom(P("Props/Console_2"), floor.StartX + Grid * 1.6f, surface, t, 2, "Console2");
        PlaceDecorByBottom(P("Props/Barrel_Red"), floor.StartX + Grid * 4.4f, surface, t, 2, "Barrel");
        PlaceDecorByBottom(P("Props/Barrel_Blue"), floor.StartX + Grid * 4.8f, surface, t, 2, "Barrel2");
        PlaceDecorByBottom(P("Props/Box"), floor.StartX + Grid * 7.2f, surface, t, 2, "Box");
        PlaceDecorByBottom(P("Props/Box_B"), floor.StartX + Grid * 7.6f, surface, t, 2, "Box2");
        PlaceDecorByBottom(P("Props/Machine"), right - Grid * 1.4f, surface, t, 2, "Machine");

        // Foreground trim: border strip along the floor edge + railing accents
        for (int i = 0; i < floor.GroundTiles; i++)
            PlaceDecorByBottom(P("Borders/Border_4"), floor.StartX + i * Grid, surface, t, 3, $"Border_{i}");
        PlaceDecorByBottom(P("Props/Railing_Top"), floor.StartX + Grid * 8.5f, surface, t, 3, "Railing");
    }

    // ----------------------------------------------------------------
    // Elevator / moving platform built from 2D_Pack art + Corgi MovingPlatform
    // ----------------------------------------------------------------
    static GameObject BuildElevator(Transform parent, string name, Vector3 bottomSurfacePos, float travel)
    {
        // Use the themed Hover_Platform (has an EdgeCollider2D top surface + sprite).
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(Load(P("Platforms/Hover_Platform")));
        inst.name = name;
        inst.transform.SetParent(parent);
        float top = TopOffset(inst);
        // Place so the platform's top surface starts at the bottom floor's surface.
        inst.transform.position = new Vector3(bottomSurfacePos.x, bottomSurfacePos.y - top, 0f);
        SetLayerRecursive(inst, LayerMovingPlatforms);
        SetSortingOrderRecursive(inst, 1);
        // Moving platforms must not be static or they won't move.
        foreach (var tr in inst.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(tr.gameObject, 0);

        // Kinematic rigidbody required by MMPathMovement-driven platforms.
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
        regularData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
        uiData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Overlay;
        regularData.cameraStack.Clear();
        regularData.cameraStack.Add(ui);
        sb.AppendLine("URP camera stack configured (Base + UI Overlay).");
    }
}
