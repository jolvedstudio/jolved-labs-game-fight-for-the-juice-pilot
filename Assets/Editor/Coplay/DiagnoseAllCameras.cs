using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class DiagnoseAllCameras
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        var cams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"Found {cams.Length} cameras:");
        foreach (var cam in cams)
        {
            var add = cam.GetComponent<UniversalAdditionalCameraData>();
            string renderType = add != null ? add.renderType.ToString() : "NO URP DATA";
            int stackCount = (add != null && add.cameraStack != null) ? add.cameraStack.Count : -1;
            sb.AppendLine($"- {GetPath(cam.transform)}");
            sb.AppendLine($"    active={cam.isActiveAndEnabled} depth={cam.depth} clear={cam.clearFlags} bg={cam.backgroundColor} cullMask={cam.cullingMask}");
            sb.AppendLine($"    URP renderType={renderType} stackCount={stackCount} targetTex={(cam.targetTexture!=null?cam.targetTexture.name:"null")}");
        }
        return sb.ToString();
    }
    static string GetPath(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
}
