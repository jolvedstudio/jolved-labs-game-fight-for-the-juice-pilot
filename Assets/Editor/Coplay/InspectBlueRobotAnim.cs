using UnityEngine;
using UnityEditor;
using System.Text;

public class InspectBlueRobotAnim
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // Inspect blueRobotWalk.anim
        string animPath = "Assets/CorgiEngine/Demos/Corgi2D/Animations/AI/blueRobotWalk.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(animPath);
        if (clip == null) return "ERROR: blueRobotWalk.anim not found";

        sb.AppendLine($"Clip: {clip.name}, length={clip.length}s, frameRate={clip.frameRate}");
        sb.AppendLine($"isLooping={clip.isLooping}, wrapMode={clip.wrapMode}");

        // Get all bindings
        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        sb.AppendLine($"\nObject Reference Bindings ({bindings.Length}):");
        foreach (var binding in bindings)
        {
            sb.AppendLine($"  path='{binding.path}' prop='{binding.propertyName}' type={binding.type.Name}");
            var keyframes = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            sb.AppendLine($"  keyframes: {keyframes.Length}");
            foreach (var kf in keyframes)
            {
                var sprite = kf.value as Sprite;
                sb.AppendLine($"    t={kf.time:F3}  sprite={(sprite != null ? sprite.name : "null")}");
            }
        }

        // Check the robot sprite sheet
        string robotSpritePath = "Assets/CorgiEngine/Demos/Corgi2D/Sprites/Enemies/robot.png";
        var allSprites = AssetDatabase.LoadAllAssetsAtPath(robotSpritePath);
        sb.AppendLine($"\nrobot.png sub-assets ({allSprites.Length}):");
        foreach (var asset in allSprites)
            if (asset is Sprite s)
                sb.AppendLine($"  sprite: {s.name}  rect={s.rect}");

        return sb.ToString();
    }
}
