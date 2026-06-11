using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

public class RebindHumanClips
{
    const string SpriteDir = "Assets/Chars/Sci Fi Character 2D/Player (Red)/Sprites";
    const string AnimDir = "Assets/Chars/Sci Fi Character 2D/Player (Red)/Animations";

    // Maps clip name -> sprite filename prefix (followed immediately by digits)
    static readonly Dictionary<string,string> ClipToPrefix = new Dictionary<string,string>
    {
        {"Idle","Idle"},
        {"Run","Walk"},
        {"Jump","Jump"},
        {"Shoot","Shoot"},
        {"Crouch","Crouch"},
        {"CrouchShoot","CrouchShoot"},
        {"Dash","Dash"},
        {"Death","Death"},
        {"Hurt","Hurt"},
    };

    static List<Sprite> SpritesForPrefix(string prefix)
    {
        // match files like Prefix0001.png but not PrefixOther0001 (prefix immediately followed by digits)
        var regex = new Regex("^" + Regex.Escape(prefix) + @"\d+$");
        var guids = AssetDatabase.FindAssets("t:Sprite", new[]{ SpriteDir });
        var result = new List<(string name, Sprite sp)>();
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var fname = System.IO.Path.GetFileNameWithoutExtension(path);
            // strip " - Copy" duplicates
            if (fname.Contains(" - Copy")) continue;
            if (!regex.IsMatch(fname)) continue;
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) result.Add((fname, sp));
        }
        // sort numerically by trailing digits
        return result
            .OrderBy(r => int.Parse(Regex.Match(r.name, @"\d+$").Value))
            .Select(r => r.sp)
            .ToList();
    }

    static string FixClip(string clipName)
    {
        string path = AnimDir + "/" + clipName + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) return $"{clipName}: clip not found";

        if (!ClipToPrefix.TryGetValue(clipName, out var prefix))
            return $"{clipName}: no prefix mapping";

        var sprites = SpritesForPrefix(prefix);
        if (sprites.Count == 0) return $"{clipName}: NO sprites for prefix {prefix}";

        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        EditorCurveBinding binding;
        if (bindings.Length > 0)
        {
            binding = bindings[0];
        }
        else
        {
            binding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
        }

        // Preserve existing keyframe TIMES if present; otherwise build fresh at clip sample rate.
        var existing = AnimationUtility.GetObjectReferenceCurve(clip, binding);
        ObjectReferenceKeyframe[] keys;
        if (existing != null && existing.Length > 0)
        {
            keys = new ObjectReferenceKeyframe[existing.Length];
            for (int i = 0; i < existing.Length; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = existing[i].time,
                    value = sprites[i % sprites.Count]
                };
            }
        }
        else
        {
            float fps = clip.frameRate > 0 ? clip.frameRate : 12f;
            keys = new ObjectReferenceKeyframe[sprites.Count];
            for (int i = 0; i < sprites.Count; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        EditorUtility.SetDirty(clip);

        return $"{clipName}: prefix={prefix} sprites={sprites.Count} keys={keys.Length} firstSprite={sprites[0].name}";
    }

    public static string Execute()
    {
        var sb = new StringBuilder();
        foreach (var name in new[]{ "Idle","Run","Jump","Shoot","Crouch","CrouchShoot","Dash","Death","Hurt" })
            sb.AppendLine(FixClip(name));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return sb.ToString();
    }
}
