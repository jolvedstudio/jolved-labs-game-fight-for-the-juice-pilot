using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using MoreMountains.CorgiEngine;

/// <summary>
/// 1. Slices lavaBotWalk.png into 8 equal frames
/// 2. Creates LavaBotWalk.anim and LavaBotIdle.anim clips
/// 3. Updates LavaBotAnimator.controller with Idle/Walk states driven by xSpeed
/// 4. Fixes CharacterHandleWeapon so weapon equips at runtime
/// 5. Updates prefab and all scene instances
/// </summary>
public class SetupLavaBotAnimationAndWeapon
{
    const string WALK_SHEET_PATH  = "Assets/CorgiEngine/Demos/Corgi2D/Sprites/Enemies/lavaBotWalk.png";
    const string IDLE_SPRITE_PATH = "Assets/CorgiEngine/Demos/Corgi2D/Sprites/Enemies/lavaBot.png";
    const string ANIM_DIR         = "Assets/CorgiEngine/Demos/Corgi2D/Animations/AI";
    const string CTRL_PATH        = "Assets/CorgiEngine/Demos/Corgi2D/Animations/AI/LavaBotAnimator.controller";
    const string PREFAB_PATH      = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/LavaBot.prefab";
    const string WEAPON_PATH      = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/Weapons/RobotWeapon.prefab";
    const int    FRAME_COUNT      = 8;
    const float  FRAME_RATE       = 12f;

    public static string Execute()
    {
        // ── 1. Slice the walk spritesheet into 8 equal frames ────────────────
        TextureImporter importer = AssetImporter.GetAtPath(WALK_SHEET_PATH) as TextureImporter;
        if (importer == null) return "ERROR: lavaBotWalk.png not found";

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(WALK_SHEET_PATH);
        int frameW = tex.width / FRAME_COUNT;
        int frameH = tex.height;

        importer.textureType         = TextureImporterType.Sprite;
        importer.spriteImportMode    = SpriteImportMode.Multiple;
        importer.alphaSource         = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.isReadable          = false;
        importer.mipmapEnabled       = false;
        importer.filterMode          = FilterMode.Point;
        importer.textureCompression  = TextureImporterCompression.Uncompressed;

        var spriteMetaData = new SpriteMetaData[FRAME_COUNT];
        for (int i = 0; i < FRAME_COUNT; i++)
        {
            spriteMetaData[i] = new SpriteMetaData
            {
                name   = $"lavaBotWalk_{i}",
                rect   = new Rect(i * frameW, 0, frameW, frameH),
                pivot  = new Vector2(0.5f, 0.5f),
                alignment = (int)SpriteAlignment.Center
            };
        }
        importer.spritesheet = spriteMetaData;
        importer.SaveAndReimport();
        AssetDatabase.Refresh();

        // ── 2. Load sliced sprites ───────────────────────────────────────────
        var walkSprites = new List<Sprite>();
        var allAssets = AssetDatabase.LoadAllAssetsAtPath(WALK_SHEET_PATH);
        foreach (var asset in allAssets)
            if (asset is Sprite s) walkSprites.Add(s);
        walkSprites.Sort((a, b) => string.Compare(a.name, b.name,
            System.StringComparison.OrdinalIgnoreCase));

        if (walkSprites.Count == 0) return "ERROR: No sprites found after slicing";

        // Load idle sprite
        Sprite idleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(IDLE_SPRITE_PATH);
        if (idleSprite == null) return "ERROR: lavaBot.png idle sprite not found";

        // ── 3. Create LavaBotIdle.anim ───────────────────────────────────────
        string idleClipPath = $"{ANIM_DIR}/LavaBotIdle.anim";
        AnimationClip idleClip = new AnimationClip();
        idleClip.frameRate = FRAME_RATE;
        AnimationUtility.SetAnimationClipSettings(idleClip,
            new AnimationClipSettings { loopTime = true });

        var idleBinding = new EditorCurveBinding
        {
            type         = typeof(SpriteRenderer),
            path         = "",
            propertyName = "m_Sprite"
        };
        var idleKeys = new ObjectReferenceKeyframe[]
        {
            new ObjectReferenceKeyframe { time = 0f, value = idleSprite }
        };
        AnimationUtility.SetObjectReferenceCurve(idleClip, idleBinding, idleKeys);
        AssetDatabase.CreateAsset(idleClip, idleClipPath);

        // ── 4. Create LavaBotWalk.anim ───────────────────────────────────────
        string walkClipPath = $"{ANIM_DIR}/LavaBotWalk.anim";
        AnimationClip walkClip = new AnimationClip();
        walkClip.frameRate = FRAME_RATE;
        AnimationUtility.SetAnimationClipSettings(walkClip,
            new AnimationClipSettings { loopTime = true });

        var walkBinding = new EditorCurveBinding
        {
            type         = typeof(SpriteRenderer),
            path         = "",
            propertyName = "m_Sprite"
        };
        float frameDuration = 1f / FRAME_RATE;
        var walkKeys = new ObjectReferenceKeyframe[walkSprites.Count];
        for (int i = 0; i < walkSprites.Count; i++)
            walkKeys[i] = new ObjectReferenceKeyframe
                { time = i * frameDuration, value = walkSprites[i] };
        AnimationUtility.SetObjectReferenceCurve(walkClip, walkBinding, walkKeys);
        AssetDatabase.CreateAsset(walkClip, walkClipPath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Reload clips from asset database
        idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(idleClipPath);
        walkClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(walkClipPath);

        // ── 5. Update LavaBotAnimator.controller ─────────────────────────────
        AnimatorController ctrl =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(CTRL_PATH);
        if (ctrl == null) return "ERROR: LavaBotAnimator.controller not found";

        // Clear existing states
        var sm = ctrl.layers[0].stateMachine;
        foreach (var s in sm.states)
            sm.RemoveState(s.state);

        // Ensure parameters exist
        var paramNames = new HashSet<string>();
        foreach (var p in ctrl.parameters) paramNames.Add(p.name);
        if (!paramNames.Contains("xSpeed"))
            ctrl.AddParameter("xSpeed", AnimatorControllerParameterType.Float);
        if (!paramNames.Contains("Walking"))
            ctrl.AddParameter("Walking", AnimatorControllerParameterType.Bool);
        if (!paramNames.Contains("Grounded"))
            ctrl.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        if (!paramNames.Contains("Alive"))
            ctrl.AddParameter("Alive", AnimatorControllerParameterType.Bool);

        // Add Idle state
        var idle = sm.AddState("Idle");
        idle.motion = idleClip;
        sm.defaultState = idle;

        // Add Walk state
        var walk = sm.AddState("Walk");
        walk.motion = walkClip;

        // Idle → Walk: xSpeed > 0.1
        var toWalk = idle.AddTransition(walk);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "xSpeed");
        toWalk.hasExitTime = false;
        toWalk.duration    = 0f;

        // Walk → Idle: xSpeed < 0.1
        var toIdle = walk.AddTransition(idle);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "xSpeed");
        toIdle.hasExitTime = false;
        toIdle.duration    = 0f;

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();

        // ── 6. Fix prefab: weapon + animator ────────────────────────────────
        GameObject weaponPrefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(WEAPON_PATH);
        Weapon weaponComponent = weaponPrefabGO?.GetComponent<Weapon>();

        using (var editScope = new PrefabUtility.EditPrefabContentsScope(PREFAB_PATH))
        {
            GameObject root = editScope.prefabContentsRoot;

            // Animator
            Animator anim = root.GetComponent<Animator>();
            if (anim != null) anim.runtimeAnimatorController = ctrl;

            // Weapon — set InitialWeapon and ensure CanPickupWeapons=false
            CharacterHandleWeapon chw = root.GetComponent<CharacterHandleWeapon>();
            if (chw != null && weaponComponent != null)
            {
                chw.InitialWeapon    = weaponComponent;
                chw.CanPickupWeapons = false;
                chw.AutomaticallyBindAnimator = true;
            }

            // Ensure Character has animator bound
            Character character = root.GetComponent<Character>();
            if (character != null)
            {
                character.CharacterAnimator = root.GetComponent<Animator>();
                character.UseDefaultMecanim = true;
            }
        }

        // ── 7. Fix all scene instances ───────────────────────────────────────
        int fixedCount = 0;
        var allGOs = Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var go in allGOs)
        {
            if (!go.name.StartsWith("LavaBot")) continue;
            if (go.transform.parent?.name != "Enemies") continue;

            Animator anim = go.GetComponent<Animator>();
            if (anim != null) anim.runtimeAnimatorController = ctrl;

            CharacterHandleWeapon chw = go.GetComponent<CharacterHandleWeapon>();
            if (chw != null && weaponComponent != null)
            {
                chw.InitialWeapon    = weaponComponent;
                chw.CanPickupWeapons = false;
            }

            Character character = go.GetComponent<Character>();
            if (character != null)
                character.CharacterAnimator = go.GetComponent<Animator>();

            EditorUtility.SetDirty(go);
            fixedCount++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        return $"SUCCESS: Created Idle+Walk clips ({walkSprites.Count} walk frames). " +
               $"Updated animator with xSpeed transitions. " +
               $"Fixed weapon on {fixedCount} scene instances. Scene saved.";
    }
}
