using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using System.Text;

public class ReskinEnemiesBatch
{
    public static string Execute()
    {
        const string ctrlPath = "Assets/Game/Animators/MechGunnerEnemy.controller";
        const string firstFrame = "Assets/TheMech/png/gunner/Walk__000.png";
        string[] targets = { "Level/Enemies/blueRobot 1", "Level/Enemies/blueRobot 2", "Level/Enemies/blueRobot 3" };

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (controller == null) return $"ERROR: controller not found at {ctrlPath}";

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(firstFrame);
        if (sprite == null)
        {
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(firstFrame))
                if (obj is Sprite s) { sprite = s; break; }
        }
        if (sprite == null) return $"ERROR: sprite not found at {firstFrame}";

        var sb = new StringBuilder();
        UnityEngine.SceneManagement.Scene scene = default;
        foreach (var path in targets)
        {
            var enemy = GameObject.Find(path);
            if (enemy == null) { sb.AppendLine($"SKIP: {path} not found"); continue; }
            scene = enemy.scene;

            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = sprite;
            var animator = enemy.GetComponent<Animator>();
            if (animator != null) animator.runtimeAnimatorController = controller;

            EditorUtility.SetDirty(enemy);
            sb.AppendLine($"OK: {path} -> Mech Gunner");
        }

        if (scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        return sb.ToString();
    }
}
