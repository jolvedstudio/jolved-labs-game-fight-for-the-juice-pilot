using System.Text;
using UnityEngine;

public class SurveyLevelElements
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        void Dump(string parent)
        {
            var go = GameObject.Find(parent);
            if (go == null) { sb.AppendLine(parent + ": NULL"); return; }
            sb.AppendLine($"== {parent} ({go.transform.childCount} children) ==");
            foreach (Transform c in go.transform)
            {
                var sr = c.GetComponent<SpriteRenderer>();
                string sz = sr != null ? $" size={sr.size}" : "";
                sb.AppendLine($"  {c.name} pos={c.position}{sz}");
            }
        }
        Dump("Level/Platforms");
        Dump("Level/Items");
        Dump("Level/Enemies");
        var start = GameObject.Find("Level/LevelStart");
        if (start != null) sb.AppendLine($"LevelStart pos={start.transform.position}");
        var gate = GameObject.Find("Level/GateToNextLevel");
        if (gate != null) sb.AppendLine($"Gate pos={gate.transform.position}");
        var dude = GameObject.Find("Level/Dude");
        if (dude != null) sb.AppendLine($"Dude pos={dude.transform.position}");
        return sb.ToString();
    }
}
