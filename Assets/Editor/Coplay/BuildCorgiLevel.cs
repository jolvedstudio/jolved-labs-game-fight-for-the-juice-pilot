using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoreMountains.CorgiEngine;

public class BuildCorgiLevel
{
    const int LayerPlatforms = 8;   // "Platforms"
    const string Folder = "Assets/_CorgiPlayground";
    const string ScenePath = "Assets/_CorgiPlayground/Scenes/CorgiPlayground_Level1.unity";

    static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(path);

    static void SetLayerRecursive(GameObject go, int layer)
    {
        go.layer = layer;
        foreach (Transform t in go.transform)
            SetLayerRecursive(t.gameObject, layer);
    }

    public static string Execute()
    {
        var sb = new StringBuilder();

        // Ensure folders
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "_CorgiPlayground");
        if (!AssetDatabase.IsValidFolder(Folder + "/Scenes"))
            AssetDatabase.CreateFolder(Folder, "Scenes");

        // New empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---- Managers ----
        var gameManagers = Load("Assets/CorgiEngine/Common/Prefabs/LevelManagers/GameManagers.prefab");
        var soundManager = Load("Assets/CorgiEngine/Common/Prefabs/LevelManagers/MMSoundManager.prefab");
        var uiCamera      = Load("Assets/CorgiEngine/Common/Prefabs/GUI/UICamera.prefab");
        var cameraRig     = Load("Assets/CorgiEngine/Demos/Minimal/Prefabs/Camera/MinimalCameraRig.prefab");
        var levelStart    = Load("Assets/CorgiEngine/Common/Prefabs/LevelManagers/LevelStart.prefab");
        var rectangle     = Load("Assets/CorgiEngine/Demos/Minimal/Prefabs/PlayableCharacters/Rectangle.prefab");

        var gmInstance = (GameObject)PrefabUtility.InstantiatePrefab(gameManagers);
        var smInstance = (GameObject)PrefabUtility.InstantiatePrefab(soundManager);
        var uiInstance = (GameObject)PrefabUtility.InstantiatePrefab(uiCamera);
        var camInstance = (GameObject)PrefabUtility.InstantiatePrefab(cameraRig);
        camInstance.transform.position = new Vector3(0, 3, -10);

        // ---- Level parent ----
        var level = new GameObject("Level");

        // ---- Checkpoint / spawn ----
        var spawnInstance = (GameObject)PrefabUtility.InstantiatePrefab(levelStart);
        spawnInstance.name = "LevelStart";
        spawnInstance.transform.SetParent(level.transform);
        spawnInstance.transform.position = new Vector3(0f, 5f, 0f);
        var checkPoint = spawnInstance.GetComponent<CheckPoint>();

        // ---- LevelManager ----
        var levelManagerGO = new GameObject("LevelManager");
        var levelManager = levelManagerGO.AddComponent<LevelManager>();
        levelManager.PlayerPrefabs = new Character[] { rectangle.GetComponent<Character>() };
        levelManager.AutoAttributePlayerIDs = true;
        levelManager.DebugSpawn = checkPoint;
        levelManager.BoundsMode = LevelManager.BoundsModes.TwoD;

        // ---- Geometry helpers ----
        var platforms = new GameObject("Platforms");
        platforms.transform.SetParent(level.transform);

        // Surface-relative placement: we place by desired top-surface world position.
        // Platform_1/2/3/4/5 : sprite 5.12 wide, top surface at +2.57 from pivot
        // Platform_6/9       : thin 5.12 wide, top surface at +0.64
        // Platform_End_1     : 3.18 wide, surface +1.01
        // Platform_Ramp_45   : 5.12, diagonal edge from (-2.55,-2.56) to (2.56,2.56)
        var placed = new List<GameObject>();

        System.Action<string, float, float, string> placeBySurface =
            (prefabPath, centerX, surfaceY, name) =>
        {
            var prefab = Load(prefabPath);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            // figure out surface offset from edge collider top point
            float topOffset = 0f;
            var edge = inst.GetComponent<EdgeCollider2D>();
            if (edge != null)
            {
                float maxY = float.MinValue;
                foreach (var p in edge.points) maxY = Mathf.Max(maxY, p.y);
                topOffset = edge.offset.y + maxY;
            }
            inst.transform.SetParent(platforms.transform);
            inst.transform.position = new Vector3(centerX, surfaceY - topOffset, 0f);
            if (!string.IsNullOrEmpty(name)) inst.name = name;
            SetLayerRecursive(inst, LayerPlatforms);
            placed.Add(inst);
        };

        string P(string n) => $"Assets/2D_Pack/!Prefabs/Platforms/{n}.prefab";

        // ---- Ground segment 1 (start) : 5 tiles, surface y = 0, x from 0..20.48 ----
        float groundSurface = 0f;
        for (int i = 0; i < 5; i++)
            placeBySurface(P("Platform_1"), i * 5.12f, groundSurface, $"Ground_{i}");

        // ---- Gap, then a thin floating step to jump onto ----
        placeBySurface(P("Platform_6"), 28.0f, 1.6f, "Step_A");
        placeBySurface(P("Platform_6"), 33.5f, 3.0f, "Step_B");

        // ---- Ground segment 2 (after the jumps) : 4 tiles ----
        float ground2X0 = 39.0f;
        for (int i = 0; i < 4; i++)
            placeBySurface(P("Platform_1"), ground2X0 + i * 5.12f, groundSurface, $"Ground2_{i}");

        // ---- A ramp up onto a higher ledge ----
        // Ramp_45 placed so its low end meets ground2 right edge.
        var rampPrefab = Load(P("Platform_Ramp_45"));
        var ramp = (GameObject)PrefabUtility.InstantiatePrefab(rampPrefab);
        ramp.transform.SetParent(platforms.transform);
        // ramp pivot center; edge from (-2.55,-2.56) to (2.56,2.56). Place so low end ~ ground surface 0 at x ~ groundEnd
        float rampCenterX = ground2X0 + 3 * 5.12f + 2.56f + 2.55f;
        ramp.transform.position = new Vector3(rampCenterX, 2.56f, 0f); // low end y ~0
        ramp.name = "Ramp_45";
        SetLayerRecursive(ramp, LayerPlatforms);
        placed.Add(ramp);

        // ---- High ledge at top of ramp (surface y ~ 5.12) ----
        float ledgeSurface = 5.12f;
        float ledgeX0 = rampCenterX + 2.56f + 2.56f;
        for (int i = 0; i < 3; i++)
            placeBySurface(P("Platform_1"), ledgeX0 + i * 5.12f, ledgeSurface, $"Ledge_{i}");

        // ---- End cap platform ----
        placeBySurface(P("Platform_End_1"), ledgeX0 + 3 * 5.12f, ledgeSurface, "End");

        // ---- Compute & set Level bounds from placed renderers ----
        Bounds b = new Bounds(new Vector3(0, 0, 0), Vector3.zero);
        bool first = true;
        foreach (var g in placed)
        {
            var r = g.GetComponentInChildren<Renderer>();
            if (r == null) continue;
            if (first) { b = r.bounds; first = false; }
            else b.Encapsulate(r.bounds);
        }
        // pad bounds vertically for jumping headroom and falling death
        b.Expand(new Vector3(2f, 14f, 10f));
        levelManager.LevelBounds = new Bounds(b.center, b.size);

        // ---- Save scene ----
        EditorSceneManager.MarkSceneDirty(scene);
        bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        sb.AppendLine($"Scene saved={saved} at {ScenePath}");
        sb.AppendLine($"Placed {placed.Count} platform objects on layer Platforms.");
        sb.AppendLine($"LevelBounds center={levelManager.LevelBounds.center} size={levelManager.LevelBounds.size}");
        sb.AppendLine($"Spawn at {spawnInstance.transform.position}");
        return sb.ToString();
    }
}
