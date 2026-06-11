using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SetupGameScenes
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        const string srcLava = "Assets/CorgiEngine/Demos/Corgi2D/Lava.unity";
        const string gameDir = "Assets/Game/Scenes";

        if (!AssetDatabase.IsValidFolder("Assets/Game"))
            AssetDatabase.CreateFolder("Assets", "Game");
        if (!AssetDatabase.IsValidFolder(gameDir))
            AssetDatabase.CreateFolder("Assets/Game", "Scenes");

        string level2 = gameDir + "/Level2.unity";
        string level3 = gameDir + "/Level3.unity";

        if (!AssetDatabase.LoadAssetAtPath<Object>(level2))
        {
            AssetDatabase.CopyAsset(srcLava, level2);
            sb.AppendLine("Created " + level2);
        }
        else sb.AppendLine(level2 + " already exists");

        if (!AssetDatabase.LoadAssetAtPath<Object>(level3))
        {
            AssetDatabase.CopyAsset(srcLava, level3);
            sb.AppendLine("Created " + level3);
        }
        else sb.AppendLine(level3 + " already exists");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Add Level2 & Level3 to build settings (right after the Lava scene if present)
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool HasScene(string p) => scenes.Exists(s => s.path == p);

        if (!HasScene(level2)) scenes.Add(new EditorBuildSettingsScene(level2, true));
        if (!HasScene(level3)) scenes.Add(new EditorBuildSettingsScene(level3, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        sb.AppendLine("Build settings now contain " + scenes.Count + " scenes (Level2/Level3 registered).");

        return sb.ToString();
    }
}
