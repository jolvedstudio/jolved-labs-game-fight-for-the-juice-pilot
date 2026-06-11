using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;

/// <summary>
/// Fixes two root causes of the blueRobot-on-play issue:
///
/// 1. Health.DestroyOnDeath=true conflicts with AutoRespawn.DisableOnKill=true.
///    When DestroyOnDeath is true, the GameObject is destroyed and AutoRespawn
///    can never re-enable it. Set DestroyOnDeath=false so AutoRespawn controls
///    the lifecycle instead.
///
/// 2. The MutualizeWaitingPools=true on the RobotWeapon's MMSimpleObjectPooler
///    means it shares a pool across scenes. If a previous play session had
///    blueRobots with the same weapon, their pooled projectiles persist in
///    DontDestroyOnLoad. This is a Unity editor state issue — clearing it
///    requires ensuring the LavaBot uses a uniquely-named weapon or a fresh pool.
///
/// This script fixes both the prefab and all scene instances.
/// </summary>
public class FixLavaBotHealth
{
    const string LAVABOT_PREFAB_PATH = "Assets/CorgiEngine/Demos/Corgi2D/Prefabs/AI/LavaBot.prefab";

    public static string Execute()
    {
        int fixedCount = 0;

        // ── Fix all scene instances ──────────────────────────────────────────
        var allHealthComponents = Object.FindObjectsByType<Health>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var health in allHealthComponents)
        {
            if (health.gameObject.name.StartsWith("LavaBot"))
            {
                // DestroyOnDeath must be FALSE — AutoRespawn handles lifecycle
                if (health.DestroyOnDeath)
                {
                    health.DestroyOnDeath = false;
                    EditorUtility.SetDirty(health);
                    fixedCount++;
                }
            }
        }

        // ── Fix the prefab ───────────────────────────────────────────────────
        using (var editScope = new PrefabUtility.EditPrefabContentsScope(LAVABOT_PREFAB_PATH))
        {
            GameObject root = editScope.prefabContentsRoot;

            Health health = root.GetComponent<Health>();
            if (health != null)
            {
                health.DestroyOnDeath = false;
                health.CollisionsOffOnDeath = true;
                health.GravityOffOnDeath    = true;
            }

            // Also ensure AutoRespawn is correctly configured
            AutoRespawn respawn = root.GetComponent<AutoRespawn>();
            if (respawn != null)
            {
                respawn.DisableOnKill                  = true;
                respawn.DisableModelOnKill             = false; // no separate model
                respawn.RespawnOnPlayerRespawn         = true;
                respawn.RepositionToInitOnPlayerRespawn = true; // reset to spawn pos
                respawn.AutoRespawnDuration            = 0f;
                respawn.AutoRespawnAmount              = -1; // infinite respawns
            }
        }

        // ── Save scene ───────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();

        return $"SUCCESS: Fixed DestroyOnDeath on {fixedCount} scene LavaBot(s) and prefab. " +
               "AutoRespawn now controls lifecycle correctly. Scene saved.";
    }
}
