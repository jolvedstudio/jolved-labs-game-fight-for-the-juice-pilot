using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DiagnoseParticlesAndFog
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        string[] names = { "Level/LavaSparksGenerator", "Level/LavaAshGenerator", "Level/LavaMistGenerator" };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go == null) { sb.AppendLine($"{n}: NOT FOUND"); continue; }
            var ps = go.GetComponent<ParticleSystem>();
            var r = go.GetComponent<ParticleSystemRenderer>();
            sb.AppendLine($"== {n} ==");
            if (ps != null)
            {
                var main = ps.main;
                sb.AppendLine($"  startColor(mode={main.startColor.mode}) = {main.startColor.color} / grad min {main.startColor.colorMin} max {main.startColor.colorMax}");
                sb.AppendLine($"  startSize={main.startSize.constant} startLifetime={main.startLifetime.constant} maxParticles={main.maxParticles}");
                var col = ps.colorOverLifetime;
                sb.AppendLine($"  colorOverLifetime.enabled={col.enabled}");
                var em = ps.emission;
                sb.AppendLine($"  emission.rate={em.rateOverTime.constant}");
            }
            if (r != null)
            {
                var mat = r.sharedMaterial;
                sb.AppendLine($"  material={(mat!=null?mat.name:"null")} shader={(mat!=null&&mat.shader!=null?mat.shader.name:"null")}");
                sb.AppendLine($"  sortingLayer={r.sortingLayerName} order={r.sortingOrder}");
            }
        }

        // Global light (for fog reference / ambient)
        var gl = GameObject.Find("Level/Lighting2D/GlobalLight2D");
        if (gl != null)
        {
            var l = gl.GetComponent<Light2D>();
            if (l != null) sb.AppendLine($"GlobalLight2D: intensity={l.intensity} color={l.color}");
        }

        // Is there any existing fog object?
        sb.AppendLine("Fog search: looking for objects named 'fog'...");
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name.ToLower().Contains("fog")) sb.AppendLine($"  found: {t.name}");

        return sb.ToString();
    }
}
