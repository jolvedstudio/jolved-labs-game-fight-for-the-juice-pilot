using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UrpStep3_LitMaterials
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        var litMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        if (litMat == null)
            return "ERROR: Could not load Sprite-Lit-Default.mat";

        int changed = 0;
        // Apply to all SpriteRenderers in the scene (skip particle-driven ones)
        foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
        {
            // skip if part of a particle system (those use their own materials)
            if (sr.GetComponent<ParticleSystem>() != null) continue;
            if (sr.sharedMaterial == litMat) continue;
            sr.sharedMaterial = litMat;
            EditorUtility.SetDirty(sr);
            changed++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        sb.AppendLine($"Assigned Sprite-Lit-Default to {changed} sprite renderers. They now respond to 2D lights.");
        return sb.ToString();
    }
}
