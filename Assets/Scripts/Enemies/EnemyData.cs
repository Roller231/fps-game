using UnityEngine;

public enum EnemyArchetype
{
    Melee,
    Shooter,
    Kamikaze,
    Heavy,
    Boss
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "FPS/Enemy Data", order = 1)]
public class EnemyData : ScriptableObject
{
    [Header("Identity")]
    public string enemyName = "Soldier";
    public EnemyArchetype archetype = EnemyArchetype.Melee;
    public GameObject prefab;
    public Color tintColor = Color.white;

    [Header("Stats")]
    public float baseHealth = 100f;
    public float moveSpeed = 3.5f;
    public float damage = 10f;
    public float attackRange = 2f;
    public float attackRate = 1f;

    [Header("Shooter")]
    public float shootRange = 25f;
    public float shootSpread = 2.5f;
    public LayerMask shootMask = ~0;

    [Header("Kamikaze")]
    public float explosionRadius = 4f;
    public float explosionDamage = 100f;

    [Header("Scaling")]
    [Tooltip("Health multiplier per wave step")] public float healthPerWave = 1.1f;
    [Tooltip("Damage multiplier per wave step")] public float damagePerWave = 1.05f;
}
