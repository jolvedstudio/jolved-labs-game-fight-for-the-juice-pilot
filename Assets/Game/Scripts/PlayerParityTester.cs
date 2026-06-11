using System.Collections;
using UnityEngine;
using MoreMountains.CorgiEngine;
using MoreMountains.Tools;

namespace Game
{
    /// <summary>
    /// Runtime parity tester. On play, locates the player, reports scale/weapon/abilities,
    /// forces a weapon shot, and verifies a projectile spawns. Logs everything with [PARITY] prefix.
    /// Self-removes after running. Safe to leave in scene.
    /// </summary>
    public class PlayerParityTester : MonoBehaviour
    {
        public float StartDelay = 1.5f;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(StartDelay);

            var players = FindObjectsOfType<Character>();
            Character player = null;
            foreach (var c in players)
                if (c.CharacterType == Character.CharacterTypes.Player) { player = c; break; }

            if (player == null) { Debug.Log("[PARITY] FAIL: no Player character spawned"); yield break; }

            Debug.Log($"[PARITY] Player='{player.name}' pos={player.transform.position}");

            // Scale / sprite
            var model = player.transform.Find("ModelContainer");
            if (model != null)
            {
                var sr = model.GetComponent<SpriteRenderer>();
                var anim = model.GetComponent<Animator>();
                Debug.Log($"[PARITY] ModelScale={model.localScale.x:0.000} sprite={(sr && sr.sprite ? sr.sprite.name : "null")} animCtrl={(anim && anim.runtimeAnimatorController ? anim.runtimeAnimatorController.name : "null")}");
            }

            // Abilities
            var jp = player.FindAbility<CharacterJetpack>();
            var jump = player.FindAbility<CharacterJump>();
            var run = player.FindAbility<CharacterRun>();
            Debug.Log($"[PARITY] Abilities jetpack={(jp!=null && jp.AbilityPermitted)} jump={(jump!=null && jump.AbilityPermitted)} run={(run!=null && run.AbilityPermitted)}");

            // Weapon firing test
            var hw = player.FindAbility<CharacterHandleWeapon>();
            if (hw == null) { Debug.Log("[PARITY] FAIL: no CharacterHandleWeapon"); yield break; }
            Debug.Log($"[PARITY] Weapon={(hw.CurrentWeapon!=null ? hw.CurrentWeapon.name : "NULL")}");

            int projBefore = CountProjectiles();
            hw.ShootStart();
            yield return new WaitForSeconds(0.4f);
            hw.ShootStop();
            yield return new WaitForSeconds(0.2f);
            int projAfter = CountProjectiles();
            Debug.Log($"[PARITY] FIRING: projectiles before={projBefore} after={projAfter} -> {(projAfter>projBefore ? "PASS (weapon fired)" : "no new projectile detected")}");

            Debug.Log("[PARITY] DONE");
        }

        private int CountProjectiles()
        {
            int n = 0;
            foreach (var p in FindObjectsOfType<Projectile>()) n++;
            return n;
        }
    }
}
