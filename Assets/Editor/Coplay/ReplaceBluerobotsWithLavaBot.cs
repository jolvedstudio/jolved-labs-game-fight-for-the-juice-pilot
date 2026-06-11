using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MoreMountains.CorgiEngine;

/// <summary>
/// Replaces all blueRobot instances under Level/Enemies in the Lava scene
/// with LavaBot instances, preserving each robot's world position.
/// </summary>
public class ReplaceBluerobotsWithLavaBot
{
    const string PREFAB_PATH = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/LavaBot.prefab";

    public static string Execute()
    {
        // Load LavaBot prefab
        GameObject lavaBotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (lavaBotPrefab == null)
            return "ERROR: LavaBot prefab not found at " + PREFAB_PATH;

        // Find the Enemies container
        GameObject enemiesContainer = GameObject.Find("Level/Enemies");
        if (enemiesContainer == null)
        {
            // Try finding it by traversal
            GameObject level = GameObject.Find("Level");
            if (level != null)
            {
                Transform enemiesT = level.transform.Find("Enemies");
                if (enemiesT != null) enemiesContainer = enemiesT.gameObject;
            }
        }
        if (enemiesContainer == null)
            return "ERROR: Could not find Level/Enemies in scene.";

        // Collect all blueRobot children
        var toReplace = new System.Collections.Generic.List<(GameObject go, Vector3 pos, Quaternion rot)>();
        foreach (Transform child in enemiesContainer.transform)
        {
            if (child.name.StartsWith("blueRobot"))
                toReplace.Add((child.gameObject, child.position, child.rotation));
        }

        if (toReplace.Count == 0)
            return "ERROR: No blueRobot objects found under Level/Enemies.";

        int count = 0;
        foreach (var (go, pos, rot) in toReplace)
        {
            // Instantiate LavaBot at same world position
            GameObject newBot = (GameObject)PrefabUtility.InstantiatePrefab(lavaBotPrefab, enemiesContainer.transform);
            newBot.transform.position = pos;
            newBot.transform.rotation = rot;
            newBot.name = "LavaBot " + count;

            // Destroy the old blueRobot
            Object.DestroyImmediate(go);
            count++;
        }

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        return $"SUCCESS: Replaced {count} blueRobot(s) with LavaBot. Scene saved.";
    }
}
