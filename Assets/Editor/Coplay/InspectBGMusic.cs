using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class InspectBGMusic
{
    public static string Execute()
    {
        var sb = new StringBuilder();
        var scene = EditorSceneManager.OpenScene("Assets/Game/Scenes/Level2.unity", OpenSceneMode.Additive);
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var c in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (c == null) continue;
                if (c.GetType().Name.Contains("BackgroundMusic"))
                {
                    sb.AppendLine($"BackgroundMusic on {c.gameObject.name}, type={c.GetType().FullName}");
                    var so = new SerializedObject(c);
                    var it = so.GetIterator();
                    while (it.NextVisible(true))
                    {
                        if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue != null)
                            sb.AppendLine($"   {it.name} = {it.objectReferenceValue.name} ({AssetDatabase.GetAssetPath(it.objectReferenceValue)})");
                        else if (it.propertyType == SerializedPropertyType.Boolean)
                            sb.AppendLine($"   {it.name} = {it.boolValue}");
                    }
                }
            }
        }
        EditorSceneManager.CloseScene(scene, true);
        return sb.ToString();
    }
}
