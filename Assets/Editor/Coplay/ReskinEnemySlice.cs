using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

public class ReskinEnemySlice
{
    public static string Execute()
    {
        const string ctrlDir = "Assets/Game/Animators";
        const string ctrlPath = ctrlDir + "/MechGunnerEnemy.controller";
        const string walkClip = "Assets/TheMech/Animation/Gunner/Walk.anim";
        const string firstFrame = "Assets/TheMech/png/gunner/Walk__000.png";
        const string targetEnemy = "Level/Enemies/blueRobot"; // slice: first enemy only

        if (!Directory.Exists(ctrlDir)) Directory.CreateDirectory(ctrlDir);

        // 1. Build a faithful single-state controller (mirrors original robot_0: one looping walk state, no params)
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(walkClip);
        if (clip == null) return $"ERROR: walk clip not found at {walkClip}";

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            var sm = controller.layers[0].stateMachine;
            var walkState = sm.AddState("Walk");
            walkState.motion = clip;
            sm.defaultState = walkState;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }

        // 2. Swap onto the target enemy in the scene
        var enemy = GameObject.Find(targetEnemy);
        if (enemy == null) return $"ERROR: enemy not found at {targetEnemy}";

        var sr = enemy.GetComponent<SpriteRenderer>();
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(firstFrame);
        if (sprite == null)
        {
            // Fallback: scan all sub-assets at the path for the Sprite
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(firstFrame))
            {
                if (obj is Sprite s) { sprite = s; break; }
            }
        }
        string spriteResult = sprite != null ? sprite.name : "NULL (not found)";
        if (sr != null && sprite != null) sr.sprite = sprite;

        var animator = enemy.GetComponent<Animator>();
        if (animator != null) animator.runtimeAnimatorController = controller;

        EditorUtility.SetDirty(enemy);
        EditorSceneManager.MarkSceneDirty(enemy.scene);
        EditorSceneManager.SaveScene(enemy.scene);

        return $"SLICE: Reskinned '{targetEnemy}' -> Mech Gunner. Controller={ctrlPath}, sprite={spriteResult}, animator swapped. Corgi AI/Health/DamageOnTouch/colliders untouched.";
    }
}
