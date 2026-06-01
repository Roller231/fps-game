using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private EnemyData data;
    [SerializeField] private Transform target;
    [SerializeField] private Transform shootOrigin;
    [SerializeField] private GameObject bulletPrefab; // optional for shooters
    [SerializeField] private float bulletSpeed = 45f;

    private NavMeshAgent agent;
    private Health health;
    private float nextAttackTime;
    private bool isDead;

    public EnemyData Data => data;
    public Health Health => health;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<Health>();
        if (data != null)
        {
            ApplyData(data);
        }
        health.OnDied.AddListener(OnDeath);
    }

    public void ApplyData(EnemyData d)
    {
        data = d;
        if (data == null) return;
        if (agent != null)
        {
            agent.speed = data.moveSpeed;
        }
        if (data.prefab != null)
        {
            var renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = data.tintColor;
            }
        }
        if (health != null)
        {
            health.Heal(999999); // reset to max, then set max
            var f = typeof(Health).GetField("maxHealth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f != null) f.SetValue(health, data.baseHealth);
            health.Heal(data.baseHealth);
        }
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    private void Update()
    {
        if (isDead || data == null) return;
        if (target == null)
        {
            // idle or look for player — left simple here
            return;
        }

        agent.SetDestination(target.position);

        float dist = Vector3.Distance(transform.position, target.position);
        switch (data.archetype)
        {
            case EnemyArchetype.Melee:
            case EnemyArchetype.Heavy:
            case EnemyArchetype.Boss:
                HandleMelee(dist);
                break;
            case EnemyArchetype.Kamikaze:
                HandleKamikaze(dist);
                break;
            case EnemyArchetype.Shooter:
                HandleShooter(dist);
                break;
        }
    }

    private void HandleMelee(float dist)
    {
        if (Time.time < nextAttackTime) return;
        if (dist <= data.attackRange)
        {
            nextAttackTime = Time.time + 1f / Mathf.Max(0.01f, data.attackRate);
            DealDamage(target, data.damage);
        }
    }

    private void HandleKamikaze(float dist)
    {
        if (dist <= data.attackRange)
        {
            Explode();
        }
    }

    private void HandleShooter(float dist)
    {
        if (dist > data.shootRange) return;
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + 1f / Mathf.Max(0.01f, data.attackRate);

        Vector3 dir = (target.position - shootOrigin.position).normalized;
        dir = Quaternion.Euler(Random.insideUnitSphere * data.shootSpread) * dir;

        // Use hitscan if no bullet prefab
        if (bulletPrefab == null)
        {
            if (Physics.Raycast(shootOrigin.position, dir, out RaycastHit hit, data.shootRange, data.shootMask, QueryTriggerInteraction.Ignore))
            {
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                dmg?.TakeDamage(data.damage, hit.point, hit.normal);
            }
            return;
        }

        var go = Instantiate(bulletPrefab, shootOrigin.position, Quaternion.LookRotation(dir));
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = dir * bulletSpeed;
        }
    }

    private void DealDamage(Transform t, float amount)
    {
        if (t == null) return;
        var dmg = t.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(amount, t.position, Vector3.up);
        }
    }

    private void Explode()
    {
        if (isDead) return;
        // Simple sphere damage
        Collider[] hits = Physics.OverlapSphere(transform.position, data.explosionRadius, data.shootMask, QueryTriggerInteraction.Ignore);
        foreach (var h in hits)
        {
            var dmg = h.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(data.explosionDamage, transform.position, Vector3.up);
            }
        }
        OnDeath();
    }

    private void OnDeath()
    {
        if (isDead) return;
        isDead = true;
        Destroy(gameObject, 0.1f);
    }
}
