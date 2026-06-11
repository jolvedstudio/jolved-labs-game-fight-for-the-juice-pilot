using System.Text;
using UnityEngine;
using UnityEditor;

public class FindPlayerPrefab
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        var lm = GameObject.Find("LevelManager");
        if (lm == null) lm = GameObject.Find("Level/LevelManagers");
        foreach (var c in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (c == null) continue;
            if (c.GetType().Name == "LevelManager")
            {
                sb.AppendLine($"LevelManager on {c.gameObject.name}");
                var so = new SerializedObject(c);
                var it = so.GetIterator();
                while (it.NextVisible(true))
                {
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null)
                    {
                        var path = AssetDatabase.GetAssetPath(it.objectReferenceValue);
                        sb.AppendLine($"   {it.propertyPath} = {it.objectReferenceValue.name} [{path}]");
                    }
                    if (it.isArray && it.propertyType == SerializedPropertyType.Generic && (it.name=="PlayerPrefabs"||it.name.Contains("Player")))
                    {
                        sb.AppendLine($"   array {it.name} size={it.arraySize}");
                        for (int i=0;i<it.arraySize;i++){
                            var e=it.GetArrayElementAtIndex(i);
                            if(e.propertyType==SerializedPropertyType.ObjectReference&&e.objectReferenceValue!=null)
                                sb.AppendLine($"      [{i}] {e.objectReferenceValue.name} [{AssetDatabase.GetAssetPath(e.objectReferenceValue)}]");
                        }
                    }
                }
            }
        }
        return sb.Length==0?"No LevelManager found":sb.ToString();
    }
}
