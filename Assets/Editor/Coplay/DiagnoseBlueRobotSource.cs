using UnityEngine;
using UnityEditor;
using System.Text;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;

/// <summary>
/// Scans the entire scene for any reference to blueRobot prefab:
/// - MMSimpleObjectPooler / MMMultipleObjectPooler
/// - AutoRespawn components
/// - LevelManager SceneCharacters / PlayerPrefabs
/// - Any component with a serialized field pointing to blueRobot
/// </summary>
public class DiagnoseBlueRobotSource
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        string blueRobotPath = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/blueRobot.prefab";
        GameObject blueRobotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(blueRobotPath);
        sb.AppendLine($"blueRobot prefab loaded: {blueRobotPrefab != null}");

        // 1. Check all MMSimpleObjectPoolers
        var simplePoolers = Object.FindObjectsByType<MMSimpleObjectPooler>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"\n=== MMSimpleObjectPoolers ({simplePoolers.Length}) ===");
        foreach (var p in simplePoolers)
        {
            SerializedObject so = new SerializedObject(p);
            var prop = so.FindProperty("GameObjectToPool");
            string pooledName = prop?.objectReferenceValue != null
                ? prop.objectReferenceValue.name : "NULL";
            sb.AppendLine($"  [{p.gameObject.name}] pools: {pooledName}");
        }

        // 2. Check all MMMultipleObjectPoolers
        var multiPoolers = Object.FindObjectsByType<MMMultipleObjectPooler>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"\n=== MMMultipleObjectPoolers ({multiPoolers.Length}) ===");
        foreach (var p in multiPoolers)
        {
            SerializedObject so = new SerializedObject(p);
            var poolProp = so.FindProperty("Pool");
            if (poolProp != null)
                for (int i = 0; i < poolProp.arraySize; i++)
                {
                    var el = poolProp.GetArrayElementAtIndex(i);
                    var goProp = el.FindPropertyRelative("GameObjectToPool");
                    string n = goProp?.objectReferenceValue != null
                        ? goProp.objectReferenceValue.name : "NULL";
                    sb.AppendLine($"  [{p.gameObject.name}] pool[{i}]: {n}");
                }
        }

        // 3. Check LevelManager
        var lm = Object.FindFirstObjectByType<LevelManager>();
        if (lm != null)
        {
            sb.AppendLine($"\n=== LevelManager ===");
            SerializedObject so = new SerializedObject(lm);
            var prefabsProp = so.FindProperty("PlayerPrefabs");
            if (prefabsProp != null)
                for (int i = 0; i < prefabsProp.arraySize; i++)
                {
                    var el = prefabsProp.GetArrayElementAtIndex(i);
                    sb.AppendLine($"  PlayerPrefabs[{i}]: {(el.objectReferenceValue != null ? el.objectReferenceValue.name : "NULL")}");
                }
            var sceneCharsProp = so.FindProperty("SceneCharacters");
            if (sceneCharsProp != null)
                for (int i = 0; i < sceneCharsProp.arraySize; i++)
                {
                    var el = sceneCharsProp.GetArrayElementAtIndex(i);
                    sb.AppendLine($"  SceneCharacters[{i}]: {(el.objectReferenceValue != null ? el.objectReferenceValue.name : "NULL")}");
                }
        }

        // 4. Check AutoRespawn on all objects
        var respawners = Object.FindObjectsByType<AutoRespawn>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"\n=== AutoRespawn components ({respawners.Length}) ===");
        foreach (var r in respawners)
            sb.AppendLine($"  {r.gameObject.name} | RespawnOnPlayerRespawn={r.RespawnOnPlayerRespawn} | DisableOnKill={r.DisableOnKill}");

        // 5. Check all inactive GameObjects under Level/Enemies
        GameObject enemiesGO = GameObject.Find("Level/Enemies");
        if (enemiesGO == null)
        {
            GameObject level = GameObject.Find("Level");
            if (level != null)
            {
                var t = level.transform.Find("Enemies");
                if (t != null) enemiesGO = t.gameObject;
            }
        }
        sb.AppendLine($"\n=== Level/Enemies children (including inactive) ===");
        if (enemiesGO != null)
        {
            foreach (Transform child in enemiesGO.transform)
                sb.AppendLine($"  {child.name} active={child.gameObject.activeSelf}");
        }
        else
            sb.AppendLine("  Level/Enemies not found!");

        // 6. Scan ALL scene GameObjects for any with "blueRobot" in name
        var allGOs = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"\n=== GameObjects with 'robot' in name ===");
        foreach (var go in allGOs)
            if (go.name.ToLower().Contains("robot"))
                sb.AppendLine($"  {go.name} active={go.activeSelf} path={GetPath(go)}");

        return sb.ToString();
    }

    static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null) { path = t.name + "/" + path; t = t.parent; }
        return path;
    }
}
