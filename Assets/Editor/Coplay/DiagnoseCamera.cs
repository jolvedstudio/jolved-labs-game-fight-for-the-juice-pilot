using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class DiagnoseCamera
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // URP pipeline + renderer
        var rp = GraphicsSettings.currentRenderPipeline;
        sb.AppendLine($"Current RP: {(rp != null ? rp.name : "NULL (Built-in!)")}  type={(rp!=null?rp.GetType().Name:"-")}");
        var urp = rp as UniversalRenderPipelineAsset;
        if (urp != null)
        {
            var so = new SerializedObject(urp);
            var list = so.FindProperty("m_RendererDataList");
            if (list != null)
                for (int i = 0; i < list.arraySize; i++)
                {
                    var r = list.GetArrayElementAtIndex(i).objectReferenceValue;
                    sb.AppendLine($"  Renderer[{i}] = {(r!=null?r.GetType().Name+" : "+r.name:"null")}");
                }
        }

        var camGo = GameObject.Find("Main Camera");
        if (camGo == null) return sb.ToString() + "No Main Camera";
        var cam = camGo.GetComponent<Camera>();
        sb.AppendLine($"Main Camera clearFlags={cam.clearFlags} bg={cam.backgroundColor}");

        // legacy postprocessing?
        foreach (var c in camGo.GetComponents<MonoBehaviour>())
        {
            if (c == null) continue;
            sb.AppendLine($"  Component: {c.GetType().FullName} enabled={c.enabled}");
        }

        var addData = camGo.GetComponent<UniversalAdditionalCameraData>();
        sb.AppendLine($"  UniversalAdditionalCameraData present: {addData != null}");
        if (addData != null)
            sb.AppendLine($"     rendererIndex={addData.GetType().GetProperty("renderType")?.GetValue(addData)} postProcessing? (checking via SO)");

        // Corgi2DPostProcessingVolume
        var vol = GameObject.Find("Corgi2DPostProcessingVolume");
        if (vol != null)
            foreach (var c in vol.GetComponents<MonoBehaviour>())
                if (c != null) sb.AppendLine($"  Volume obj component: {c.GetType().FullName}");

        return sb.ToString();
    }
}
