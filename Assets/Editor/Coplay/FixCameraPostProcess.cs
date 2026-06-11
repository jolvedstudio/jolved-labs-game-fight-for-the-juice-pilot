using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixCameraPostProcess
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        var camGo = GameObject.Find("Main Camera");
        if (camGo == null) return "No Main Camera";
        var cam = camGo.GetComponent<Camera>();

        // 1) Remove the legacy v2 PostProcessLayer (incompatible with URP -> blanks the game view)
        foreach (var c in camGo.GetComponents<MonoBehaviour>())
        {
            if (c == null) continue;
            var tn = c.GetType().FullName;
            if (tn == "UnityEngine.Rendering.PostProcessing.PostProcessLayer")
            {
                Object.DestroyImmediate(c);
                sb.AppendLine("Removed legacy PostProcessLayer from Main Camera.");
            }
        }

        // 2) URP 2D has no skybox -> use a solid dark clear color for the abandoned-facility mood
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
        EditorUtility.SetDirty(cam);

        // 3) Make sure URP camera renders post-processing via its own Volume system
        var addData = camGo.GetComponent<UniversalAdditionalCameraData>();
        if (addData != null)
        {
            addData.renderPostProcessing = true;
            EditorUtility.SetDirty(addData);
        }

        // 4) Disable the orphaned legacy PostProcessVolume object (its v2 effects can't run on URP)
        var vol = GameObject.Find("Corgi2DPostProcessingVolume");
        if (vol != null)
        {
            foreach (var c in vol.GetComponents<MonoBehaviour>())
            {
                if (c == null) continue;
                if (c.GetType().FullName == "UnityEngine.Rendering.PostProcessing.PostProcessVolume")
                {
                    c.enabled = false;
                    EditorUtility.SetDirty(c);
                    sb.AppendLine("Disabled legacy PostProcessVolume component.");
                }
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        sb.AppendLine($"Camera clearFlags={cam.clearFlags}, bg set dark, post-processing routed through URP.");
        return sb.ToString();
    }
}
