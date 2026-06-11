using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MoreMountains.Tools;

/// <summary>
/// Finds any MMSimpleObjectPooler in the scene that references the blueRobot prefab
/// and updates it to reference the LavaBot prefab instead.
/// </summary>
public class FixLavaBotPooler
{
    const string LAVABOT_PREFAB_PATH = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/LavaBot.prefab";
    const string BLUEROBOT_PREFAB_PATH = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/blueRobot.prefab";

    public static string Execute()
    {
        GameObject lavaBotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LAVABOT_PREFAB_PATH);
        if (lavaBotPrefab == null)
            return "ERROR: LavaBot prefab not found.";

        GameObject blueRobotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BLUEROBOT_PREFAB_PATH);

        int fixedCount = 0;
        var poolers = Object.FindObjectsByType<MMSimpleObjectPooler>(FindObjectsSortMode.None);
        foreach (var pooler in poolers)
        {
            SerializedObject so = new SerializedObject(pooler);
            SerializedProperty gameObjectProp = so.FindProperty("GameObjectToPool");
            if (gameObjectProp != null)
            {
                GameObject pooledGO = gameObjectProp.objectReferenceValue as GameObject;
                if (pooledGO != null && pooledGO.name.ToLower().Contains("bluerobot"))
                {
                    gameObjectProp.objectReferenceValue = lavaBotPrefab;
                    so.ApplyModifiedProperties();
                    fixedCount++;
                }
                // Also catch by direct prefab reference comparison
                else if (blueRobotPrefab != null && pooledGO == blueRobotPrefab)
                {
                    gameObjectProp.objectReferenceValue = lavaBotPrefab;
                    so.ApplyModifiedProperties();
                    fixedCount++;
                }
            }
        }

        // Also check MMMultipleObjectPooler
        var multiPoolers = Object.FindObjectsByType<MMMultipleObjectPooler>(FindObjectsSortMode.None);
        foreach (var pooler in multiPoolers)
        {
            SerializedObject so = new SerializedObject(pooler);
            SerializedProperty poolProp = so.FindProperty("Pool");
            if (poolProp != null)
            {
                for (int i = 0; i < poolProp.arraySize; i++)
                {
                    SerializedProperty element = poolProp.GetArrayElementAtIndex(i);
                    SerializedProperty goProp = element.FindPropertyRelative("GameObjectToPool");
                    if (goProp != null)
                    {
                        GameObject pooledGO = goProp.objectReferenceValue as GameObject;
                        if (pooledGO != null && pooledGO.name.ToLower().Contains("bluerobot"))
                        {
                            goProp.objectReferenceValue = lavaBotPrefab;
                            so.ApplyModifiedProperties();
                            fixedCount++;
                        }
                    }
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        return fixedCount > 0
            ? $"SUCCESS: Updated {fixedCount} pooler(s) from blueRobot to LavaBot. Scene saved."
            : "INFO: No poolers referencing blueRobot found. Scene saved.";
    }
}
