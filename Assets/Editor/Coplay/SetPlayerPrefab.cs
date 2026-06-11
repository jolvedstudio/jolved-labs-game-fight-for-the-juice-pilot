using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using MoreMountains.CorgiEngine;

public class SetPlayerPrefab
{
    public static string Execute()
    {
        const string variantPath = "Assets/Game/Prefabs/MechTrooperPlayer.prefab";
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
        if (go == null) return "ERROR: variant prefab not found";
        var character = go.GetComponent<Character>();
        if (character == null) return "ERROR: Character component not found on variant";

        var lm = Object.FindObjectOfType<LevelManager>();
        if (lm == null) return "ERROR: LevelManager not in scene";

        lm.PlayerPrefabs = new Character[] { character };
        EditorUtility.SetDirty(lm);
        EditorSceneManager.MarkSceneDirty(lm.gameObject.scene);
        EditorSceneManager.SaveScene(lm.gameObject.scene);

        return $"LevelManager.PlayerPrefabs set to {character.name} ({variantPath})";
    }
}
