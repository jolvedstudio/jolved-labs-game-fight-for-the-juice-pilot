using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UrpStep9_ParticlesAndFog
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // ---------- (b) Recolor the rising sparks from bright yellow -> rusty embers ----------
        var sparksGo = GameObject.Find("Level/LavaSparksGenerator");
        if (sparksGo != null)
        {
            var ps = sparksGo.GetComponent<ParticleSystem>();

            // start color: warm rust/orange instead of pale blue-white
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.65f, 0.22f, 0.06f, 1f),   // dark rust
                new Color(0.85f, 0.40f, 0.12f, 1f));   // burnt orange

            // colorOverLifetime: rust -> dim ember -> fade (no bright yellow)
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.80f, 0.35f, 0.10f), 0.0f), // glowing rust
                    new GradientColorKey(new Color(0.55f, 0.20f, 0.06f), 0.5f), // cooling ember
                    new GradientColorKey(new Color(0.30f, 0.12f, 0.05f), 1.0f), // dark rust
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0.0f),
                    new GradientAlphaKey(0.8f, 0.5f),
                    new GradientAlphaKey(0f, 1.0f),
                });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            EditorUtility.SetDirty(sparksGo);
            sb.AppendLine("Recolored LavaSparks to rusty embers (rust->burnt-orange->dark fade, no bright yellow).");
        }

        // Note about lighting: particle renderers use 'Particles/Standard Unlit' / 'Sprites/Default',
        // which are NOT affected by URP 2D lights by design. For embers/sparks this is correct -
        // they are self-emissive glowing bits, so they read well against the darkness.

        // ---------- (c) Add a cheap volumetric-style fog at the lowest level ----------
        var level = GameObject.Find("Level");
        if (level != null)
        {
            var existing = level.transform.Find("GroundFog");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var fogGo = new GameObject("GroundFog");
            fogGo.transform.SetParent(level.transform, false);
            // place along the bottom of the level, slightly in front of background
            fogGo.transform.position = new Vector3(-8f, -13.5f, 1.5f);

            var ps = fogGo.AddComponent<ParticleSystem>();

            // Reuse the existing mist material (soft sprite) so no new asset is needed.
            var mistGo = GameObject.Find("Level/LavaMistGenerator");
            Material mistMat = null;
            if (mistGo != null)
            {
                var mr = mistGo.GetComponent<ParticleSystemRenderer>();
                if (mr != null) mistMat = mr.sharedMaterial;
            }

            var r = fogGo.GetComponent<ParticleSystemRenderer>();
            if (mistMat != null) r.sharedMaterial = mistMat;
            r.sortingOrder = 5; // in front of most geometry, behind UI
            r.renderMode = ParticleSystemRenderMode.Billboard;

            // --- CHEAP config: very few, very large, slow particles ---
            var main = ps.main;
            main.duration = 10f;
            main.loop = true;
            main.startLifetime = 18f;
            main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(14f, 22f);  // big soft clouds
            main.startColor = new Color(0.32f, 0.30f, 0.34f, 0.16f);    // faint cool grey haze
            main.maxParticles = 24;                                     // <-- key: tiny count = cheap
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var em = ps.emission;
            em.enabled = true;
            em.rateOverTime = 1.5f; // a couple of clouds per second, capped by maxParticles

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 1.5f, 0.1f); // wide, thin band hugging the floor

            // gentle drift + slow fade for a living fog
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
            vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(0.32f, 0.30f, 0.34f), 0f),
                        new GradientColorKey(new Color(0.30f, 0.28f, 0.33f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(1f, 0.3f),
                        new GradientAlphaKey(1f, 0.7f),
                        new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);

            // slight size growth for a drifting, expanding haze
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 1.3f));

            ps.Play();
            EditorUtility.SetDirty(fogGo);
            sb.AppendLine("Added 'GroundFog' (24-particle soft haze band) hugging the lowest level. Very cheap: ~24 large billboards, rate 1.5/s.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return sb.ToString();
    }
}
