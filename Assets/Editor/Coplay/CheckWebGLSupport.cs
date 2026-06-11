using UnityEditor;
using UnityEngine;

public class CheckWebGLSupport
{
    public static string Execute()
    {
        bool supported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        return $"WebGL build support installed: {supported}\nActive target: {EditorUserBuildSettings.activeBuildTarget}";
    }
}
