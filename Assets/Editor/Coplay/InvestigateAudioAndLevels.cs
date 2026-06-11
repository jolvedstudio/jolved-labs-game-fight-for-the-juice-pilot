using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class InvestigateAudioAndLevels
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // Search active scene for any AudioSource and BackgroundMusic-like components more deeply
        sb.AppendLine("=== Active scene: all AudioSources & music ===");
        foreach (var a in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            sb.AppendLine($"  {GetPath(a.transform)} clip={(a.clip!=null?a.clip.name:"none")} loop={a.loop} playOnAwake={a.playOnAwake}");

        // Look at MMSoundManager settings (does it have music tracks?)
        var mm = GameObject.Find("MMSoundManager");
        if (mm != null)
        {
            sb.AppendLine("\n=== MMSoundManager components ===");
            foreach (var c in mm.GetComponents<Component>())
                sb.AppendLine($"  {c.GetType().FullName}");
        }

        // Any component named BackgroundMusic anywhere
        sb.AppendLine("\n=== BackgroundMusic components (by type name) ===");
        foreach (var c in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (c == null) continue;
            if (c.GetType().Name.Contains("BackgroundMusic"))
            {
                var so = new SerializedObject(c);
                var sp = so.FindProperty("SoundClip") ?? so.FindProperty("MusicClip") ?? so.FindProperty("musicClip");
                sb.AppendLine($"  {GetPath(c.transform)} [{c.GetType().Name}] clip={(sp!=null&&sp.objectReferenceValue!=null?sp.objectReferenceValue.name:"?")}");
            }
        }

        // L2/L3 platform sprite paths
        sb.AppendLine("\n=== Level2 / Level3 platform textures ===");
        foreach (var p in new[]{"Assets/Game/Scenes/Level2.unity","Assets/Game/Scenes/Level3.unity"})
        {
            sb.AppendLine($"-- {p} --");
            var scene = EditorSceneManager.OpenScene(p, OpenSceneMode.Additive);
            var texPaths = new System.Collections.Generic.HashSet<string>();
            string audioInfo = "";
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.sprite != null) texPaths.Add(AssetDatabase.GetAssetPath(sr.sprite));
                foreach (var a in root.GetComponentsInChildren<AudioSource>(true))
                    audioInfo += $"\n    AudioSource {GetPath(a.transform)} clip={(a.clip!=null?a.clip.name:"none")}";
                foreach (var c in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (c != null && c.GetType().Name.Contains("BackgroundMusic"))
                        audioInfo += $"\n    BackgroundMusic on {GetPath(c.transform)}";
            }
            foreach (var tp in texPaths.OrderBy(x=>x))
                sb.AppendLine($"    tex: {tp}");
            sb.AppendLine($"    audio:{(audioInfo==""?" none":audioInfo)}");
            EditorSceneManager.CloseScene(scene, true);
        }

        return sb.ToString();
    }
    static string GetPath(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
}
