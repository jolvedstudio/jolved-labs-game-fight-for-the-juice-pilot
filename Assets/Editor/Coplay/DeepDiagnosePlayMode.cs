using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using MoreMountains.CorgiEngine;

/// <summary>
/// Deep diagnosis: checks the LavaBot prefab's MMPoolableObject,
/// checks the scene YAML for any GUID references to blueRobot,
/// and checks if the LavaBot prefab itself is correctly saved.
/// </summary>
public class DeepDiagnosePlayMode
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // 1. Check LavaBot prefab contents
        string lavaBotPath = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/LavaBot.prefab";
        string blueRobotPath = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/blueRobot.prefab";

        // Get GUIDs
        string lavaBotGUID = AssetDatabase.AssetPathToGUID(lavaBotPath);
        string blueRobotGUID = AssetDatabase.AssetPathToGUID(blueRobotPath);
        sb.AppendLine($"LavaBot GUID:   {lavaBotGUID}");
        sb.AppendLine($"blueRobot GUID: {blueRobotGUID}");

        // 2. Check if scene YAML contains blueRobot GUID
        string scenePath = "Assets/CorgiEngine/Demos/Corgi2D/Lava.unity";
        string sceneFullPath = Path.GetFullPath(scenePath);
        bool sceneHasBlueRobotGUID = false;
        bool sceneHasLavaBotGUID = false;
        if (File.Exists(sceneFullPath))
        {
            string sceneText = File.ReadAllText(sceneFullPath);
            sceneHasBlueRobotGUID = sceneText.Contains(blueRobotGUID);
            sceneHasLavaBotGUID   = sceneText.Contains(lavaBotGUID);
        }
        sb.AppendLine($"\nScene contains blueRobot GUID: {sceneHasBlueRobotGUID}");
        sb.AppendLine($"Scene contains LavaBot GUID:   {sceneHasLavaBotGUID}");

        // 3. Check LavaBot prefab YAML for blueRobot GUID
        string lavaBotFullPath = Path.GetFullPath(lavaBotPath);
        bool lavaBotHasBlueRobotGUID = false;
        if (File.Exists(lavaBotFullPath))
        {
            string prefabText = File.ReadAllText(lavaBotFullPath);
            lavaBotHasBlueRobotGUID = prefabText.Contains(blueRobotGUID);
        }
        sb.AppendLine($"LavaBot prefab contains blueRobot GUID: {lavaBotHasBlueRobotGUID}");

        // 4. Check if LavaBot scene instances are prefab instances of LavaBot or blueRobot
        sb.AppendLine("\n=== LavaBot scene instance prefab sources ===");
        GameObject[] allGOs = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allGOs)
        {
            if (go.name.StartsWith("LavaBot"))
            {
                GameObject prefabSource = PrefabUtility.GetCorrespondingObjectFromSource(go);
                string sourceName = prefabSource != null ? prefabSource.name : "none (not a prefab instance)";
                string sourceAssetPath = prefabSource != null
                    ? AssetDatabase.GetAssetPath(prefabSource) : "N/A";
                sb.AppendLine($"  {go.name} → prefab source: '{sourceName}' at {sourceAssetPath}");

                // Check if it's an instance of blueRobot
                GameObject rootPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
                if (rootPrefab != null)
                {
                    GameObject rootSource = PrefabUtility.GetCorrespondingObjectFromSource(rootPrefab);
                    string rootSourcePath = rootSource != null
                        ? AssetDatabase.GetAssetPath(rootSource) : "N/A";
                    sb.AppendLine($"    outermost root source: {rootSourcePath}");
                }
            }
        }

        // 5. Check AutoRespawn - does it store original position?
        sb.AppendLine("\n=== AutoRespawn initial positions ===");
        var respawners = Object.FindObjectsByType<AutoRespawn>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var r in respawners)
        {
            if (r.gameObject.name.StartsWith("LavaBot"))
            {
                SerializedObject so = new SerializedObject(r);
                sb.AppendLine($"  {r.gameObject.name}: pos={r.transform.position}");
            }
        }

        return sb.ToString();
    }
}
