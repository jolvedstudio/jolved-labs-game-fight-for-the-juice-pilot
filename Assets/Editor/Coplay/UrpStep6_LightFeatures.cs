using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Game;

public class UrpStep6_LightFeatures
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // Make the unlit "glow_radial" sprites unlit again so they read as emissive glows,
        // and the coin sprite itself can stay lit. Then add a real Light2D to each coin glow.
        Material unlitSpriteMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

        // ---------- NEW-a: coins emit real 2D light ----------
        int coinLights = 0;
        var items = GameObject.Find("Level/Items");
        if (items != null)
        {
            foreach (var glow in items.GetComponentsInChildren<Transform>(true))
            {
                if (glow.name != "CoinGlow") continue;

                // keep the additive glow sprite unlit so it always shines
                var sr = glow.GetComponent<SpriteRenderer>();
                if (sr != null && unlitSpriteMat != null) sr.sharedMaterial = unlitSpriteMat;

                if (glow.GetComponent<Light2D>() == null)
                {
                    var l = glow.gameObject.AddComponent<Light2D>();
                    l.lightType = Light2D.LightType.Point;
                    l.color = new Color(0.35f, 0.95f, 1f);
                    l.intensity = 0.7f;
                    l.pointLightInnerRadius = 0.1f;
                    l.pointLightOuterRadius = 2.6f;
                    l.falloffIntensity = 0.7f;
                    l.shadowsEnabled = false;
                    // gentle flicker like an unstable pickup
                    var f = glow.gameObject.AddComponent<LightFlicker2D>();
                    f.MinInterval = 12f; f.MaxInterval = 30f;
                    f.MinIntensityFactor = 0.45f;
                    coinLights++;
                    EditorUtility.SetDirty(glow.gameObject);
                }
            }
        }
        sb.AppendLine($"NEW-a: coin lights added/updated = {coinLights}");

        // ---------- NEW-b: small red pilot light on NPCs ----------
        int npcLights = 0;
        var enemies = GameObject.Find("Level/Enemies");
        npcLights += AddPilotLights(enemies);
        var dude = GameObject.Find("Level/Dude");
        if (dude != null) npcLights += AddPilotLights(dude.transform.parent != null ? dude : dude);
        // Dude is a single object; handle directly
        if (dude != null && dude.transform.Find("PilotLight") == null)
        {
            npcLights += AddPilotLight(dude.transform, new Color(1f, 0.85f, 0.2f)); // friendly = amber
        }
        sb.AppendLine($"NEW-b: NPC pilot lights added = {npcLights}");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return sb.ToString();
    }

    static int AddPilotLights(GameObject parent)
    {
        if (parent == null) return 0;
        int count = 0;
        // each direct child that is an enemy character
        foreach (Transform child in parent.transform)
        {
            if (child.name == "PilotLight") continue;
            // only add to objects that look like a robot/enemy (have a Character or Health)
            var comps = child.GetComponents<MonoBehaviour>();
            bool isAgent = false;
            foreach (var c in comps) { if (c != null && (c.GetType().Name == "Character" || c.GetType().Name == "Health")) { isAgent = true; break; } }
            if (!isAgent) continue;
            if (child.Find("PilotLight") != null) continue;
            count += AddPilotLight(child, new Color(1f, 0.15f, 0.12f)); // hostile = red
        }
        return count;
    }

    static int AddPilotLight(Transform parent, Color color)
    {
        var go = new GameObject("PilotLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = 0.9f;
        l.pointLightInnerRadius = 0.05f;
        l.pointLightOuterRadius = 1.6f;
        l.falloffIntensity = 0.8f;
        l.shadowsEnabled = false;
        // Steady locator beacon so the enemy's position is always readable.
        // (Kept un-flickering to honour the "10s+ between flickers" rule and to
        //  serve as a reliable position indicator.)
        EditorUtility.SetDirty(go);
        return 1;
    }
}
