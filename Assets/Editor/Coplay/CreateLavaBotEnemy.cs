using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;

/// <summary>
/// Creates the LavaBot enemy prefab and replaces all blueRobots in the Lava scene.
///
/// AI Brain - 4 States:
///   1. Patrol  : walks back and forth, avoids holes/walls
///   2. Alert   : player detected in radius 8 → waits 0.3s → Chase
///   3. Chase   : moves toward player + shoots; health below 9 → Retreat; target lost → Patrol
///   4. Retreat : moves away from player + shoots; after 3s → Chase
/// </summary>
public class CreateLavaBotEnemy
{
    const string PREFAB_SAVE_PATH = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/LavaBot.prefab";
    const string SPRITE_PATH      = "Assets/CorgiEngine/Demos/Corgi2D/Sprites/Enemies/lavaBot.png";
    const string WEAPON_PATH      = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/Weapons/RobotWeapon.prefab";
    const string ROBOT_ANIM_PATH  = "Assets/CorgiEngine/Demos/Corgi2D/Animations/AI/robot_0.controller";

    public static string Execute()
    {
        // ── 1. Load assets ──────────────────────────────────────────────────
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SPRITE_PATH);
        if (sprite == null) return "ERROR: lavaBot.png not found at " + SPRITE_PATH;

        RuntimeAnimatorController animCtrl =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ROBOT_ANIM_PATH);

        GameObject weaponPrefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(WEAPON_PATH);

        // ── 2. Build root GameObject ────────────────────────────────────────
        GameObject root = new GameObject("LavaBot");
        root.layer = LayerMask.NameToLayer("Enemies");

        // SpriteRenderer
        SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color  = Color.white;

        // Animator
        if (animCtrl != null)
        {
            Animator anim = root.AddComponent<Animator>();
            anim.runtimeAnimatorController = animCtrl;
        }

        // BoxCollider2D
        BoxCollider2D col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size      = new Vector2(1.92f, 1.57f);
        col.offset    = new Vector2(0f, -0.16f);

        // Rigidbody2D
        Rigidbody2D rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType     = RigidbodyType2D.Kinematic;
        rb.sleepMode    = RigidbodySleepMode2D.NeverSleep;
        rb.gravityScale = 1f;

        // CorgiController
        CorgiController cc = root.AddComponent<CorgiController>();
        cc.NumberOfHorizontalRays          = 5;
        cc.NumberOfVerticalRays            = 6;
        cc.AutomaticallySetPhysicsSettings = true;

        // ── 3. Corgi Engine components ──────────────────────────────────────

        // DamageOnTouch
        DamageOnTouch dot = root.AddComponent<DamageOnTouch>();
        dot.TargetLayerMask            = LayerManager.PlayerLayerMask;
        dot.MinDamageCaused            = 15f;
        dot.MaxDamageCaused            = 15f;
        dot.DamageCausedKnockbackType  = DamageOnTouch.KnockbackStyles.SetForce;
        dot.DamageCausedKnockbackForce = new Vector2(30f, 4f);
        dot.InvincibilityDuration      = 0.5f;

        // Health — 30 HP, worth 25 pts
        Health health = root.AddComponent<Health>();
        health.InitialHealth       = 30f;
        health.MaximumHealth       = 30f;
        health.PointsWhenDestroyed = 25;
        health.DestroyOnDeath      = true;
        health.ApplyDeathForce     = true;
        health.DeathForce          = new Vector2(0f, 12f);
        health.FlickerSpriteOnHit  = true;
        health.FlickerColor        = new Color(1f, 0.2f, 0f, 1f);

        // AutoRespawn
        AutoRespawn respawn = root.AddComponent<AutoRespawn>();
        respawn.RespawnOnPlayerRespawn        = true;
        respawn.DisableOnKill                 = true;
        respawn.DisableModelOnKill            = true;
        respawn.AutoRespawnAmount             = 3;
        respawn.IgnoreCheckpointsAlwaysRespawn = true;

        // Character
        Character character = root.AddComponent<Character>();
        character.CharacterType              = Character.CharacterTypes.AI;
        character.InitialFacingDirection     = Character.FacingDirections.Left;
        character.FlipModelOnDirectionChange = true;
        character.ModelFlipValue             = new Vector3(-1f, 1f, 1f);
        character.SendStateChangeEvents      = false;
        character.SendStateUpdateEvents      = false;
        character.UseDefaultMecanim          = true;

        // CharacterHorizontalMovement
        CharacterHorizontalMovement chm = root.AddComponent<CharacterHorizontalMovement>();
        chm.WalkSpeed = 5f;

        // CharacterJump — can jump over small gaps
        CharacterJump cj = root.AddComponent<CharacterJump>();
        cj.NumberOfJumps    = 1;
        cj.JumpHeight       = 3f;
        cj.JumpRestrictions = CharacterJump.JumpBehavior.CanJumpOnGround;

        // CharacterHandleWeapon
        CharacterHandleWeapon chw = root.AddComponent<CharacterHandleWeapon>();
        if (weaponPrefabGO != null)
            chw.InitialWeapon = weaponPrefabGO.GetComponent<Weapon>();

        // ── 4. Feedback child objects ────────────────────────────────────────
        GameObject damageFB = new GameObject("DamageFeedbacks");
        damageFB.transform.SetParent(root.transform, false);
        MMFeedbacks damageFeedbacks = damageFB.AddComponent<MMFeedbacks>();

        GameObject deathFB = new GameObject("DeathFeedbacks");
        deathFB.transform.SetParent(root.transform, false);
        MMFeedbacks deathFeedbacks = deathFB.AddComponent<MMFeedbacks>();

        health.DamageFeedbacks = damageFeedbacks;
        health.DeathFeedbacks  = deathFeedbacks;

        // ── 5. Advanced AI Brain ─────────────────────────────────────────────
        AIBrain brain = root.AddComponent<AIBrain>();

        // --- Decisions (added as components on root) -------------------------

        // Detect player within radius 8
        AIDecisionDetectTargetRadius detectRadius = root.AddComponent<AIDecisionDetectTargetRadius>();
        detectRadius.Radius      = 8f;
        detectRadius.TargetLayer = LayerManager.PlayerLayerMask;

        // Alert timer: 0.3s before engaging
        AIDecisionTimeInState alertTimer = root.AddComponent<AIDecisionTimeInState>();
        alertTimer.AfterTimeMin = 0.3f;
        alertTimer.AfterTimeMax = 0.3f;

        // Target is null check (x2 — one per state that needs it)
        AIDecisionTargetIsNull targetNullAlert = root.AddComponent<AIDecisionTargetIsNull>();
        AIDecisionTargetIsNull targetNullChase = root.AddComponent<AIDecisionTargetIsNull>();

        // Low health: <= 9 (30% of 30)
        AIDecisionHealth lowHealth = root.AddComponent<AIDecisionHealth>();
        lowHealth.TrueIfHealthIs = AIDecisionHealth.ComparisonModes.LowerThan;
        lowHealth.HealthValue    = 9;
        lowHealth.OnlyOnce       = true;

        // Retreat timer: 3s then back to Chase
        AIDecisionTimeInState retreatTimer = root.AddComponent<AIDecisionTimeInState>();
        retreatTimer.AfterTimeMin = 3f;
        retreatTimer.AfterTimeMax = 3f;

        // --- Actions (added as components on root) ---------------------------

        AIActionPatrol patrolAction = root.AddComponent<AIActionPatrol>();
        patrolAction.ChangeDirectionOnWall      = true;
        patrolAction.AvoidFalling               = true;
        patrolAction.HoleDetectionRaycastLength = 1f;

        AIActionDoNothing doNothing = root.AddComponent<AIActionDoNothing>();

        AIActionMoveTowardsTarget chaseMove = root.AddComponent<AIActionMoveTowardsTarget>();
        chaseMove.MinimumDistance = 3f;

        AIActionShoot chaseShoot = root.AddComponent<AIActionShoot>();
        chaseShoot.FaceTarget    = true;
        chaseShoot.AimAtTarget   = false;

        AIActionMoveAwayFromTarget retreatMove = root.AddComponent<AIActionMoveAwayFromTarget>();
        retreatMove.MinimumDistance = 6f;

        AIActionShoot retreatShoot = root.AddComponent<AIActionShoot>();
        retreatShoot.FaceTarget  = false;
        retreatShoot.AimAtTarget = false;

        // --- Build States ----------------------------------------------------

        // STATE 1: Patrol
        AIState patrolState = new AIState();
        patrolState.StateName  = "Patrol";
        patrolState.Actions    = new AIActionsList();
        patrolState.Actions.Add(patrolAction);
        patrolState.Transitions = new AITransitionsList();
        patrolState.Transitions.Add(new AITransition { Decision = detectRadius, TrueState = "Alert" });

        // STATE 2: Alert
        AIState alertState = new AIState();
        alertState.StateName  = "Alert";
        alertState.Actions    = new AIActionsList();
        alertState.Actions.Add(doNothing);
        alertState.Transitions = new AITransitionsList();
        alertState.Transitions.Add(new AITransition { Decision = targetNullAlert, TrueState = "Patrol" });
        alertState.Transitions.Add(new AITransition { Decision = alertTimer,      TrueState = "Chase"  });

        // STATE 3: Chase
        AIState chaseState = new AIState();
        chaseState.StateName  = "Chase";
        chaseState.Actions    = new AIActionsList();
        chaseState.Actions.Add(chaseMove);
        chaseState.Actions.Add(chaseShoot);
        chaseState.Transitions = new AITransitionsList();
        chaseState.Transitions.Add(new AITransition { Decision = lowHealth,       TrueState = "Retreat" });
        chaseState.Transitions.Add(new AITransition { Decision = targetNullChase, TrueState = "Patrol"  });

        // STATE 4: Retreat
        AIState retreatState = new AIState();
        retreatState.StateName  = "Retreat";
        retreatState.Actions    = new AIActionsList();
        retreatState.Actions.Add(retreatMove);
        retreatState.Actions.Add(retreatShoot);
        retreatState.Transitions = new AITransitionsList();
        retreatState.Transitions.Add(new AITransition { Decision = retreatTimer, TrueState = "Chase" });

        // Assign states to brain
        brain.States      = new List<AIState> { patrolState, alertState, chaseState, retreatState };
        brain.BrainActive = true;

        character.CharacterBrain = brain;

        // MMPoolableObject
        MMPoolableObject pool = root.AddComponent<MMPoolableObject>();
        pool.BoundsBasedOn = MMObjectBounds.WaysToDetermineBounds.Collider2D;

        // ── 6. Save prefab ───────────────────────────────────────────────────
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_SAVE_PATH);
        Object.DestroyImmediate(root);

        if (prefab == null)
            return "ERROR: Failed to save prefab to " + PREFAB_SAVE_PATH;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return "SUCCESS: LavaBot prefab created at " + PREFAB_SAVE_PATH;
    }
}
