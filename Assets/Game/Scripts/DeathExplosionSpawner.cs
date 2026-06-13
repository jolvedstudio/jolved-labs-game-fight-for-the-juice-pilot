using UnityEngine;
using MoreMountains.CorgiEngine;

/// <summary>
/// Spawns an explosion VFX prefab at this object's position when its Health reaches zero.
/// Hooks into CorgiEngine's Health.OnDeath delegate, so it works with pooled/recycled enemies.
/// </summary>
[RequireComponent(typeof(Health))]
public class DeathExplosionSpawner : MonoBehaviour
{
    [Tooltip("The explosion VFX prefab to instantiate when this character dies.")]
    public GameObject ExplosionPrefab;

    [Tooltip("Optional local offset for where the explosion spawns.")]
    public Vector3 SpawnOffset = Vector3.zero;

    protected Health _health;

    protected virtual void Awake()
    {
        _health = GetComponent<Health>();
    }

    protected virtual void OnEnable()
    {
        if (_health != null)
        {
            _health.OnDeath += OnDeath;
        }
    }

    protected virtual void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDeath -= OnDeath;
        }
    }

    protected virtual void OnDeath()
    {
        if (ExplosionPrefab == null)
        {
            return;
        }

        Vector3 spawnPos = transform.position + SpawnOffset;
        GameObject vfx = Instantiate(ExplosionPrefab, spawnPos, Quaternion.identity);

        // Auto-clean the spawned VFX so it doesn't linger.
        float life = 3f;
        var ps = vfx.GetComponentInChildren<ParticleSystem>();
        if (ps != null)
        {
            life = ps.main.duration + ps.main.startLifetime.constantMax + 0.5f;
        }
        Destroy(vfx, life);
    }
}
