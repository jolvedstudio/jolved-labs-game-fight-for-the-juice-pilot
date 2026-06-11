using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

public class UrpStep1_CreatePipeline
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        string dir = "Assets/Game/Rendering";
        System.IO.Directory.CreateDirectory(dir);
        string rendererPath = dir + "/Renderer2D.asset";
        string urpAssetPath = dir + "/URP2D_PipelineAsset.asset";

        // 1) Create Renderer2DData
        var renderer2D = ScriptableObject.CreateInstance<Renderer2DData>();
        // assign default post process data via internal static method
        var ppType = typeof(UniversalRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.Universal.PostProcessData");
        var getDefaultPP = ppType.GetMethod("GetDefaultPostProcessData", BindingFlags.NonPublic | BindingFlags.Static);
        var ppData = getDefaultPP.Invoke(null, null);
        var ppField = typeof(Renderer2DData).GetProperty("postProcessData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (ppField != null && ppField.CanWrite) ppField.SetValue(renderer2D, ppData);
        AssetDatabase.CreateAsset(renderer2D, rendererPath);

        // reload package resources into renderer (shaders/textures)
        var coreUtilsAsm = typeof(CoreUtils).Assembly;
        var resourceReloader = coreUtilsAsm.GetType("UnityEngine.Rendering.ResourceReloader");
        var reloadMethod = resourceReloader.GetMethod("ReloadAllNullIn", BindingFlags.Public | BindingFlags.Static, null, new[]{typeof(UnityEngine.Object), typeof(string)}, null);
        reloadMethod.Invoke(null, new object[]{ renderer2D, "Packages/com.unity.render-pipelines.universal" });
        EditorUtility.SetDirty(renderer2D);

        // 2) Create URP pipeline asset referencing the 2D renderer
        var urp = UniversalRenderPipelineAsset.Create(renderer2D);
        AssetDatabase.CreateAsset(urp, urpAssetPath);
        EditorUtility.SetDirty(urp);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(rendererPath);
        AssetDatabase.ImportAsset(urpAssetPath);

        // 3) Assign as active pipeline (graphics + all quality levels)
        GraphicsSettings.defaultRenderPipeline = urp;
        int qCount = QualitySettings.names.Length;
        for (int i = 0; i < qCount; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urp;
        }

        AssetDatabase.SaveAssets();
        sb.AppendLine($"Created Renderer2D at {rendererPath}");
        sb.AppendLine($"Created URP asset at {urpAssetPath}");
        sb.AppendLine($"Assigned URP as active pipeline (graphics + {qCount} quality levels).");
        sb.AppendLine($"current pipeline now: {(GraphicsSettings.currentRenderPipeline!=null?GraphicsSettings.currentRenderPipeline.GetType().Name:"NULL")}");
        return sb.ToString();
    }
}
