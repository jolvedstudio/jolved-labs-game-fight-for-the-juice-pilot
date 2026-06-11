using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.Text;

public class DiagnoseEnemies
{
    public static string Execute()
    {
        const string ctrlPath = "Assets/Game/Animators/MechGunnerEnemy.controller";
        const string firstFrame = "Assets/TheMech/png/gunner/Walk__000.png";
        string[] targets = {
            "Level/Enemies/blueRobot",
            "Level/Enemies/blueRobot 1",
            "Level/Enemies/blueRobot 2",
            "Level/Enemies/blueRobot 3"
        };

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(firstFrame);
        if (sprite == null)
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(firstFrame))
                if (obj is Sprite s) { sprite = s; break; }

        var sb = new StringBuilder();
        sb.AppendLine($"Loaded sprite: {(sprite != null ? sprite.name : "NULL")} (type {(sprite != null ? sprite.GetType().Name : "-")})");

        UnityEngine.SceneManagement.Scene scene = default;
        foreach (var path in targets)
        {
            var enemy = GameObject.Find(path);
            if (enemy == null) { sb.AppendLine($"{path}: NOT FOUND"); continue; }
            scene = enemy.scene;
            var sr = enemy.GetComponent<SpriteRenderer>();
            var an = enemy.GetComponent<Animator>();

            string before = sr != null && sr.sprite != null ? sr.sprite.name : "null";

            if (sr != null && sprite != null)
            {
                sr.sprite = sprite;
                Undo.RecordObject(sr, "set sprite");
                EditorUtility.SetDirty(sr);
            }
            if (an != null && controller != null) an.runtimeAnimatorController = controller;
            EditorUtility.SetDirty(enemy);

            // Check prefab status
            bool isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(enemy);

            string after = sr != null && sr.sprite != null ? sr.sprite.name : "null";
            sb.AppendLine($"{path}: before='{before}' after='{after}' prefabInstance={isPrefabInstance}");
        }

        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            bool saved = EditorSceneManager.SaveScene(scene);
            sb.AppendLine($"Scene saved: {saved}");
        }
        return sb.ToString();
    }
}
