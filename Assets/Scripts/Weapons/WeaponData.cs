using UnityEngine;

public enum FireMode
{
    Single,
    Automatic,
    Burst
}

[CreateAssetMenu(fileName = "WeaponData", menuName = "FPS/Weapon Data", order = 0)]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string weaponName = "New Weapon";
    public Sprite icon;
    public GameObject viewModelPrefab;

    [Header("Damage")]
    public float damage = 15f;
    public float headshotMultiplier = 2f;
    public float range = 100f;
    public float impactForce = 20f;

    [Header("Fire")]
    public FireMode fireMode = FireMode.Automatic;
    [Tooltip("Rounds per minute")]
    public float fireRate = 600f;
    public int burstCount = 3;
    [Tooltip("Pellets per shot (>1 for shotguns)")]
    public int pelletsPerShot = 1;

    [Header("Spread / Recoil")]
    [Tooltip("Spread in degrees from aim direction")]
    public float spreadAngle = 1.5f;
    public Vector2 recoilPerShot = new Vector2(1.5f, 0.6f);

    [Header("ADS (Aim Down Sights)")]
    [Tooltip("Local position of weapon in hip state")] public Vector3 hipLocalPos = new Vector3(0.25f, -0.25f, 0.5f);
    [Tooltip("Local position of weapon in ADS state (aligned with sights)")] public Vector3 adsLocalPos = new Vector3(0.0f, -0.05f, 0.25f);
    [Tooltip("FOV while hip-firing, 0 = use camera's current FOV")] public float hipFov = 0f;
    [Tooltip("FOV while aiming, 0 = don't change")] public float adsFov = 55f;
    [Tooltip("Time to blend hip<->ADS")] public float adsBlendTime = 0.12f;
    [Tooltip("Multiplier for spread while ADS")] public float adsSpreadMultiplier = 0.5f;
    [Tooltip("Multiplier for recoil while ADS")] public float adsRecoilMultiplier = 0.5f;

    [Header("Ammo")]
    public int magazineSize = 30;
    public int reservedAmmo = 120;
    public float reloadTime = 1.8f;

    [Header("FX")]
    public GameObject muzzleFlashPrefab;
    public GameObject impactPrefab;
    [Tooltip("Optional impact FX when hitting enemies (overrides impactPrefab for enemies)")]
    public GameObject enemyImpactPrefab;
    [Tooltip("If set, weapon fires projectiles instead of hitscan")] public GameObject projectilePrefab;
    [Tooltip("Optional tracer prefab for hitscan (LineRenderer or simple VFX)")] public GameObject tracerPrefab;
    [Tooltip("Optional trail/tracer to attach to physical projectiles")] public GameObject projectileTracerPrefab;
    [Tooltip("Lifetime of muzzle flash instance (seconds)")] public float muzzleFlashLifetime = 0.5f;
    [Tooltip("Lifetime of tracer instance (seconds)")] public float tracerLifetime = 0.2f;
    public AudioClip fireSound;
    public AudioClip reloadSound;

    public float SecondsBetweenShots => fireRate > 0f ? 60f / fireRate : 0f;
}
