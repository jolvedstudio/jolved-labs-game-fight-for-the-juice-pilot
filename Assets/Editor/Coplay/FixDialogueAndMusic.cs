using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;

public class FixDialogueAndMusic
{
    public static string Execute()
    {
        var sb = new StringBuilder();

        // 1) Rewrite NPC dialogue to remove "lava" references -> abandoned facility theme
        var dude = GameObject.Find("Level/Dude");
        if (dude != null)
        {
            var dz = dude.GetComponentInChildren<DialogueZone>(true);
            if (dz != null)
            {
                var so = new SerializedObject(dz);
                var dialogueArray = so.FindProperty("Dialogue");
                if (dialogueArray != null && dialogueArray.isArray)
                {
                    string[] lines = {
                        "Hello again!",
                        "This is the abandoned facility.",
                        "Careful - the power's failing in here.",
                        "I'm sure you'll find \nthe exit easily.",
                        "(It's in the top right corner)"
                    };
                    dialogueArray.arraySize = lines.Length;
                    for (int i = 0; i < lines.Length; i++)
                        dialogueArray.GetArrayElementAtIndex(i).stringValue = lines[i];
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(dz);
                    sb.AppendLine($"Updated NPC dialogue ({lines.Length} lines), removed lava references.");
                }
                else sb.AppendLine("Could not find 'Dialogue' array on DialogueZone.");
            }
            else sb.AppendLine("No DialogueZone found on Dude.");
        }
        else sb.AppendLine("Dude not found.");

        // 2) Add BackgroundMusic to Lava (it was missing; L2/L3 have it)
        var existing = GameObject.Find("BackgroundMusic");
        if (existing == null)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/CorgiEngine/Common/Prefabs/Music/uniform-motion_there-is-no-way_instrumental.wav");
            var bgmGo = new GameObject("BackgroundMusic");
            var bgm = bgmGo.AddComponent<BackgroundMusic>();
            var so = new SerializedObject(bgm);
            so.FindProperty("SoundClip").objectReferenceValue = clip;
            var loop = so.FindProperty("Loop");
            if (loop != null) loop.boolValue = true;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(bgmGo);
            sb.AppendLine($"Added BackgroundMusic (clip={(clip!=null?clip.name:"NULL")}).");
        }
        else sb.AppendLine("BackgroundMusic already present.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        return sb.ToString();
    }
}
