using System.Text;
using UnityEditor;
using UnityEngine;
using MoreMountains.Feedbacks;

public class WireAudioFeedbacks
{
    const string SfxDir = "Assets/Casual Game Sounds U6/CasualGameSounds/";

    public static string Execute()
    {
        var sb = new StringBuilder();

        // event -> (prefab path, child holding MMF_Player, clip file)
        AddSoundToPrefab("Assets/CorgiEngine/Demos/Corgi2D/Prefabs/Items/coin.prefab",
            "PickFeedback", "DM-CGS-20.wav", "Coin", sb);

        AddSoundToPrefab("Assets/CorgiEngine/Demos/Corgi2D/Prefabs/Items/stimpack.prefab",
            "PickFeedback", "DM-CGS-06.wav", "Stimpack", sb);

        return sb.ToString();
    }

    static void AddSoundToPrefab(string prefabPath, string childWithPlayer, string clipFile, string label, StringBuilder sb)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(SfxDir + clipFile);
        if (clip == null) { sb.AppendLine($"WARN[{label}]: clip not found {clipFile}"); return; }

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) { sb.AppendLine($"WARN[{label}]: prefab not loaded {prefabPath}"); return; }

        try
        {
            // find the MMF_Player (search the named child, fallback to any in children)
            MMF_Player player = null;
            var t = root.transform.Find(childWithPlayer);
            if (t != null) player = t.GetComponent<MMF_Player>();
            if (player == null) player = root.GetComponentInChildren<MMF_Player>(true);
            if (player == null) { sb.AppendLine($"WARN[{label}]: no MMF_Player found"); return; }

            // avoid duplicate sound feedbacks if re-run
            bool already = player.FeedbacksList != null && player.FeedbacksList.Exists(f => f is MMF_Sound);
            if (already) { sb.AppendLine($"[{label}]: MMF_Sound already present, skipping"); return; }

            var sound = player.AddFeedback(typeof(MMF_Sound)) as MMF_Sound;
            sound.Label = label + " SFX";
            sound.Sfx = clip;
            sound.MinVolume = 0.9f;
            sound.MaxVolume = 1f;

            EditorUtility.SetDirty(player);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            sb.AppendLine($"[{label}]: added MMF_Sound '{clipFile}' to {childWithPlayer}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
