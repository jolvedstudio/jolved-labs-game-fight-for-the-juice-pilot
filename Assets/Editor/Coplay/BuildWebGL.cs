using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildWebGL
{
    public static string Execute()
    {
        var enabledScenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (enabledScenes.Length == 0)
            return "ERROR: no enabled scenes in build settings";

        const string outputDir = "Builds/WebGL";

        // Reasonable WebGL settings for a 2D platformer demo build
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);

        var options = new BuildPlayerOptions
        {
            scenes = enabledScenes,
            locationPathName = outputDir,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        var sb = new StringBuilder();
        sb.AppendLine($"Build result: {summary.result}");
        sb.AppendLine($"Output: {summary.outputPath}");
        sb.AppendLine($"Total size: {summary.totalSize / (1024 * 1024)} MB");
        sb.AppendLine($"Total time: {summary.totalTime}");
        sb.AppendLine($"Errors: {summary.totalErrors} | Warnings: {summary.totalWarnings}");
        sb.AppendLine("Scenes built:");
        foreach (var s in enabledScenes) sb.AppendLine($"  - {s}");

        return sb.ToString();
    }
}
