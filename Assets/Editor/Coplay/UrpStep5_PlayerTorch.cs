using System.Text;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEditor;
using Game;

public class UrpStep5_PlayerTorch
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        string prefabPath = "Assets/Game/Prefabs/MechTrooperPlayer.prefab";
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // Remove any pre-existing torch from earlier runs
            var oldTorch = root.transform.Find("Torch");
            if (oldTorch != null) Object.DestroyImmediate(oldTorch.gameObject);

            // Create torch light child
            var torchGo = new GameObject("Torch");
            torchGo.transform.SetParent(root.transform, false);
            // Centered origin: the torch now aims in any direction, so emit from the
            // player's center rather than offset to one side.
            torchGo.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            var light = torchGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Point;          // spot light
            light.color = new Color(1f, 0.85f, 0.55f);          // warm torch
            light.intensity = 1.5f;
            light.pointLightInnerRadius = 0.5f;
            light.pointLightOuterRadius = 9f;
            light.pointLightInnerAngle = 55f;                   // cone
            light.pointLightOuterAngle = 80f;
            light.falloffIntensity = 0.5f;
            light.shadowsEnabled = true;
            light.shadowIntensity = 0.75f;
            // point the cone to the right by default (will be flipped by script)
            torchGo.transform.localEulerAngles = new Vector3(0, 0, 0);

            // Add the torch controller on the root and wire references
            var torchCtrl = root.GetComponent<PlayerTorch>();
            if (torchCtrl == null) torchCtrl = root.AddComponent<PlayerTorch>();
            torchCtrl.TorchLight = light;
            torchCtrl.StartOn = true;
            torchCtrl.TurnSpeed = 540f;
            torchCtrl.MoveThreshold = 0.5f;
            torchCtrl.VerticalInfluence = 0.8f;
            var model = root.transform.Find("ModelContainer");
            torchCtrl.FacingReference = model != null ? model : null;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            sb.AppendLine("Added Torch (spot Light2D) + PlayerTorch controller to MechTrooperPlayer prefab.");
            sb.AppendLine($"FacingReference = {(model!=null?model.name:"null")}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
        return sb.ToString();
    }
}
