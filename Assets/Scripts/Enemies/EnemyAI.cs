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
    [SerializeField] private LayerMask obstacleMask = ~0; // for line-of-sight blocking
    [Header("Rotation")]
    [SerializeField] private float turnSpeedDegPerSec = 720f;
    [SerializeField] private float turnStepClampDeg = 120f; // max rotation per frame
    [SerializeField] private float yawOffsetDeg = 0f; // additional yaw offset when facing target
    [Header("Anim")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animSpeedParam = "Speed";
    [SerializeField] private string animShootBool = "Shoot";

    private NavMeshAgent agent;
    private Health health;
    private float nextAttackTime;
    private bool isDead;
    private bool shootState;

    public EnemyData Data => data;
    public Health Health => health;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null) agent.updateRotation = false;
        health = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
        if (data != null)
        {
            ApplyData(data);
        }
        health.OnDied.AddListener(OnDeath);
    }

    private void AdjustPathIfBlocked()
    {
        if (agent == null) return;
        if (target == null) return;
        if (agent.pathPending || !agent.hasPath) return;
        if (agent.pathStatus != NavMeshPathStatus.PathPartial) return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        if (NavMesh.Raycast(transform.position, target.position, out NavMeshHit hit, NavMesh.AllAreas))
        {
            Vector3 stopPos = hit.position - dir.normalized * Mathf.Max(agent.radius, 0.2f);
            if (NavMesh.SamplePosition(stopPos, out NavMeshHit snap, agent.radius * 1.5f, NavMesh.AllAreas))
            {
                agent.SetDestination(snap.position);
            }
        }
    }

    private void FaceTarget()
    {
        if (target == null || agent == null) return;
        Vector3 flatDir = target.position - transform.position;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude < 0.0001f) return;

        Quaternion desired = Quaternion.LookRotation(flatDir.normalized, Vector3.up);
        if (Mathf.Abs(yawOffsetDeg) > 0.01f)
        {
            desired = Quaternion.Euler(0f, yawOffsetDeg, 0f) * desired;
        }
        float step = turnSpeedDegPerSec * Time.deltaTime;
        if (turnStepClampDeg > 0f)
        {
            step = Mathf.Min(step, turnStepClampDeg);
        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desired, step);
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
        AdjustPathIfBlocked();
        FaceTarget();
        UpdateAnimSpeed();

        float dist = Vector3.Distance(transform.position, target.position);
        switch (data.archetype)
        {
            case EnemyArchetype.Melee:
            case EnemyArchetype.Heavy:
            case EnemyArchetype.Boss:
                agent.isStopped = false;
                SetShoot(false);
                HandleMelee(dist);
                break;
            case EnemyArchetype.Kamikaze:
                agent.isStopped = false;
                SetShoot(false);
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
        bool inRange = dist <= data.shootRange;
        agent.isStopped = inRange;
        if (!inRange)
        {
            SetShoot(false);
            return;
        }
        // In range: keep shoot anim on
        SetShoot(true);

        // Line of sight check: if something blocks, keep moving closer
        Vector3 toTarget = target.position - shootOrigin.position;
        float maxDist = toTarget.magnitude;
        if (Physics.Raycast(shootOrigin.position, toTarget.normalized, out RaycastHit losHit, maxDist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // If мы попали не в цель, не стреляем
            if (losHit.transform != target && losHit.collider.GetComponentInParent<EnemyAI>() == null)
            {
                SetShoot(false);
                // в радиусе, но нет прямой видимости — продолжаем идти к цели, чтобы выйти на линию
                agent.isStopped = false;
                return;
            }
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }
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
        // keep shooting state while in range
        SetShoot(true);
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

    private void UpdateAnimSpeed()
    {
        if (animator == null || string.IsNullOrEmpty(animSpeedParam)) return;
        float speedValue = 0f;
        if (agent != null)
        {
            speedValue = agent.isStopped ? 0f : agent.velocity.magnitude;
        }
        animator.SetFloat(animSpeedParam, speedValue);
    }

    private void SetShoot(bool value)
    {
        if (animator == null || string.IsNullOrEmpty(animShootBool)) return;
        if (shootState == value) return;
        shootState = value;
        animator.SetBool(animShootBool, shootState);
    }
}
