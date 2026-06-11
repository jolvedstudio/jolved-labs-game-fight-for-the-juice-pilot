using System.Text;
using UnityEngine;

public class CheckPlayerRuntime
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.CompareTag("Player"))
                sb.AppendLine($"Player-tagged: {GetPath(go.transform)} active={go.activeInHierarchy}");
        }
        foreach (var c in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (c == null) continue;
            if (c.GetType().Name == "Character")
            {
                var f = c.GetType().GetField("CharacterType");
                var val = f != null ? f.GetValue(c).ToString() : "?";
                if (val.Contains("Player")) sb.AppendLine($"Player Character: {GetPath(c.transform)}");
            }
        }
        return sb.Length==0?"No player found in runtime scene":sb.ToString();
    }
    static string GetPath(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
}
