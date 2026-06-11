using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Game;

public class VerifyTorchAim
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        var torch = Object.FindFirstObjectByType<PlayerTorch>();
        if (torch == null) return "No PlayerTorch in scene (not playing or not spawned).";

        sb.AppendLine($"PlayerTorch found on {torch.gameObject.name}");
        sb.AppendLine($"  TurnSpeed={torch.TurnSpeed} MoveThreshold={torch.MoveThreshold} VerticalInfluence={torch.VerticalInfluence}");
        sb.AppendLine($"  FacingReference={(torch.FacingReference!=null?torch.FacingReference.name:"null")}");
        if (torch.TorchLight != null)
        {
            sb.AppendLine($"  Torch light enabled={torch.TorchLight.enabled}");
            sb.AppendLine($"  Torch localEuler.z = {torch.TorchLight.transform.localEulerAngles.z:F1}");
            sb.AppendLine($"  Torch worldPos = {torch.TorchLight.transform.position}");
        }
        return sb.ToString();
    }
}
