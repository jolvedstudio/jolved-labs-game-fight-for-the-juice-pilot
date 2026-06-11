using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UrpStep4_ShadowCasters
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        var platforms = GameObject.Find("Level/Platforms");
        if (platforms == null) return "ERROR: Level/Platforms not found";

        int added = 0;
        foreach (var sr in platforms.GetComponentsInChildren<SpriteRenderer>(true))
        {
            var go = sr.gameObject;
            if (go.GetComponent<ShadowCaster2D>() != null) continue;
            var sc = go.AddComponent<ShadowCaster2D>();
            // CastShadow only (not self-shadow) reads cleaner for platforms
            sc.selfShadows = false;
            EditorUtility.SetDirty(go);
            added++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        sb.AppendLine($"Added ShadowCaster2D to {added} platforms (auto box shape from bounds).");
        return sb.ToString();
    }
}
