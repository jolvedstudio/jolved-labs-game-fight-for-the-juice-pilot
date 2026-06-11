using System.Linq;
using UnityEngine;
using UnityEditor;
using MoreMountains.Feedbacks;
using MoreMountains.CorgiEngine;

public class WirePlayerDeathSound
{
    public static string Execute()
    {
        const string prefabPath = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/PlayableCharacters/spine-space-corgi.prefab";
        const string clipPath = "Assets/Libraries/Soundbits_freeSFX_2025/Sounds/ji-d_impact-253.wav";

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        if (clip == null)
            return $"ERROR: could not load AudioClip at {clipPath}";

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var health = root.GetComponentInChildren<Health>(true);
            if (health == null)
                return "ERROR: Health not found on prefab";

            // health.DeathFeedbacks is typed as the base MMFeedbacks; the existing
            // DeathFeedbacks child is already an MMF_Player (verified). Cast to it.
            var player = health.DeathFeedbacks as MMF_Player;
            if (player == null)
            {
                // Fallback: create a dedicated MMF_Player if none/legacy is assigned.
                GameObject fbGo = new GameObject("DeathSoundFeedback");
                fbGo.transform.SetParent(root.transform, false);
                player = fbGo.AddComponent<MMF_Player>();
                health.DeathFeedbacks = player;
            }

            bool alreadyHasSound = player.FeedbacksList != null &&
                player.FeedbacksList.OfType<MMF_Sound>().Any(s => s != null && s.Label == "PlayerDeath");
            if (!alreadyHasSound)
            {
                var sound = (MMF_Sound)player.AddFeedback(typeof(MMF_Sound));
                sound.Label = "PlayerDeath";
                sound.Sfx = clip;
                sound.PlayMethod = MMF_Sound.PlayMethods.Event;
                sound.MinVolume = 0.85f;
                sound.MaxVolume = 1.0f;
                sound.MinPitch = 0.9f;
                sound.MaxPitch = 1.0f;
            }

            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return $"Wired player death impact ({clip.name}) into spine-space-corgi Health.DeathFeedbacks.";
    }
}
