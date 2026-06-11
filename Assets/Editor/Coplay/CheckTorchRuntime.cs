using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Game;

public class CheckTorchRuntime
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        var torchCtrl = Object.FindFirstObjectByType<PlayerTorch>();
        if (torchCtrl == null) { sb.AppendLine("No PlayerTorch in scene (player not spawned?)."); }
        else
        {
            sb.AppendLine($"PlayerTorch found on {GetPath(torchCtrl.transform)}");
            sb.AppendLine($"  TorchLight set: {torchCtrl.TorchLight != null}");
            if (torchCtrl.TorchLight != null)
            {
                var l = torchCtrl.TorchLight;
                sb.AppendLine($"  Light enabled={l.enabled} type={l.lightType} intensity={l.intensity} outerRadius={l.pointLightOuterRadius}");
                sb.AppendLine($"  Light world pos={l.transform.position}");
            }
            sb.AppendLine($"  FacingReference: {(torchCtrl.FacingReference!=null?torchCtrl.FacingReference.name:"null")}");
        }
        return sb.ToString();
    }
    static string GetPath(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
}
