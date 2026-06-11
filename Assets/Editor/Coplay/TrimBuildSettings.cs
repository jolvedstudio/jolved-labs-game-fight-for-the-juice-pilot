using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;

public class TrimBuildSettings
{
    public static string Execute()
    {
        // Desired enabled game flow, in order. StartScreen must be index 0.
        string[] gameFlow = {
            "Assets/Game/Scenes/StartScreen.unity",
            "Assets/CorgiEngine/Demos/Corgi2D/Lava.unity",
            "Assets/Game/Scenes/Level2.unity",
            "Assets/Game/Scenes/Level3.unity",
            "Assets/Game/Scenes/Win.unity",
        };

        // Loading screen scenes are referenced by name by the MMSceneLoadingManager
        // (LevelManager.LoadingSceneName = "LoadingScreen") and must remain in build.
        string[] requiredSupport = {
            "Assets/CorgiEngine/ThirdParty/MoreMountains/MMTools/Core/MMSceneLoading/LoadingScreens/LoadingScreen.unity",
        };

        var existing = EditorBuildSettings.scenes.ToDictionary(s => s.path, s => s);
        var newList = new List<EditorBuildSettingsScene>();
        var sb = new StringBuilder();

        // 1. Game flow first, enabled, in order
        foreach (var path in gameFlow)
        {
            if (existing.TryGetValue(path, out var sc))
                newList.Add(new EditorBuildSettingsScene(path, true));
            else
                newList.Add(new EditorBuildSettingsScene(path, true)); // still add even if not previously listed
            sb.AppendLine($"[{newList.Count - 1}] ENABLED  {path}");
        }

        // 2. Required support scenes, enabled, after the flow
        foreach (var path in requiredSupport)
        {
            newList.Add(new EditorBuildSettingsScene(path, true));
            sb.AppendLine($"[{newList.Count - 1}] ENABLED  {path}  (support)");
        }

        // 3. Keep every other previously-listed scene but DISABLED, so references/GUIDs
        //    are preserved without bloating the build or confusing scene indices.
        var keep = new HashSet<string>(gameFlow.Concat(requiredSupport));
        int disabledCount = 0;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (keep.Contains(s.path)) continue;
            newList.Add(new EditorBuildSettingsScene(s.path, false));
            disabledCount++;
        }

        EditorBuildSettings.scenes = newList.ToArray();

        sb.AppendLine($"\nTotal scenes: {newList.Count} | Enabled: {gameFlow.Length + requiredSupport.Length} | Disabled (preserved): {disabledCount}");
        return sb.ToString();
    }
}
