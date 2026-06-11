using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UrpStep2_AddLights
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // Compute level bounds from platform renderers
        var platforms = GameObject.Find("Level/Platforms");
        Bounds b = new Bounds();
        bool init = false;
        if (platforms != null)
        {
            foreach (var sr in platforms.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!init) { b = sr.bounds; init = true; }
                else b.Encapsulate(sr.bounds);
            }
        }
        if (!init) { b = new Bounds(Vector3.zero, new Vector3(60, 30, 1)); }
        sb.AppendLine($"Level bounds center={b.center} size={b.size}");

        // Container for lights
        var existing = GameObject.Find("Level/Lighting2D");
        if (existing != null) Object.DestroyImmediate(existing);
        var container = new GameObject("Lighting2D");
        container.transform.SetParent(GameObject.Find("Level").transform, false);

        // 1) Dim global light so the level is darker but still readable
        var globalGo = new GameObject("GlobalLight2D");
        globalGo.transform.SetParent(container.transform, false);
        var global = globalGo.AddComponent<Light2D>();
        global.lightType = Light2D.LightType.Global;
        global.color = new Color(0.55f, 0.6f, 0.8f); // cool ambient
        global.intensity = 0.22f;

        // 2) Scatter warm point lights across the level (deterministic seed)
        Random.InitState(1337);
        int count = 14;
        var warm = new Color(1f, 0.72f, 0.42f); // emergency/torch warm
        var coolAccent = new Color(0.5f, 0.7f, 1f);
        float minX = b.min.x + 2f, maxX = b.max.x - 2f;
        float minY = b.min.y + 1f, maxY = b.max.y - 1f;
        var placed = new List<Vector2>();
        int made = 0, attempts = 0;
        while (made < count && attempts < count * 20)
        {
            attempts++;
            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);
            // keep some spacing
            bool tooClose = false;
            foreach (var p in placed) if (Vector2.Distance(p, new Vector2(x, y)) < 6f) { tooClose = true; break; }
            if (tooClose) continue;
            placed.Add(new Vector2(x, y));

            var go = new GameObject($"PointLight2D_{made}");
            go.transform.SetParent(container.transform, false);
            go.transform.position = new Vector3(x, y, 0);
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;
            bool isWarm = Random.value > 0.25f;
            light.color = isWarm ? warm : coolAccent;
            light.intensity = Random.Range(0.9f, 1.5f);
            light.pointLightInnerRadius = Random.Range(0.5f, 1.5f);
            light.pointLightOuterRadius = Random.Range(5f, 9f);
            light.shadowsEnabled = true;
            light.shadowIntensity = 0.7f;
            light.falloffIntensity = 0.6f;
            made++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        sb.AppendLine($"Added 1 dim global light + {made} scattered point lights (shadows enabled).");
        return sb.ToString();
    }
}
