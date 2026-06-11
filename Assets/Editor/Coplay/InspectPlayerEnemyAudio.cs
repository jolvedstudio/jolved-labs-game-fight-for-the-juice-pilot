using System.Text;
using UnityEditor;
using UnityEngine;
using MoreMountains.Feedbacks;
using MoreMountains.CorgiEngine;

public class InspectPlayerEnemyAudio
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        string[] prefabs = {
            "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/PlayableCharacters/spine-space-corgi.prefab",
            "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/LavaBot.prefab",
        };

        foreach (var p in prefabs)
        {
            var root = PrefabUtility.LoadPrefabContents(p);
            if (root == null) { sb.AppendLine("MISS " + p); continue; }
            try
            {
                sb.AppendLine("=== " + System.IO.Path.GetFileName(p));
                sb.AppendLine("  -- All MMF_Players in hierarchy --");
                foreach (var player in root.GetComponentsInChildren<MMF_Player>(true))
                {
                    var sounds = "";
                    foreach (var f in player.FeedbacksList)
                        if (f is MMF_Sound snd) sounds += $"[{(snd.Sfx? snd.Sfx.name : "NULL")}]";
                    sb.AppendLine($"    Player '{GetPath(player.transform, root.transform)}' fb={player.FeedbacksList.Count} sounds={sounds}");
                }

                // Jump ability feedback references
                var jump = root.GetComponent<CharacterJump>();
                if (jump != null)
                    sb.AppendLine($"  CharacterJump.abilityStartFeedbacks={(jump.AbilityStartFeedbacks? jump.AbilityStartFeedbacks.name : "None")}");

                var hmove = root.GetComponent<CharacterHorizontalMovement>();
                if (hmove != null)
                    sb.AppendLine($"  HMove.touchTheGroundFeedback set={(hmove.TouchTheGroundFeedback!=null)}");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        return sb.ToString();
    }

    static string GetPath(Transform t, Transform root)
    {
        string path = t.name;
        while (t.parent != null && t != root) { t = t.parent; if (t!=root) path = t.name + "/" + path; }
        return path;
    }
}
