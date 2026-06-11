using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MoreMountains.Feedbacks;
using MoreMountains.CorgiEngine;

public class ApplyAudioToLevels
{
    const string MusicPrefab = "Assets/CorgiEngine/Common/Prefabs/Music/BgMusicUniformMotionThereIsNoWay.prefab";
    const string StingerClip = "Assets/Libraries/Soundbits_freeSFX_2025/Sounds/cht2_cinematic_transition_101.wav";

    public static string Execute()
    {
        var sb = new StringBuilder();
        string[] scenes = {
            "Assets/Game/Scenes/Level2.unity",
            "Assets/Game/Scenes/Level3.unity"
        };

        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(StingerClip);
        var musicPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MusicPrefab);
        if (clip == null) return "ERROR: stinger clip not found";
        if (musicPrefab == null) return "ERROR: music prefab not found";

        foreach (var scenePath in scenes)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // --- Background music ---
            var existingMusic = GameObject.Find("BackgroundMusic");
            if (existingMusic == null || existingMusic.GetComponent<BackgroundMusic>() == null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(musicPrefab);
                inst.name = "BackgroundMusic";
                sb.AppendLine($"{scenePath}: added BackgroundMusic");
            }
            else sb.AppendLine($"{scenePath}: music already present");

            // --- Level-complete stinger ---
            var finish = Object.FindObjectsByType<FinishLevel>(FindObjectsSortMode.None).FirstOrDefault();
            if (finish == null)
            {
                sb.AppendLine($"{scenePath}: WARNING no FinishLevel found");
            }
            else
            {
                var player = finish.ActivationFeedback as MMF_Player;
                if (player == null)
                {
                    var go = new GameObject("LevelCompleteStinger");
                    go.transform.SetParent(finish.transform, false);
                    player = go.AddComponent<MMF_Player>();
                    finish.ActivationFeedback = player;
                }
                bool has = player.FeedbacksList != null &&
                    player.FeedbacksList.OfType<MMF_Sound>().Any(s => s != null && s.Label == "LevelComplete");
                if (!has)
                {
                    var sound = (MMF_Sound)player.AddFeedback(typeof(MMF_Sound));
                    sound.Label = "LevelComplete";
                    sound.Sfx = clip;
                    sound.PlayMethod = MMF_Sound.PlayMethods.Event;
                    sound.MinVolume = 0.9f; sound.MaxVolume = 1.0f;
                    sb.AppendLine($"{scenePath}: added stinger to '{finish.name}' (next='{finish.LevelName}')");
                }
                else sb.AppendLine($"{scenePath}: stinger already present");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // Reopen Lava so the editor returns to the working scene
        EditorSceneManager.OpenScene("Assets/CorgiEngine/Demos/Corgi2D/Lava.unity", OpenSceneMode.Single);

        return sb.ToString();
    }
}
