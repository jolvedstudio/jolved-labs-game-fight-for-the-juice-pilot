using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Game;

public class UrpStep7_BulletAndFlicker
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // ---------- NEW-c: ephemeral light on pooled bullet prefab ----------
        // The weapon object-pools 20 bullets and reuses them, so adding ONE Light2D
        // to the prefab means at most ~20 lights ever exist - no per-shot allocation.
        string bulletPath = "Assets/CorgiEngine/Demos/Minimal/Prefabs/Weapons/Ammo/MinimalMachineGunBullet.prefab";
        var bullet = PrefabUtility.LoadPrefabContents(bulletPath);
        try
        {
            var existing = bullet.transform.Find("BulletLight");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("BulletLight");
            go.transform.SetParent(bullet.transform, false);
            go.transform.localPosition = Vector3.zero;
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = new Color(1f, 0.75f, 0.35f);   // warm muzzle/tracer glow
            l.intensity = 1.4f;
            l.pointLightInnerRadius = 0.05f;
            l.pointLightOuterRadius = 2.2f;
            l.falloffIntensity = 0.6f;
            l.shadowsEnabled = false;                 // cheap: tracers don't cast shadows

            PrefabUtility.SaveAsPrefabAsset(bullet, bulletPath);
            sb.AppendLine("NEW-c: ephemeral BulletLight (Light2D) added to pooled bullet prefab (max ~20 instances, no per-shot alloc).");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(bullet);
        }

        // ---------- NEW-d: flicker on the scattered environment point lights ----------
        int flickerAdded = 0;
        var lighting = GameObject.Find("Level/Lighting2D");
        if (lighting != null)
        {
            foreach (Transform child in lighting.transform)
            {
                if (!child.name.StartsWith("PointLight2D")) continue;
                if (child.GetComponent<Light2D>() == null) continue;
                if (child.GetComponent<LightFlicker2D>() != null) continue;
                var f = child.gameObject.AddComponent<LightFlicker2D>();
                // sparse, eerie: at least 10s, up to ~30s between flickers, randomised phase
                f.MinInterval = Random.Range(10f, 14f);
                f.MaxInterval = Random.Range(22f, 34f);
                f.MinBurstDuration = 0.08f;
                f.MaxBurstDuration = 0.5f;
                f.MinIntensityFactor = 0.08f;
                EditorUtility.SetDirty(child.gameObject);
                flickerAdded++;
            }
        }
        sb.AppendLine($"NEW-d: LightFlicker2D added to {flickerAdded} scattered environment lights (10s+ apart, desynced).");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return sb.ToString();
    }
}
