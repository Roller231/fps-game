using UnityEngine;

public class Weapon : MonoBehaviour
{
    [SerializeField] private WeaponData data;
    [SerializeField] private Transform muzzle;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private LayerMask bloodLayers = 0;

    private Camera fireCamera;
    private AudioSource audioSource;

    private int currentAmmo;
    private int reserveAmmo;
    private float nextFireTime;
    private bool isReloading;
    private int burstShotsLeft;

    public WeaponData Data => data;
    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public bool IsReloading => isReloading;

    public System.Action OnShot;
    public System.Action OnReloadStart;
    public System.Action OnReloadEnd;
    public System.Action OnAmmoChanged;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.3f;

        if (muzzle == null)
        {
            var t = transform.Find("Muzzle");
            if (t != null) muzzle = t;
        }

        if (data != null)
        {
            currentAmmo = data.magazineSize;
            reserveAmmo = data.reservedAmmo;
        }
    }

    public void Initialize(Camera cam, WeaponData overrideData = null)
    {
        fireCamera = cam;
        if (overrideData != null) data = overrideData;
        
        // Патроны загружаются через ProfileService
        if (data != null && currentAmmo == 0 && reserveAmmo == 0)
        {
            currentAmmo = data.magazineSize;
            reserveAmmo = data.reservedAmmo;
        }
        OnAmmoChanged?.Invoke();
    }

    // Патроны теперь сохраняются через ProfileService

    /// <summary>
    /// Пополнить боезапас до максимума
    /// </summary>
    public void RefillAmmo()
    {
        if (data == null) return;

        reserveAmmo = data.reservedAmmo;
        OnAmmoChanged?.Invoke();
        Debug.Log($"[Weapon] Refilled {data.weaponName} ammo to {reserveAmmo}");
    }

    public void HandleTriggerHeld()
    {
        if (data == null || isReloading) return;
        if (data.fireMode != FireMode.Automatic) return;
        TryShoot();
    }

    public void HandleTriggerPressed()
    {
        if (data == null || isReloading) return;

        switch (data.fireMode)
        {
            case FireMode.Single:
            case FireMode.Automatic:
                TryShoot();
                break;
            case FireMode.Burst:
                burstShotsLeft = Mathf.Max(burstShotsLeft, data.burstCount);
                TryShoot();
                break;
        }
    }

    private void Update()
    {
        if (burstShotsLeft > 0 && !isReloading && data != null && data.fireMode == FireMode.Burst)
        {
            TryShoot();
        }
    }

    public bool TryShoot()
    {
        if (data == null || isReloading) return false;
        if (Time.time < nextFireTime) return false;

        if (currentAmmo <= 0)
        {
            TryReload();
            return false;
        }

        nextFireTime = Time.time + data.SecondsBetweenShots;
        currentAmmo--;
        if (burstShotsLeft > 0) burstShotsLeft--;

        FireOnce();

        OnShot?.Invoke();
        OnAmmoChanged?.Invoke();
        return true;
    }

    private void FireOnce()
    {
        if (fireCamera == null) fireCamera = Camera.main;
        if (fireCamera == null) return;

        Vector3 origin = fireCamera.transform.position;
        Vector3 forward = fireCamera.transform.forward;

        float spreadMul = 1f;
        var holder = GetComponentInParent<WeaponHolder>();
        if (holder != null && holder.enabled && data != null)
        {
            spreadMul = holder.IsAiming ? data.adsSpreadMultiplier : 1f;
        }

        bool useProjectiles = data.projectilePrefab != null;
        for (int i = 0; i < Mathf.Max(1, data.pelletsPerShot); i++)
        {
            Vector3 dir = ApplySpread(forward, data.spreadAngle * spreadMul);
            if (useProjectiles)
            {
                Transform spawn = muzzle != null ? muzzle : transform;
                GameObject go = Instantiate(data.projectilePrefab, spawn.position, Quaternion.LookRotation(dir));
                var proj = go.GetComponent<Projectile>();
                if (proj == null) proj = go.AddComponent<Projectile>();
                float speed = 200f; // default speed if projectile doesn't set it
                Vector3 vel = (muzzle != null ? muzzle.forward : dir) * speed;
                proj.Launch(vel, data.damage, data.impactForce, data.impactPrefab, hitMask, data.enemyImpactPrefab, data.headshotMultiplier, data.hitmarkerSound);

                if (data.projectileTracerPrefab != null)
                {
                    var tracer = Instantiate(data.projectileTracerPrefab, go.transform.position, go.transform.rotation, go.transform);
                    if (tracer.TryGetComponent<Rigidbody>(out var trb)) trb.isKinematic = true;
                }
            }
            else
            {
                Vector3 start = origin;
                Vector3 end = origin + dir * data.range;
                if (Physics.Raycast(origin, dir, out RaycastHit hit, data.range, hitMask, QueryTriggerInteraction.Ignore))
                {
                    end = hit.point;
                    float dmg = data.damage;
                    if (hit.collider.CompareTag("Head")) dmg *= data.headshotMultiplier;

                    var damageable = hit.collider.GetComponentInParent<IDamageable>();
                    if (damageable != null)
                    {
                        damageable.TakeDamage(dmg, hit.point, hit.normal);
                    }

                    if (hit.rigidbody != null)
                    {
                        hit.rigidbody.AddForceAtPosition(dir * data.impactForce, hit.point, ForceMode.Impulse);
                    }

                    bool hasEnemyAI = hit.collider.GetComponentInParent<EnemyAI>() != null;
                    bool blood = ((1 << hit.collider.gameObject.layer) & bloodLayers) != 0;
                    bool playBloodFx = blood || hasEnemyAI;
                    if (playBloodFx)
                    {
                        if (data.hitmarkerSound != null && (GameSettings.Instance == null || GameSettings.Instance.HitmarkerSoundsEnabled))
                            audioSource.PlayOneShot(data.hitmarkerSound);
                        if (data.enemyImpactPrefab != null)
                        {
                            var efx = Instantiate(data.enemyImpactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                            Destroy(efx, 3f);
                        }
                    }
                    else if (!playBloodFx && data.impactPrefab != null)
                    {
                        var fx = Instantiate(data.impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                        Destroy(fx, 3f);
                    }
                }

                if (data.tracerPrefab != null)
                {
                    var tracer = Instantiate(data.tracerPrefab, muzzle != null ? muzzle.position : start, Quaternion.identity);
                    var lr = tracer.GetComponent<LineRenderer>();
                    if (lr != null)
                    {
                        lr.positionCount = 2;
                        lr.SetPosition(0, muzzle != null ? muzzle.position : start);
                        lr.SetPosition(1, end);
                    }
                    Destroy(tracer, data.tracerLifetime);
                }
            }
        }

        if (data.muzzleFlashPrefab != null && muzzle != null)
        {
var mf = Instantiate(data.muzzleFlashPrefab, muzzle.position, muzzle.rotation, muzzle);
            Destroy(mf, data.muzzleFlashLifetime);
        }

        if (data.fireSound != null) audioSource.PlayOneShot(data.fireSound);
    }

    private Vector3 ApplySpread(Vector3 forward, float angleDeg)
    {
        if (angleDeg <= 0f) return forward;
        Quaternion rot = Quaternion.LookRotation(forward);
        Vector2 rand = Random.insideUnitCircle * angleDeg;
        return rot * Quaternion.Euler(rand.y, rand.x, 0f) * Vector3.forward;
    }

    public bool TryReload()
    {
        if (data == null || isReloading) return false;
        if (currentAmmo >= data.magazineSize || reserveAmmo <= 0) return false;

        isReloading = true;
        OnReloadStart?.Invoke();
        if (data.reloadSound != null) audioSource.PlayOneShot(data.reloadSound);
        Invoke(nameof(FinishReload), data.reloadTime);
        return true;
    }

    private void FinishReload()
    {
        int needed = data.magazineSize - currentAmmo;
        int taken = Mathf.Min(needed, reserveAmmo);
        currentAmmo += taken;
        reserveAmmo -= taken;
        isReloading = false;
        OnReloadEnd?.Invoke();
        OnAmmoChanged?.Invoke();
    }

    public void AddReserveAmmo(int amount)
    {
        reserveAmmo += amount;
        OnAmmoChanged?.Invoke();
    }

    public void OverrideAmmo(int current, int reserve)
    {
        currentAmmo = Mathf.Max(0, current);
        reserveAmmo = Mathf.Max(0, reserve);
        OnAmmoChanged?.Invoke();
    }
}
