using UnityEngine;

/// <summary>
/// Ящик с припасами - пополняет HP и боезапас за деньги
/// </summary>
public class SupplyBox : MonoBehaviour
{
    [Header("Prices")]
    [SerializeField] private int healthPrice = 50;
    [SerializeField] private int ammoPrice = 100;
    [SerializeField] private int fullRestockPrice = 120; // HP + Ammo вместе (скидка)

    [Header("Settings")]
    [SerializeField] private float healAmount = 100f; // Полное восстановление HP
    
    private Health playerHealth;
    private WeaponInventory weaponInventory;

    public int HealthPrice => healthPrice;
    public int AmmoPrice => ammoPrice;
    public int FullRestockPrice => fullRestockPrice;

    private void Start()
    {
        // Найти компоненты игрока
        if (GameManager.Instance != null && GameManager.Instance.Player != null)
        {
            playerHealth = GameManager.Instance.Player.GetComponent<Health>();
            weaponInventory = WeaponInventory.Instance;
        }
    }

    /// <summary>
    /// Купить только HP
    /// </summary>
    public bool BuyHealth()
    {
        if (MoneyManager.Instance == null || !MoneyManager.Instance.CanAfford(healthPrice))
        {
            Debug.Log($"[SupplyBox] Not enough money for health. Need ${healthPrice}");
            return false;
        }

        if (playerHealth == null)
        {
            Debug.LogWarning("[SupplyBox] PlayerHealth not found!");
            return false;
        }

        if (playerHealth.Current >= playerHealth.Max)
        {
            Debug.Log("[SupplyBox] Health already full!");
            return false;
        }

        // Списываем деньги и восстанавливаем HP
        if (MoneyManager.Instance.SpendMoney(healthPrice))
        {
            playerHealth.Heal(healAmount);
            Debug.Log($"[SupplyBox] Health restored for ${healthPrice}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Купить только боезапас
    /// </summary>
    public bool BuyAmmo()
    {
        if (MoneyManager.Instance == null || !MoneyManager.Instance.CanAfford(ammoPrice))
        {
            Debug.Log($"[SupplyBox] Not enough money for ammo. Need ${ammoPrice}");
            return false;
        }

        if (weaponInventory == null)
        {
            Debug.LogWarning("[SupplyBox] WeaponInventory not found!");
            return false;
        }

        // Списываем деньги и пополняем боезапас
        if (MoneyManager.Instance.SpendMoney(ammoPrice))
        {
            RefillAllWeaponsAmmo();
            Debug.Log($"[SupplyBox] Ammo refilled for ${ammoPrice}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Купить HP + боезапас вместе (со скидкой)
    /// </summary>
    public bool BuyFullRestock()
    {
        if (MoneyManager.Instance == null || !MoneyManager.Instance.CanAfford(fullRestockPrice))
        {
            Debug.Log($"[SupplyBox] Not enough money for full restock. Need ${fullRestockPrice}");
            return false;
        }

        bool healthFull = playerHealth != null && playerHealth.Current >= playerHealth.Max;
        
        // Списываем деньги
        if (MoneyManager.Instance.SpendMoney(fullRestockPrice))
        {
            // Восстанавливаем HP
            if (playerHealth != null && !healthFull)
            {
                playerHealth.Heal(healAmount);
            }

            // Пополняем боезапас
            if (weaponInventory != null)
            {
                RefillAllWeaponsAmmo();
            }

            Debug.Log($"[SupplyBox] Full restock for ${fullRestockPrice}");
            return true;
        }

        return false;
    }

    /// <summary>
    /// Пополнить боезапас всех купленных оружий
    /// </summary>
    private void RefillAllWeaponsAmmo()
    {
        if (weaponInventory == null) return;

        var allWeapons = weaponInventory.GetAllWeapons();
        if (allWeapons == null) return;

        foreach (var weaponData in allWeapons)
        {
            if (weaponData == null) continue;
            
            // Проверяем что оружие куплено
            if (!weaponInventory.IsOwned(weaponData)) continue;

            // Пополняем боезапас до максимума
            Debug.Log($"[SupplyBox] Refilled {weaponData.weaponName} ammo to {weaponData.reservedAmmo}");
        }

        // Сохранение через ProfileService автоматически

        // Обновить текущее оружие если оно экипировано
        var holder = FindObjectOfType<WeaponHolder>();
        if (holder != null && holder.CurrentWeapon != null)
        {
            holder.CurrentWeapon.RefillAmmo();
        }
    }

    public bool CanAffordHealth()
    {
        return MoneyManager.Instance != null && MoneyManager.Instance.CanAfford(healthPrice);
    }

    public bool CanAffordAmmo()
    {
        return MoneyManager.Instance != null && MoneyManager.Instance.CanAfford(ammoPrice);
    }

    public bool CanAffordFullRestock()
    {
        return MoneyManager.Instance != null && MoneyManager.Instance.CanAfford(fullRestockPrice);
    }

    public bool IsHealthFull()
    {
        return playerHealth != null && playerHealth.Current >= playerHealth.Max;
    }
}
