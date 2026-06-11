using System.Text;
using UnityEditor;
using UnityEngine;
using MoreMountains.CorgiEngine;

public class TunePlayerFeel
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        string prefabPath = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/PlayableCharacters/spine-space-corgi.prefab";

        // Load prefab contents for editing
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null) return "ERROR: could not load prefab " + prefabPath;

        try
        {
            var jump = root.GetComponent<CharacterJump>();
            if (jump != null)
            {
                jump.CoyoteTime = 0.12f;          // forgiving ledge jumps
                jump.InputBufferDuration = 0.15f; // buffered jump inputs land cleanly
                EditorUtility.SetDirty(jump);
                sb.AppendLine($"CharacterJump: CoyoteTime=0.12, InputBufferDuration=0.15 (was 0/0). NumberOfJumps={jump.NumberOfJumps}, JumpHeight={jump.JumpHeight}");
            }
            else sb.AppendLine("WARN: CharacterJump not found");

            // Save modifications back to the prefab asset
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return sb.ToString();
    }
}
