using System.Text;
using UnityEngine;

public class FindPlayer
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        foreach (var c in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (c == null) continue;
            var tn = c.GetType().Name;
            if (tn == "Character")
            {
                // check PlayerID via reflection
                var f = c.GetType().GetField("CharacterType");
                sb.AppendLine($"Character: {GetPath(c.transform)}  type={(f!=null?f.GetValue(c):"?")}");
            }
        }
        // also list objects tagged Player
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            if (go.CompareTag("Player")) sb.AppendLine($"Tagged Player: {GetPath(go.transform)}");
        return sb.Length==0?"none found":sb.ToString();
    }
    static string GetPath(Transform t){string p=t.name;while(t.parent!=null){t=t.parent;p=t.name+"/"+p;}return p;}
}
