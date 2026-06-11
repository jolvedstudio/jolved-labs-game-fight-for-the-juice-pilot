using System.Text;
using UnityEngine;
using MoreMountains.CorgiEngine;

public class InspectRuntimePlayer
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        if (!Application.isPlaying) { sb.AppendLine("NOT in play mode"); }

        var chars = Object.FindObjectsOfType<Character>();
        sb.AppendLine($"Characters found: {chars.Length}");
        foreach (var c in chars)
        {
            sb.AppendLine($"--- {c.name} type={c.CharacterType} pos={c.transform.position}");
            if (c.CharacterType != Character.CharacterTypes.Player) continue;

            // Scale / model
            var model = c.transform.Find("ModelContainer");
            if (model != null)
            {
                var sr = model.GetComponent<SpriteRenderer>();
                sb.AppendLine($"   ModelContainer scale={model.localScale.x:0.000} sprite={(sr!=null && sr.sprite!=null ? sr.sprite.name : "null")} enabled={(sr!=null && sr.enabled)}");
            }

            // Health
            var h = c.GetComponent<Health>();
            if (h != null) sb.AppendLine($"   Health={h.CurrentHealth}/{h.MaximumHealth}");

            // Weapon handling
            var hw = c.FindAbility<CharacterHandleWeapon>();
            if (hw != null)
            {
                sb.AppendLine($"   HandleWeapon permitted={hw.AbilityPermitted} currentWeapon={(hw.CurrentWeapon!=null ? hw.CurrentWeapon.name : "NULL")}");
            }
            else sb.AppendLine("   HandleWeapon: MISSING");

            // Jetpack emitter still wired?
            var jp = c.FindAbility<CharacterJetpack>();
            if (jp != null) sb.AppendLine($"   Jetpack permitted={jp.AbilityPermitted}");
        }
        return sb.ToString();
    }
}
