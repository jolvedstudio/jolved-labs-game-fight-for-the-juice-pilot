using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public class UrpStep8_NpcLightTune
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // 1) Strengthen enemy pilot lights so their legs are lit too (wider + slightly lower).
        int tuned = 0;
        var enemies = GameObject.Find("Level/Enemies");
        if (enemies != null)
        {
            foreach (Transform child in enemies.transform)
            {
                var plTf = child.Find("PilotLight");
                if (plTf == null) continue;
                var l = plTf.GetComponent<Light2D>();
                if (l == null) continue;
                // brighter + larger radius to spill onto the legs, drop the source a touch
                l.intensity = 1.5f;
                l.pointLightInnerRadius = 0.2f;
                l.pointLightOuterRadius = 3.2f;
                l.falloffIntensity = 0.6f;
                var p = plTf.localPosition; p.y = 0.1f; plTf.localPosition = p;
                EditorUtility.SetDirty(plTf.gameObject);
                tuned++;
            }
        }
        sb.AppendLine($"Strengthened {tuned} enemy pilot lights (intensity 1.5, outerRadius 3.2, lowered to cover legs).");

        // 2) Give the Dude his own tight, focused overhead spotlight (cone pointing down).
        var dude = GameObject.Find("Level/Dude");
        if (dude != null)
        {
            var existing = dude.transform.Find("OverheadSpot");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject("OverheadSpot");
            go.transform.SetParent(dude.transform, false);
            // Dude scale is (-1.5,1.5,1) so use local coords that place light above his head.
            go.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            // point the cone straight down
            go.transform.localEulerAngles = new Vector3(0f, 0f, -90f);

            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = new Color(1f, 0.95f, 0.75f);     // warm, inviting
            l.intensity = 2.2f;
            l.pointLightInnerRadius = 0.3f;
            l.pointLightOuterRadius = 4.5f;
            l.pointLightInnerAngle = 35f;               // tight focused cone
            l.pointLightOuterAngle = 55f;
            l.falloffIntensity = 0.5f;
            l.shadowsEnabled = false;
            EditorUtility.SetDirty(go);
            sb.AppendLine("Added tight focused overhead spotlight 'OverheadSpot' above the Dude's head.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return sb.ToString();
    }
}
