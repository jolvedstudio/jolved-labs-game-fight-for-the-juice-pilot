using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class InvestigateAll
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // 1) Render pipeline
        var rp = GraphicsSettings.currentRenderPipeline;
        sb.AppendLine($"=== RENDER PIPELINE ===");
        sb.AppendLine($"currentRenderPipeline: {(rp!=null?rp.GetType().FullName:"(none / built-in)")}");
        if (rp != null) sb.AppendLine($"  asset: {AssetDatabase.GetAssetPath(rp)}");

        // Look for 2D renderer data assets
        sb.AppendLine("\n=== 2D Renderer Data assets ===");
        foreach (var g in AssetDatabase.FindAssets("t:ScriptableObject"))
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if (p.Contains("Renderer2D") || (p.EndsWith(".asset") && p.ToLower().Contains("2d") && p.ToLower().Contains("render")))
                sb.AppendLine($"  {p}");
        }
        // Also detect any existing Light2D in scene
        var light2dType = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
        sb.AppendLine($"\nLight2D type available: {light2dType != null}");

        // 2) NPC dialogue (the Dude)
        sb.AppendLine("\n=== NPC DIALOGUE (Level/Dude) ===");
        var dude = GameObject.Find("Level/Dude");
        if (dude != null)
        {
            foreach (var c in dude.GetComponentsInChildren<Component>(true))
            {
                if (c == null) continue;
                var so = new SerializedObject(c);
                var it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(it.stringValue) && it.stringValue.Length > 2)
                        sb.AppendLine($"  [{c.GetType().Name}] {it.name} = \"{it.stringValue}\"");
                }
            }
        }
        else sb.AppendLine("  Dude not found");

        // 3) Audio - background music & sound managers
        sb.AppendLine("\n=== AUDIO ===");
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            foreach (var c in go.GetComponents<Component>())
            {
                if (c == null) continue;
                var tn = c.GetType().Name;
                if (tn.Contains("BackgroundMusic") || tn.Contains("SoundManager") || tn.Contains("MMSoundManager") || tn == "AudioSource" || tn.Contains("Playlist"))
                {
                    sb.Append($"  [{GetPath(go.transform)}] {tn}");
                    if (c is AudioSource a)
                        sb.Append($" clip={(a.clip!=null?a.clip.name:"none")} playOnAwake={a.playOnAwake} loop={a.loop}");
                    sb.AppendLine();
                }
            }
        }

        return sb.ToString();
    }
    static string GetPath(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
}
