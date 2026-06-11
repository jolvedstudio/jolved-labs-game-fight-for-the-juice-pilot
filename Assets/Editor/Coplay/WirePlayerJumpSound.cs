using System.Linq;
using UnityEngine;
using UnityEditor;
using MoreMountains.Feedbacks;
using MoreMountains.CorgiEngine;

public class WirePlayerJumpSound
{
    public static string Execute()
    {
        const string prefabPath = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/PlayableCharacters/spine-space-corgi.prefab";
        const string clipPath = "Assets/Libraries/Soundbits_freeSFX_2025/Sounds/jw_whoosh-051.wav";

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        if (clip == null)
            return $"ERROR: could not load AudioClip at {clipPath}";

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var jump = root.GetComponentInChildren<CharacterJump>(true);
            if (jump == null)
                return "ERROR: CharacterJump not found on prefab";

            // Create or reuse a child to hold the feedback player
            Transform existing = root.transform.Find("JumpSoundFeedback");
            GameObject fbGo = existing != null ? existing.gameObject : new GameObject("JumpSoundFeedback");
            if (existing == null)
                fbGo.transform.SetParent(root.transform, false);

            var player = fbGo.GetComponent<MMF_Player>();
            if (player == null)
                player = fbGo.AddComponent<MMF_Player>();

            // Avoid duplicating if re-run
            bool alreadyHasSound = player.FeedbacksList != null &&
                player.FeedbacksList.OfType<MMF_Sound>().Any();
            if (!alreadyHasSound)
            {
                var sound = (MMF_Sound)player.AddFeedback(typeof(MMF_Sound));
                sound.Label = "JumpWhoosh";
                sound.Sfx = clip;
                sound.PlayMethod = MMF_Sound.PlayMethods.Event;
                sound.MinVolume = 0.7f;
                sound.MaxVolume = 0.8f;
                sound.MinPitch = 0.95f;
                sound.MaxPitch = 1.1f;
            }

            // Assign to the legacy MMFeedbacks field (MMF_Player IS-A MMFeedbacks)
            jump.AbilityStartFeedbacks = player;

            EditorUtility.SetDirty(root);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return $"Wired jump whoosh ({clip.name}) into spine-space-corgi CharacterJump.AbilityStartFeedbacks via MMF_Player+MMF_Sound.";
    }
}
