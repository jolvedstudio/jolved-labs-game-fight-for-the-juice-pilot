using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-time recovery: copy the (currently treated) lava scene into our standalone
/// game folder as Level1, then fix its 'next level' to point at Level2.
/// Leaves the original Corgi demo file untouched after this.
/// </summary>
public class RecoverLevel1
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        string src = "Assets/CorgiEngine/Demos/Corgi2D/Lava.unity";
        string dst = "Assets/Game/Scenes/Level1.unity";

        if (System.IO.File.Exists(dst))
            AssetDatabase.DeleteAsset(dst);

        bool ok = AssetDatabase.CopyAsset(src, dst);
        AssetDatabase.Refresh();
        sb.AppendLine($"Copied treated lava layout -> {dst} (ok={ok})");

        // Open the new standalone scene and set the gate's next level to Level2
        var scene = EditorSceneManager.OpenScene(dst, OpenSceneMode.Single);
        var lm = GameObject.Find("LevelManager");
        // The LevelName lives on a GoToLevelEntryPoint / GotoNextLevel component on the gate.
        // We rename through reflection-safe search of all MonoBehaviours that expose a LevelName field.
        int patched = 0;
        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            var f = mb.GetType().GetField("LevelName");
            if (f != null && f.FieldType == typeof(string))
            {
                string cur = (string)f.GetValue(mb);
                if (cur == "Win" || cur == "Level2" || cur == "Lava" || cur == "Level3")
                {
                    f.SetValue(mb, "Level2");
                    EditorUtility.SetDirty(mb);
                    patched++;
                }
            }
        }
        sb.AppendLine($"Patched next-level -> Level2 on {patched} component(s).");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, dst);
        sb.AppendLine("Saved Level1 to its own path.");
        return sb.ToString();
    }
}
