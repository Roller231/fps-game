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
    [Header("Targeting")]
    [SerializeField] private float playerDetectRange = 40f;
    [SerializeField] private LayerMask playerMask = ~0;
    [Header("Rotation")]
    [SerializeField] private float turnSpeedDegPerSec = 720f;
    [SerializeField] private float turnStepClampDeg = 120f; // max rotation per frame
    [SerializeField] private float yawOffsetDeg = 0f; // additional yaw offset when facing target
    [Header("Pathing")]
    [Tooltip("Минимальный интервал между вызовами SetDestination (снижает дёрганье/частое репатхинг).")]
    [SerializeField] private float repathInterval = 0.4f;
    [Tooltip("Порог смещения цели, после которого обновляем путь немедленно (м).")]
    [SerializeField] private float repathPositionThreshold = 0.75f;
    [Tooltip("Время 'прилипании' к игроку после потери прямой видимости (сек).")]
    [SerializeField] private float playerStickyTime = 1.5f;
    [Tooltip("Кулдаун на корректировку частичного пути (NavMeshPathStatus.Partial), сек.")]
    [SerializeField] private float partialRepathCooldown = 1.0f;
    [Header("Target Type")]
    [Tooltip("Тег динамических целей (обычно Player). Если пусто, тег не используется.")]
    [SerializeField] private string dynamicTargetTag = "Player";
    [Tooltip("Слои динамических целей. Если 0 — не используется.")]
    [SerializeField] private LayerMask dynamicTargetMask = 0;
    [Header("Approach to Static Targets")]
    [Tooltip("Искать у цели дочерний Transform с именем 'NavPoint' и целиться в него (рекомендуется для сооружений).")]
    [SerializeField] private bool preferChildNavPoint = true;
    [Tooltip("Радиус выборки точки подхода на NavMesh для статичных целей, если NavPoint не найден.")]
    [SerializeField] private float staticApproachSampleRadius = 2f;
    [Tooltip("Смещать точку подхода внутрь навмеша от ближайшей границы (по нормали края). Помогает избежать подпрыгиваний на границе.")]
    [SerializeField] private bool staticApproachEdgeSnap = true;
    [Tooltip("Дополнительный отступ внутрь навмеша (м). К радиусу агента прибавляется это значение.")]
    [SerializeField] private float staticApproachInward = 0.3f;
    [Header("Anim")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animSpeedParam = "Speed";
    [SerializeField] private string animShootBool = "Shoot";

    private NavMeshAgent agent;
    private Health health;
    private float nextAttackTime;
    private bool isDead;
    private bool shootState;
    private float healthMul = 1f;
    private float damageMul = 1f;
    private float nextRepathTime;
    private Vector3 lastSetDestination;
    private float lastSeenPlayerTime;
    private Transform lastTarget;
    private bool destinationInitialized;
    private float nextPartialAdjustTime;
    private Vector3 lastPartialAdjust;
    private bool inRangeState;
    [SerializeField] private float shootRangeHysteresis = 1.0f;
    private bool isStaticTarget;
    private bool hasStaticApproach;
    private Vector3 staticApproachPos;
    private Health currentBase;
    private Transform desiredTarget;

    private float Damage => (data != null ? data.damage : 0f) * damageMul;

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

    private void SelectTarget()
    {
        // Priority: player in LOS, else closest alive base part from GameManager
        Transform player = GameManager.Instance != null ? GameManager.Instance.Player : null;
        bool seesPlayerNow = player != null && IsPlayerVisible(player);
        if (seesPlayerNow) lastSeenPlayerTime = Time.time;
        bool sticky = player != null && (Time.time - lastSeenPlayerTime) <= playerStickyTime;

        Transform want;
        if (seesPlayerNow || sticky)
        {
            currentBase = null; // переключились на игрока
            want = player;
        }
        else
        {
            // Не перевыбираем ближайшую базу каждый кадр — это и вызывало постоянный
            // перерасчёт пути (на стене из множества блоков "ближайший" постоянно меняется).
            // Держим выбранную часть базы, пока она не уничтожена.
            if (currentBase == null || currentBase.IsDead)
            {
                currentBase = GameManager.Instance != null
                    ? GameManager.Instance.GetClosestAliveBase(transform.position)
                    : null;
            }
            want = currentBase != null ? currentBase.transform : desiredTarget;
        }

        // Меняем цель (и сбрасываем путь) только когда логическая цель реально сменилась.
        if (want != desiredTarget)
        {
            desiredTarget = want;
            SetTarget(want);
        }
    }

    private bool IsPlayerVisible(Transform player)
    {
        Vector3 eye = shootOrigin != null ? shootOrigin.position : transform.position + Vector3.up * 1.6f;
        Vector3 aim = GetAimPoint(player);
        Vector3 toPlayer = aim - eye;
        float sqrDist = toPlayer.sqrMagnitude;
        if (sqrDist > playerDetectRange * playerDetectRange) return false;
        int mask = obstacleMask | playerMask; // consider both obstacles and the player
        if (Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, Mathf.Sqrt(sqrDist), mask, QueryTriggerInteraction.Ignore))
        {
            // visible only if the first hit is the player (or its children)
            if (hit.transform == player || hit.transform.IsChildOf(player)) return true;
            return false;
        }
        return false;
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

        if (Time.time < nextPartialAdjustTime) return;
        if (NavMesh.Raycast(transform.position, target.position, out NavMeshHit hit, NavMesh.AllAreas))
        {
            Vector3 stopPos = hit.position - dir.normalized * Mathf.Max(agent.radius, 0.2f);
            if (NavMesh.SamplePosition(stopPos, out NavMeshHit snap, agent.radius * 1.5f, NavMesh.AllAreas))
            {
                // Не спамим одинаковые точки назначения
                if ((lastPartialAdjust - snap.position).sqrMagnitude > 0.01f)
                {
                    agent.SetDestination(snap.position);
                    lastPartialAdjust = snap.position;
                }
                nextPartialAdjustTime = Time.time + partialRepathCooldown;
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
        ApplyData(d, 1);
    }

    /// <summary>
    /// Применяет данные врага с учётом номера волны.
    /// Здоровье и урон масштабируются множителями healthPerWave / damagePerWave.
    /// </summary>
    public void ApplyData(EnemyData d, int wave)
    {
        data = d;
        if (data == null) return;

        int steps = Mathf.Max(0, wave - 1);
        healthMul = Mathf.Pow(Mathf.Max(0.0001f, data.healthPerWave), steps);
        damageMul = Mathf.Pow(Mathf.Max(0.0001f, data.damagePerWave), steps);

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
            health.SetMaxHealth(data.baseHealth * healthMul, true);
        }
    }

    public void SetTarget(Transform t)
    {
        if (target != t)
        {
            target = t;
            lastTarget = t;
            destinationInitialized = false; // сбросить, чтобы для новой цели установить путь

            // Определим тип цели и подготовим точку подхода для статичных
            isStaticTarget = IsDynamicTarget(t) == false;
            hasStaticApproach = false;
            if (isStaticTarget && t != null)
            {
                // 1) Если есть дочерний NavPoint — целимся в него
                if (preferChildNavPoint)
                {
                    Transform navPoint = FindChildByName(t, "NavPoint");
                    if (navPoint != null)
                    {
                        target = navPoint; // переопределяем цель на навпоинт
                        lastTarget = target;
                        // Для NavPoint статическая точка подхода не нужна
                        hasStaticApproach = false;
                        return;
                    }
                }
                // 2) Иначе один раз сэмплируем ближайшую точку на NavMesh
                if (NavMesh.SamplePosition(t.position, out NavMeshHit hit, staticApproachSampleRadius, NavMesh.AllAreas))
                {
                    staticApproachPos = hit.position;
                    if (staticApproachEdgeSnap)
                    {
                        if (NavMesh.FindClosestEdge(staticApproachPos, out NavMeshHit edge, NavMesh.AllAreas))
                        {
                            float inward = (agent != null ? agent.radius : 0.3f) + Mathf.Max(0f, staticApproachInward);
                            Vector3 candidate = edge.position - edge.normal * inward;
                            if (NavMesh.SamplePosition(candidate, out NavMeshHit snap2, inward, NavMesh.AllAreas))
                            {
                                staticApproachPos = snap2.position;
                            }
                            else
                            {
                                staticApproachPos = candidate; // fallback, обычно всё равно на навмеш попадает
                            }
                        }
                    }
                    hasStaticApproach = true;
                }
            }
        }
    }

    private void Update()
    {
        if (isDead || data == null) return;
        SelectTarget();
        if (target == null) return;

        // Throttled SetDestination: часто репатчим только для динамических целей (игрок)
        Vector3 tpos = hasStaticApproach ? staticApproachPos : target.position;
        bool isDynamic = IsDynamicTarget(target);
        bool mustUpdate = false;
        if (isDynamic)
        {
            mustUpdate = !agent.hasPath
                         || Time.time >= nextRepathTime
                         || (lastSetDestination - tpos).sqrMagnitude >= (repathPositionThreshold * repathPositionThreshold);
        }
        else
        {
            // Статические цели (база/сооружения): путь задаём ОДИН раз.
            // Намеренно НЕ репатчим на PathPartial/PathInvalid: для целей с NavMeshObstacle
            // (Carve) путь почти всегда частичный (упирается в вырез), и повторный
            // SetDestination каждый кадр вызывал баг с дёрганьем. Агент сам дойдёт до края
            // выреза у объекта и остановится.
            mustUpdate = !destinationInitialized || !agent.hasPath;
        }
        if (mustUpdate)
        {
            agent.SetDestination(tpos);
            lastSetDestination = tpos;
            nextRepathTime = Time.time + repathInterval;
            destinationInitialized = true;
        }
        // Корректировка частичного пути — только для динамических целей (игрок).
        // Для статичных частичный путь это норма (упёрлись в obstacle) и репатч не нужен.
        if (isDynamic) AdjustPathIfBlocked();
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
        if (dist <= data.attackRange || HasArrived())
        {
            nextAttackTime = Time.time + 1f / Mathf.Max(0.01f, data.attackRate);
            DealDamage(target, Damage);
        }
    }

    private void HandleKamikaze(float dist)
    {
        if (dist <= data.attackRange || HasArrived())
        {
            Explode();
        }
    }

    // Агент дошёл до конца проложенного пути (например, упёрся в obstacle цели).
    // Позволяет атаковать сооружение с NavMeshObstacle, даже если центр объекта дальше attackRange.
    private bool HasArrived()
    {
        if (agent == null || agent.pathPending || !agent.hasPath) return false;
        if (agent.remainingDistance > agent.stoppingDistance + 0.15f) return false;
        return agent.velocity.sqrMagnitude < 0.04f;
    }

    private void HandleShooter(float dist)
    {
        if (shootOrigin == null) shootOrigin = transform;
        // Гистерезис вокруг дистанции стрельбы, чтобы не дёргаться на границе
        float enter = Mathf.Max(0.1f, data.shootRange - shootRangeHysteresis);
        float exit = data.shootRange + shootRangeHysteresis;
        if (!inRangeState && dist <= enter) inRangeState = true;
        else if (inRangeState && dist >= exit) inRangeState = false;

        agent.isStopped = inRangeState;
        if (!inRangeState)
        {
            SetShoot(false);
            return;
        }
        // In range: keep shoot anim on
        SetShoot(true);

        // Line of sight check: if something blocks, keep moving closer
        Vector3 aimPoint = GetAimPoint(target);
        Vector3 toTarget = aimPoint - shootOrigin.position;
        float maxDist = toTarget.magnitude;
        if (Physics.Raycast(shootOrigin.position, toTarget.normalized, out RaycastHit losHit, maxDist, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            // If мы попали не в цель (и не в её дочерние коллайдеры), не стреляем
            bool hitIsTarget = losHit.transform == target || losHit.transform.IsChildOf(target);
            if (!hitIsTarget && losHit.collider.GetComponentInParent<EnemyAI>() == null)
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

        Vector3 dir = GetSpreadDirection(shootOrigin.position, aimPoint, data.shootSpread);

        // Use hitscan if no bullet prefab
        if (bulletPrefab == null)
        {
            if (Physics.Raycast(shootOrigin.position, dir, out RaycastHit hit, data.shootRange, data.shootMask, QueryTriggerInteraction.Ignore))
            {
                var dmg = hit.collider.GetComponentInParent<IDamageable>();
                dmg?.TakeDamage(Damage, hit.point, hit.normal);
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

    private Vector3 GetAimPoint(Transform t)
    {
        if (t == null) return transform.position + Vector3.up * 1.0f;
        var col = t.GetComponentInChildren<Collider>();
        if (col != null) return col.bounds.center;
        return t.position + Vector3.up * 1.0f;
    }

    // Returns a direction within a cone of spreadDeg around the ideal direction to the aim point
    private Vector3 GetSpreadDirection(Vector3 from, Vector3 to, float spreadDeg)
    {
        Vector3 dir = (to - from).normalized;
        if (spreadDeg <= 0.01f) return dir;

        // Build an orthonormal basis around dir
        Vector3 basisUp = Mathf.Abs(Vector3.Dot(dir, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;
        Vector3 right = Vector3.Cross(basisUp, dir).normalized;
        Vector3 up = Vector3.Cross(dir, right).normalized;

        // Random offset in the perpendicular plane; small-angle approx using tan(theta)
        float rad = spreadDeg * Mathf.Deg2Rad;
        Vector2 off = Random.insideUnitCircle * Mathf.Tan(rad);
        Vector3 deviated = (dir + right * off.x + up * off.y).normalized;
        return deviated;
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
                dmg.TakeDamage(data.explosionDamage * damageMul, transform.position, Vector3.up);
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

    private bool IsDynamicTarget(Transform t)
    {
        if (t == null) return false;
        if (GameManager.Instance != null && t == GameManager.Instance.Player) return true;
        if (!string.IsNullOrEmpty(dynamicTargetTag) && t.CompareTag(dynamicTargetTag)) return true;
        if (dynamicTargetMask != 0 && ((1 << t.gameObject.layer) & dynamicTargetMask) != 0) return true;
        return false;
    }

    private Transform FindChildByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrEmpty(name)) return null;
        string n = name.ToLowerInvariant();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name.ToLowerInvariant() == n) return t;
        }
        return null;
    }
}
