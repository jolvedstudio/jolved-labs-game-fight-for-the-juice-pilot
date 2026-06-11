using System.Text;
using UnityEditor;
using UnityEngine;
using MoreMountains.Feedbacks;

public class InspectFeedbacks
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        string[] prefabs = {
            "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/Items/coin.prefab",
            "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/Items/stimpack.prefab",
            "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/LavaBot.prefab",
        };

        foreach (var p in prefabs)
        {
            var root = PrefabUtility.LoadPrefabContents(p);
            if (root == null) { sb.AppendLine("MISS " + p); continue; }
            try
            {
                sb.AppendLine("=== " + System.IO.Path.GetFileName(p));
                foreach (var player in root.GetComponentsInChildren<MMF_Player>(true))
                {
                    sb.AppendLine($"  Player on '{player.gameObject.name}' ({player.FeedbacksList.Count} feedbacks):");
                    foreach (var f in player.FeedbacksList)
                    {
                        string extra = "";
                        if (f is MMF_Sound snd)
                            extra = " Sfx=" + (snd.Sfx != null ? snd.Sfx.name : "NULL") +
                                    " RandomSfx=" + (snd.RandomSfx != null ? snd.RandomSfx.Length.ToString() : "0");
                        sb.AppendLine($"    - {f.GetType().Name} | label='{f.Label}'{extra}");
                    }
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        return sb.ToString();
    }
}
