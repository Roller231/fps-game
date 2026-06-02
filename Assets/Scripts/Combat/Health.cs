using UnityEngine;
using UnityEngine.Events;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath = true;

    public UnityEvent<float, float> OnHealthChanged;
    public UnityEvent OnDied;

    private float currentHealth;
    private bool isDead;

    public float Current => currentHealth;
    public float Max => maxHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (isDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            isDead = true;
            OnDied?.Invoke();
            if (destroyOnDeath) Destroy(gameObject);
        }
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // Restore to full health and clear death flag (useful for respawn)
    public void ResetToFull()
    {
        isDead = false;
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
