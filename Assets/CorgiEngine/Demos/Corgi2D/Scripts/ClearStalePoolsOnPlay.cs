using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.CorgiEngine
{
    /// <summary>
    /// Attach this to the GameManagers object.
    /// On Awake (before anything spawns), destroys any DontDestroyOnLoad object poolers
    /// that reference the old blueRobot prefab, preventing stale pools from a previous
    /// play session from re-spawning old enemies.
    /// </summary>
    public class ClearStalePoolsOnPlay : MonoBehaviour
    {
        void Awake()
        {
            // Find all poolers in the scene including DontDestroyOnLoad
            MMSimpleObjectPooler[] poolers = FindObjectsByType<MMSimpleObjectPooler>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var pooler in poolers)
            {
                if (pooler.GameObjectToPool != null &&
                    pooler.GameObjectToPool.name.ToLower().Contains("bluerobot"))
                {
                    Debug.Log($"[ClearStalePoolsOnPlay] Destroying stale blueRobot pooler: {pooler.gameObject.name}");
                    Destroy(pooler.gameObject);
                }
            }

            // Also destroy any stale blueRobot instances in DontDestroyOnLoad
            GameObject[] allGOs = FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var go in allGOs)
            {
                if (go.name.ToLower().Contains("bluerobot") && go.scene.name == "DontDestroyOnLoad")
                {
                    Debug.Log($"[ClearStalePoolsOnPlay] Destroying stale blueRobot in DontDestroyOnLoad: {go.name}");
                    Destroy(go);
                }
            }
        }
    }
}
