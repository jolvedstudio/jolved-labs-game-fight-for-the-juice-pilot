using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MoreMountains.CorgiEngine;

public class WireLevelChain
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        SetGateTarget("Assets/CorgiEngine/Demos/Corgi2D/Lava.unity", "Level2", sb);
        SetGateTarget("Assets/Game/Scenes/Level2.unity", "Level3", sb);
        SetGateTarget("Assets/Game/Scenes/Level3.unity", "Win", sb);

        return sb.ToString();
    }

    static void SetGateTarget(string scenePath, string nextLevel, StringBuilder sb)
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        FinishLevel gate = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            gate = root.GetComponentInChildren<FinishLevel>(true);
            if (gate != null) break;
        }
        if (gate == null)
        {
            sb.AppendLine("WARN: no FinishLevel found in " + scenePath);
            return;
        }
        gate.LevelName = nextLevel;
        gate.TriggerFade = true; // smooth fade transition
        EditorUtility.SetDirty(gate);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        sb.AppendLine($"{System.IO.Path.GetFileNameWithoutExtension(scenePath)} -> '{nextLevel}' (fade on)");
    }
}
