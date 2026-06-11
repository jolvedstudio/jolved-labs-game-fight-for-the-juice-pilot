using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;
using Game;

/// <summary>
/// Applies the full "abandoned facility" lighting + atmosphere treatment (originally
/// built for the Lava scene) to whatever level scene is currently open.
/// Idempotent: safe to run multiple times.
/// </summary>
public class ApplyFullTreatmentToLevel
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        var levelGo = GameObject.Find("Level");
        if (levelGo == null) return "ERROR: no 'Level' root in scene.";

        // ---------------- 1. Lit materials on all non-particle sprites ----------------
        var litMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        int litCount = 0;
        if (litMat != null)
        {
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (sr.GetComponent<ParticleSystem>() != null) continue;
                if (sr.name == "CoinGlow") continue; // glow stays unlit
                if (sr.sharedMaterial == litMat) continue;
                sr.sharedMaterial = litMat;
                EditorUtility.SetDirty(sr);
                litCount++;
            }
        }
        sb.AppendLine($"1. Lit material assigned to {litCount} sprites.");

        // ---------------- 2. Lighting2D: global + scattered point lights ----------------
        var platforms = GameObject.Find("Level/Platforms");
        Bounds b = new Bounds(); bool init = false;
        if (platforms != null)
            foreach (var sr in platforms.GetComponentsInChildren<SpriteRenderer>(true))
            { if (!init) { b = sr.bounds; init = true; } else b.Encapsulate(sr.bounds); }
        if (!init) b = new Bounds(Vector3.zero, new Vector3(60, 30, 1));

        var existingLighting = GameObject.Find("Level/Lighting2D");
        if (existingLighting != null) Object.DestroyImmediate(existingLighting);
        var container = new GameObject("Lighting2D");
        container.transform.SetParent(levelGo.transform, false);

        var globalGo = new GameObject("GlobalLight2D");
        globalGo.transform.SetParent(container.transform, false);
        var global = globalGo.AddComponent<Light2D>();
        global.lightType = Light2D.LightType.Global;
        global.color = new Color(0.55f, 0.62f, 0.85f);
        global.intensity = 0.3f;

        Random.InitState(1337);
        int count = 14, made = 0, attempts = 0;
        var warm = new Color(1f, 0.72f, 0.42f);
        var coolAccent = new Color(0.5f, 0.7f, 1f);
        float minX = b.min.x + 2f, maxX = b.max.x - 2f, minY = b.min.y + 1f, maxY = b.max.y - 1f;
        var placed = new List<Vector2>();
        while (made < count && attempts < count * 20)
        {
            attempts++;
            float x = Random.Range(minX, maxX), y = Random.Range(minY, maxY);
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
            // flicker (sparse, desynced)
            var f = go.AddComponent<LightFlicker2D>();
            f.MinInterval = Random.Range(10f, 14f);
            f.MaxInterval = Random.Range(22f, 34f);
            f.MinBurstDuration = 0.08f; f.MaxBurstDuration = 0.5f;
            f.MinIntensityFactor = 0.08f;
            made++;
        }
        sb.AppendLine($"2. Global light + {made} flickering point lights added.");

        // ---------------- 3. Shadow casters on platforms ----------------
        int shadows = 0;
        if (platforms != null)
            foreach (var sr in platforms.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.GetComponent<ShadowCaster2D>() != null) continue;
                var sc = sr.gameObject.AddComponent<ShadowCaster2D>();
                sc.selfShadows = false;
                EditorUtility.SetDirty(sr.gameObject);
                shadows++;
            }
        sb.AppendLine($"3. ShadowCaster2D added to {shadows} platforms.");

        // ---------------- 4. Coin glows: real Light2D ----------------
        Material unlitSpriteMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        var glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Game/Sprites/glow_radial.png");
        int coinLights = 0;
        var items = GameObject.Find("Level/Items");
        if (items != null)
        {
            // collect coin roots
            var coinRoots = new List<Transform>();
            foreach (Transform t in items.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("coin") && t.parent == items.transform) coinRoots.Add(t);

            foreach (var coin in coinRoots)
            {
                var glowTf = coin.Find("CoinGlow");
                if (glowTf == null)
                {
                    var g = new GameObject("CoinGlow");
                    g.transform.SetParent(coin, false);
                    g.transform.localPosition = Vector3.zero;
                    var gsr = g.AddComponent<SpriteRenderer>();
                    if (glowSprite != null) gsr.sprite = glowSprite;
                    gsr.color = new Color(0.4f, 0.95f, 1f, 0.8f);
                    gsr.sortingOrder = -1;
                    glowTf = g.transform;
                }
                var sr = glowTf.GetComponent<SpriteRenderer>();
                if (sr != null && unlitSpriteMat != null) sr.sharedMaterial = unlitSpriteMat;
                if (glowTf.GetComponent<Light2D>() == null)
                {
                    var l = glowTf.gameObject.AddComponent<Light2D>();
                    l.lightType = Light2D.LightType.Point;
                    l.color = new Color(0.35f, 0.95f, 1f);
                    l.intensity = 0.7f;
                    l.pointLightInnerRadius = 0.1f;
                    l.pointLightOuterRadius = 2.6f;
                    l.falloffIntensity = 0.7f;
                    l.shadowsEnabled = false;
                    var f = glowTf.gameObject.AddComponent<LightFlicker2D>();
                    f.MinInterval = 12f; f.MaxInterval = 30f; f.MinIntensityFactor = 0.45f;
                    coinLights++;
                    EditorUtility.SetDirty(glowTf.gameObject);
                }
            }
        }
        sb.AppendLine($"4. Coin lights added/updated = {coinLights}.");

        // ---------------- 5. Enemy pilot lights ----------------
        int pilot = 0;
        var enemies = GameObject.Find("Level/Enemies");
        if (enemies != null)
            foreach (Transform child in enemies.transform)
            {
                if (child.Find("PilotLight") != null) continue;
                AddPilotLight(child, new Color(1f, 0.15f, 0.12f), 1.5f, 3.2f);
                pilot++;
            }
        // Dude friendly amber
        var dude = GameObject.Find("Level/Dude");
        if (dude != null && dude.transform.Find("PilotLight") == null)
        { AddPilotLight(dude.transform, new Color(1f, 0.85f, 0.2f), 0.9f, 1.6f); pilot++; }
        sb.AppendLine($"5. Pilot lights added = {pilot}.");

        // ---------------- 6. Dude overhead spotlight (if Dude present) ----------------
        if (dude != null && levelGo.transform.Find("OverheadSpot") == null)
        {
            var go = new GameObject("OverheadSpot");
            go.transform.SetParent(levelGo.transform, false);
            go.transform.position = new Vector3(dude.transform.position.x, dude.transform.position.y + 3.8f, 0f);
            go.transform.rotation = Quaternion.Euler(0, 0, 180f);
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = new Color(1f, 0.95f, 0.75f);
            l.intensity = 2.2f;
            l.pointLightInnerRadius = 0.3f;
            l.pointLightOuterRadius = 4.5f;
            l.pointLightInnerAngle = 35f;
            l.pointLightOuterAngle = 55f;
            l.falloffIntensity = 0.5f;
            l.shadowsEnabled = false;
            var follow = go.AddComponent<OverheadLightFollow>();
            follow.Target = dude.transform;
            follow.Offset = new Vector3(0f, 3.8f, 0f);
            sb.AppendLine("6. Dude overhead spotlight added.");
        }

        // ---------------- 7. Recolor rising sparks to rusty embers ----------------
        var sparksGo = GameObject.Find("Level/LavaSparksGenerator");
        if (sparksGo != null)
        {
            var ps = sparksGo.GetComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.65f, 0.22f, 0.06f, 1f), new Color(0.85f, 0.40f, 0.12f, 1f));
            var col = ps.colorOverLifetime; col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.80f,0.35f,0.10f),0f),
                        new GradientColorKey(new Color(0.55f,0.20f,0.06f),0.5f),
                        new GradientColorKey(new Color(0.30f,0.12f,0.05f),1f) },
                new[] { new GradientAlphaKey(1f,0f), new GradientAlphaKey(0.8f,0.5f), new GradientAlphaKey(0f,1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            EditorUtility.SetDirty(sparksGo);
            sb.AppendLine("7. Sparks recolored to rusty embers.");
        }

        // ---------------- 8. Ground fog ----------------
        if (levelGo.transform.Find("GroundFog") == null)
        {
            var fogGo = new GameObject("GroundFog");
            fogGo.transform.SetParent(levelGo.transform, false);
            fogGo.transform.position = new Vector3(b.center.x, b.min.y + 1f, 1.5f);
            var ps = fogGo.AddComponent<ParticleSystem>();
            var mistGo = GameObject.Find("Level/LavaMistGenerator");
            Material mistMat = null;
            if (mistGo != null) { var mr = mistGo.GetComponent<ParticleSystemRenderer>(); if (mr != null) mistMat = mr.sharedMaterial; }
            var r = fogGo.GetComponent<ParticleSystemRenderer>();
            if (mistMat != null) r.sharedMaterial = mistMat;
            r.sortingOrder = 5;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            var main = ps.main;
            main.duration = 10f; main.loop = true; main.startLifetime = 18f; main.startSpeed = 0.25f;
            main.startSize = new ParticleSystem.MinMaxCurve(14f, 22f);
            main.startColor = new Color(0.32f, 0.30f, 0.34f, 0.16f);
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var em = ps.emission; em.enabled = true; em.rateOverTime = 1.5f;
            var shape = ps.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(Mathf.Max(34f, b.size.x), 1.5f, 0.1f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f); vel.y = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            var colf = ps.colorOverLifetime; colf.enabled = true;
            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(0.32f,0.30f,0.34f),0f), new GradientColorKey(new Color(0.30f,0.28f,0.33f),1f) },
                new[] { new GradientAlphaKey(0f,0f), new GradientAlphaKey(1f,0.3f), new GradientAlphaKey(1f,0.7f), new GradientAlphaKey(0f,1f) });
            colf.color = new ParticleSystem.MinMaxGradient(g);
            var sol = ps.sizeOverLifetime; sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.8f, 1f, 1.3f));
            sb.AppendLine("8. Ground fog added.");
        }

        // ---------------- 9. Camera: dark clear + remove legacy PP + UI overlay stack ----------------
        var mainGo = GameObject.Find("Main Camera");
        var uiGo = GameObject.Find("UICamera");
        if (mainGo != null)
        {
            var cam = mainGo.GetComponent<Camera>();
            foreach (var c in mainGo.GetComponents<MonoBehaviour>())
                if (c != null && c.GetType().FullName == "UnityEngine.Rendering.PostProcessing.PostProcessLayer")
                { Object.DestroyImmediate(c); }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
            var mainData = mainGo.GetComponent<UniversalAdditionalCameraData>();
            if (mainData == null) mainData = mainGo.AddComponent<UniversalAdditionalCameraData>();
            mainData.renderType = CameraRenderType.Base;
            mainData.renderPostProcessing = true;
            if (uiGo != null)
            {
                var uiCam = uiGo.GetComponent<Camera>();
                var uiData = uiGo.GetComponent<UniversalAdditionalCameraData>();
                if (uiData == null) uiData = uiGo.AddComponent<UniversalAdditionalCameraData>();
                uiData.renderType = CameraRenderType.Overlay;
                if (!mainData.cameraStack.Contains(uiCam)) mainData.cameraStack.Add(uiCam);
            }
            EditorUtility.SetDirty(mainGo);
            sb.AppendLine("9. Camera fixed (dark clear, legacy PP removed, UICamera overlay-stacked).");
        }
        // disable legacy volume
        var vol = GameObject.Find("Corgi2DPostProcessingVolume");
        if (vol != null)
            foreach (var c in vol.GetComponents<MonoBehaviour>())
                if (c != null && c.GetType().FullName == "UnityEngine.Rendering.PostProcessing.PostProcessVolume")
                { c.enabled = false; EditorUtility.SetDirty(c); }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return sb.ToString();
    }

    static void AddPilotLight(Transform parent, Color color, float intensity, float outerRadius)
    {
        var go = new GameObject("PilotLight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, 0.1f, 0f);
        var l = go.AddComponent<Light2D>();
        l.lightType = Light2D.LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.pointLightInnerRadius = 0.2f;
        l.pointLightOuterRadius = outerRadius;
        l.falloffIntensity = 0.6f;
        l.shadowsEnabled = false;
        EditorUtility.SetDirty(go);
    }
}
