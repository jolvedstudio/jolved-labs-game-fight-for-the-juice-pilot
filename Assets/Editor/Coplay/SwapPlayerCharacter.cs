using UnityEngine;
using UnityEditor;
using MoreMountains.CorgiEngine;

public class SwapPlayerCharacter
{
    public static string Execute()
    {
        // Find the LevelManager in the scene
        LevelManager levelManager = Object.FindFirstObjectByType<LevelManager>();
        if (levelManager == null)
            return "ERROR: LevelManager not found in scene.";

        // Load the new character prefab
        string prefabPath = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/PlayableCharacters/spine-space-corgi.prefab";
        GameObject prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefabGO == null)
            return "ERROR: Could not load prefab at: " + prefabPath;

        Character characterPrefab = prefabGO.GetComponent<Character>();
        if (characterPrefab == null)
            return "ERROR: Prefab does not have a Character component.";

        // Use SerializedObject to set the PlayerPrefabs array
        SerializedObject so = new SerializedObject(levelManager);
        SerializedProperty playerPrefabsProp = so.FindProperty("PlayerPrefabs");
        if (playerPrefabsProp == null)
            return "ERROR: Could not find PlayerPrefabs property via SerializedObject.";

        playerPrefabsProp.arraySize = 1;
        playerPrefabsProp.GetArrayElementAtIndex(0).objectReferenceValue = characterPrefab;
        so.ApplyModifiedProperties();

        // Mark scene dirty and save
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        return $"SUCCESS: LevelManager PlayerPrefabs[0] set to '{characterPrefab.name}' ({prefabPath}). Scene saved.";
    }
}
