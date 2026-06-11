using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MoreMountains.CorgiEngine;

/// <summary>
/// Fixes the LavaBot:
/// 1. Creates a new AnimatorController that does NOT bake sprite references
///    (uses only bool/float parameters driven by Corgi Engine)
/// 2. Assigns it to the prefab and all scene instances
/// 3. Fixes the CorgiController platform masks to match the scene
/// 4. Fixes the collider offset so the bot stands on platforms correctly
/// </summary>
public class FixLavaBotAnimatorAndPhysics
{
    const string LAVABOT_PREFAB_PATH  = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/LavaBot.prefab";
    const string ANIM_CTRL_SAVE_PATH  = "Assets/CorgiEngine/Demos/Corgi2D/Animations/AI/LavaBotAnimator.controller";
    const string SPRITE_PATH          = "Assets/CorgiEngine/Demos/Corgi2D/Sprites/Enemies/lavaBot.png";

    public static string Execute()
    {
        // ── 1. Create a new AnimatorController with no sprite-baked clips ────
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ANIM_CTRL_SAVE_PATH);

        // Add standard Corgi Engine parameters
        ctrl.AddParameter("Grounded",        AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("xSpeed",          AnimatorControllerParameterType.Float);
        ctrl.AddParameter("ySpeed",          AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Alive",           AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Idle",            AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Walking",         AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("FacingRight",     AnimatorControllerParameterType.Bool);

        // Single state: just plays — no sprite override, no clips
        var rootStateMachine = ctrl.layers[0].stateMachine;
        var idleState = rootStateMachine.AddState("Idle");
        idleState.motion = null; // no clip = no sprite override

        AssetDatabase.SaveAssets();

        // ── 2. Fix the prefab ────────────────────────────────────────────────
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);

        using (var editScope = new PrefabUtility.EditPrefabContentsScope(LAVABOT_PREFAB_PATH))
        {
            GameObject root = editScope.prefabContentsRoot;

            // Assign new animator controller
            Animator anim = root.GetComponent<Animator>();
            if (anim != null)
                anim.runtimeAnimatorController = ctrl;

            // Ensure sprite is set
            SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
            if (sr != null && sprite != null)
                sr.sprite = sprite;

            // Fix CorgiController - copy platform masks from blueRobot
            CorgiController cc = root.GetComponent<CorgiController>();
            if (cc != null)
            {
                // These match the LayerManager defaults used by all Corgi2D enemies
                cc.PlatformMask             = LayerManager.PlatformsLayerMask | LayerManager.PushablesLayerMask;
                cc.MovingPlatformMask       = LayerManager.MovingPlatformsLayerMask;
                cc.OneWayPlatformMask       = LayerManager.OneWayPlatformsLayerMask;
                cc.MovingOneWayPlatformMask = LayerManager.MovingOneWayPlatformsMask;
                cc.StairsMask               = LayerManager.StairsLayerMask;
                cc.AutomaticallySetPhysicsSettings = true;
                cc.AutomaticGravitySettings = true;
            }

            // Fix collider - match blueRobot exactly
            BoxCollider2D col = root.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.size   = new Vector2(1.92f, 1.57f);
                col.offset = new Vector2(0f, -0.16f);
            }
        }

        // ── 3. Fix all scene instances ───────────────────────────────────────
        int fixedCount = 0;
        var allGOs = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var go in allGOs)
        {
            if (!go.name.StartsWith("LavaBot")) continue;
            if (go.transform.parent?.name != "Enemies") continue;

            // Fix animator
            Animator anim = go.GetComponent<Animator>();
            if (anim != null)
                anim.runtimeAnimatorController = ctrl;

            // Fix sprite
            SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
            if (sr != null && sprite != null)
                sr.sprite = sprite;

            // Fix CorgiController masks
            CorgiController cc = go.GetComponent<CorgiController>();
            if (cc != null)
            {
                cc.PlatformMask             = LayerManager.PlatformsLayerMask | LayerManager.PushablesLayerMask;
                cc.MovingPlatformMask       = LayerManager.MovingPlatformsLayerMask;
                cc.OneWayPlatformMask       = LayerManager.OneWayPlatformsLayerMask;
                cc.MovingOneWayPlatformMask = LayerManager.MovingOneWayPlatformsMask;
                cc.StairsMask               = LayerManager.StairsLayerMask;
            }

            // Fix collider
            BoxCollider2D col = go.GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.size   = new Vector2(1.92f, 1.57f);
                col.offset = new Vector2(0f, -0.16f);
            }

            EditorUtility.SetDirty(go);
            fixedCount++;
        }

        // ── 4. Save ──────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return $"SUCCESS: Created LavaBotAnimator.controller (no sprite baking). " +
               $"Fixed {fixedCount} scene instances. Platform masks and colliders corrected.";
    }
}
