using System.Linq;
using UnityEngine;
using UnityEditor;
using MoreMountains.Feedbacks;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;

public class WireLevelCompleteStinger
{
    public static string Execute()
    {
        const string clipPath = "Assets/Libraries/Soundbits_freeSFX_2025/Sounds/cht2_cinematic_transition_101.wav";

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        if (clip == null)
            return $"ERROR: could not load AudioClip at {clipPath}";

        var gate = GameObject.Find("Level/GateToNextLevel");
        if (gate == null)
        {
            // Fallback: locate by component
            var finishAll = Object.FindObjectsByType<FinishLevel>(FindObjectsSortMode.None);
            gate = finishAll.Length > 0 ? finishAll[0].gameObject : null;
        }
        if (gate == null)
            return "ERROR: GateToNextLevel / FinishLevel not found in scene";

        var finish = gate.GetComponent<FinishLevel>();
        if (finish == null)
            return "ERROR: FinishLevel component not found on gate";

        // activationFeedback is the base MMFeedbacks; create an MMF_Player child for it.
        var player = finish.ActivationFeedback as MMF_Player;
        if (player == null)
        {
            Transform existing = gate.transform.Find("LevelCompleteStinger");
            GameObject fbGo = existing != null ? existing.gameObject : new GameObject("LevelCompleteStinger");
            if (existing == null)
                fbGo.transform.SetParent(gate.transform, false);
            player = fbGo.GetComponent<MMF_Player>();
            if (player == null)
                player = fbGo.AddComponent<MMF_Player>();
            finish.ActivationFeedback = player;
        }

        bool alreadyHasSound = player.FeedbacksList != null &&
            player.FeedbacksList.OfType<MMF_Sound>().Any(s => s != null && s.Label == "LevelComplete");
        if (!alreadyHasSound)
        {
            var sound = (MMF_Sound)player.AddFeedback(typeof(MMF_Sound));
            sound.Label = "LevelComplete";
            sound.Sfx = clip;
            sound.PlayMethod = MMF_Sound.PlayMethods.Event;
            sound.MinVolume = 0.9f;
            sound.MaxVolume = 1.0f;
            sound.MinPitch = 1.0f;
            sound.MaxPitch = 1.0f;
        }

        EditorUtility.SetDirty(gate);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gate.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(gate.scene);

        return $"Wired level-complete stinger ({clip.name}) into FinishLevel.ActivationFeedback on GateToNextLevel.";
    }
}
