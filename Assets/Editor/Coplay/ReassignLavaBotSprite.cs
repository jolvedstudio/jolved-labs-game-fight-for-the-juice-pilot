using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class ReassignLavaBotSprite
{
    const string SPRITE_PATH  = "Assets/CorgiEngine/Demos/Corgi2D/Sprites/Enemies/lavaBot.png";
    const string PREFAB_PATH  = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/LavaBot.prefab";

    public static string Execute()
    {
        // Load the sprite
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
        if (sprite == null)
            return "ERROR: Sprite not found at " + SPRITE_PATH;

        int sceneFixed = 0;

        // Fix all LavaBot instances in the scene
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject go in allObjects)
        {
            if (go.name.StartsWith("LavaBot"))
            {
                SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite == null)
                {
                    sr.sprite = sprite;
                    EditorUtility.SetDirty(go);
                    sceneFixed++;
                }
            }
        }

        // Fix the prefab asset too
        using (var editScope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
        {
            GameObject root = editScope.prefabContentsRoot;
            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = sprite;
            }
        }

        // Mark scene dirty and save
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        // Frame LavaBot 0 in scene view
        GameObject lavaBot0 = GameObject.Find("LavaBot 0");
        if (lavaBot0 != null)
        {
            Selection.activeGameObject = lavaBot0;
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                sv.FrameSelected();
                sv.Repaint();
            }
        }

        return $"SUCCESS: Sprite reassigned to {sceneFixed} scene LavaBot(s) and prefab updated.";
    }
}
