using UnityEngine;
using UnityEditor;
using System.Text;

public class InspectRobotAnimator
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // Check robot_0 animator parameters
        string animPath = "Assets/CorgiEngine/Demos/Corgi2D/Animations/AI/robot_0.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animPath);
        if (ctrl == null) return "ERROR: robot_0.controller not found";

        var animCtrl = ctrl as UnityEditor.Animations.AnimatorController;
        if (animCtrl != null)
        {
            sb.AppendLine("=== robot_0.controller parameters ===");
            foreach (var p in animCtrl.parameters)
                sb.AppendLine($"  {p.name} ({p.type})");

            sb.AppendLine("\n=== States ===");
            foreach (var layer in animCtrl.layers)
                foreach (var state in layer.stateMachine.states)
                    sb.AppendLine($"  [{layer.name}] {state.state.name}");
        }

        // Check blueRobot prefab - what layer/tag does it use
        string blueRobotPath = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/blueRobot.prefab";
        var blueRobot = AssetDatabase.LoadAssetAtPath<GameObject>(blueRobotPath);
        if (blueRobot != null)
        {
            sb.AppendLine($"\n=== blueRobot layer={blueRobot.layer} tag={blueRobot.tag} ===");
            var sr = blueRobot.GetComponent<SpriteRenderer>();
            sb.AppendLine($"  sprite={sr?.sprite?.name}");
            var anim = blueRobot.GetComponent<Animator>();
            sb.AppendLine($"  animator={anim?.runtimeAnimatorController?.name}");
        }

        // Check LavaBot scene instances - what layer are they on?
        sb.AppendLine("\n=== Scene LavaBot instances ===");
        var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allGOs)
        {
            if (go.name.StartsWith("LavaBot") && go.transform.parent?.name == "Enemies")
            {
                sb.AppendLine($"  {go.name}: layer={go.layer}({LayerMask.LayerToName(go.layer)}) tag={go.tag} active={go.activeSelf}");
                var sr = go.GetComponent<SpriteRenderer>();
                sb.AppendLine($"    sprite={(sr?.sprite != null ? sr.sprite.name : "NULL")}");
                var anim = go.GetComponent<Animator>();
                sb.AppendLine($"    animator={(anim?.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "NULL")}");
                sb.AppendLine($"    pos={go.transform.position}");
            }
        }

        return sb.ToString();
    }
}
