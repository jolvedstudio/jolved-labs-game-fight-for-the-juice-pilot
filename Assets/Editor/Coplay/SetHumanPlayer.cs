using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MoreMountains.CorgiEngine;

public class SetHumanPlayer
{
    public static string Execute()
    {
        const string variantPath = "Assets/Game/Prefabs/SciFiHumanPlayer.prefab";
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
        if (go == null) return "ERROR: human variant not found";
        var character = go.GetComponent<Character>();
        if (character == null) return "ERROR: Character missing on variant";

        var lm = Object.FindObjectOfType<LevelManager>();
        if (lm == null) return "ERROR: LevelManager not in scene";

        var so = new SerializedObject(lm);
        var prop = so.FindProperty("PlayerPrefabs");
        prop.arraySize = 1;
        prop.GetArrayElementAtIndex(0).objectReferenceValue = character;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(lm);
        EditorSceneManager.MarkSceneDirty(lm.gameObject.scene);
        EditorSceneManager.SaveScene(lm.gameObject.scene);
        return $"LevelManager.PlayerPrefabs[0] = {character.name} ({variantPath})";
    }
}
