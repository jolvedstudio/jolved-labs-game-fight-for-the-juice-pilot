using UnityEngine;
using UnityEditor;

public class AddBackgroundMusic
{
    public static string Execute()
    {
        const string musicPrefabPath = "Assets/CorgiEngine/Common/Prefabs/Music/BgMusicUniformMotionThereIsNoWay.prefab";
        const string instanceName = "BackgroundMusic";

        // Skip if it already exists in the scene
        var existing = GameObject.Find(instanceName);
        if (existing != null && existing.GetComponent<MoreMountains.CorgiEngine.BackgroundMusic>() != null)
            return "Background music already present in scene; skipping.";

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(musicPrefabPath);
        if (prefab == null)
            return $"ERROR: could not load music prefab at {musicPrefabPath}";

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instance == null)
            return "ERROR: failed to instantiate music prefab";

        instance.name = instanceName;

        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        return $"Added looping background music ({prefab.name}) to scene '{scene.name}'.";
    }
}
