using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Управляет инвентарём оружия игрока: покупка, экипировка в слоты, сохранение
/// </summary>
public class WeaponInventory : MonoBehaviour
{
    public static WeaponInventory Instance { get; private set; }

    [Header("Weapon Slots")]
    [SerializeField] private int maxSlots = 2;
    
    [Header("Available Weapons")]
    [SerializeField] private WeaponData[] allWeapons;

    private HashSet<string> ownedWeapons = new HashSet<string>();
    private WeaponData[] equippedWeapons;
    private int currentSlot = 0;

    public int MaxSlots => maxSlots;
    public int CurrentSlot => currentSlot;
    public WeaponData[] EquippedWeapons => equippedWeapons;
    public WeaponData CurrentWeapon => equippedWeapons[currentSlot];

    public System.Action<int> OnSlotChanged;
    public System.Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        equippedWeapons = new WeaponData[maxSlots];
        LoadInventory();
    }

    private void Update()
    {
        // ВРЕМЕННО: Очистка сохранений по F9
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.LogWarning("Clearing all PlayerPrefs!");
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void LoadInventory()
    {
        // Загрузить купленное оружие
        string ownedData = PlayerPrefs.GetString("OwnedWeapons", "");
        if (!string.IsNullOrEmpty(ownedData))
        {
            string[] owned = ownedData.Split(',');
            foreach (var name in owned)
            {
                if (!string.IsNullOrEmpty(name)) ownedWeapons.Add(name);
            }
        }

        // Дать стартовый пистолет бесплатно при первом запуске
        if (ownedWeapons.Count == 0 && allWeapons != null && allWeapons.Length > 0)
        {
            var pistol = GetWeaponByName("Pistol");
            if (pistol != null)
            {
                ownedWeapons.Add(pistol.weaponName);
                equippedWeapons[0] = pistol; // Экипировать в первый слот
                SaveInventory();
            }
        }

        // Загрузить экипированное оружие
        for (int i = 0; i < maxSlots; i++)
        {
            string weaponName = PlayerPrefs.GetString($"EquippedSlot{i}", "");
            if (!string.IsNullOrEmpty(weaponName))
            {
                var weapon = GetWeaponByName(weaponName);
                if (weapon != null && IsOwned(weapon))
                {
                    equippedWeapons[i] = weapon;
                }
            }
        }

        currentSlot = PlayerPrefs.GetInt("CurrentSlot", 0);
        if (currentSlot >= maxSlots) currentSlot = 0;
    }

    private void SaveInventory()
    {
        // Сохранить купленное оружие
        string ownedData = string.Join(",", ownedWeapons);
        PlayerPrefs.SetString("OwnedWeapons", ownedData);

        // Сохранить экипированное оружие
        for (int i = 0; i < maxSlots; i++)
        {
            string weaponName = equippedWeapons[i] != null ? equippedWeapons[i].weaponName : "";
            PlayerPrefs.SetString($"EquippedSlot{i}", weaponName);
        }

        PlayerPrefs.SetInt("CurrentSlot", currentSlot);
        PlayerPrefs.Save();
    }

    public bool IsOwned(WeaponData weapon)
    {
        return weapon != null && ownedWeapons.Contains(weapon.weaponName);
    }

    public bool PurchaseWeapon(WeaponData weapon, int price)
    {
        if (weapon == null || IsOwned(weapon)) return false;
        
        if (GameManager.Instance.SpendMoney(price))
        {
            ownedWeapons.Add(weapon.weaponName);
            SaveInventory();
            OnInventoryChanged?.Invoke();
            return true;
        }
        return false;
    }

    public bool EquipWeapon(WeaponData weapon, int slotIndex)
    {
        if (weapon == null || !IsOwned(weapon)) return false;
        if (slotIndex < 0 || slotIndex >= maxSlots) return false;

        // Проверить что это оружие не экипировано в другом слоте
        for (int i = 0; i < maxSlots; i++)
        {
            if (i != slotIndex && equippedWeapons[i] == weapon)
            {
                // Убрать из другого слота
                equippedWeapons[i] = null;
                Debug.Log($"WeaponInventory: Removed {weapon.weaponName} from slot {i}");
            }
        }

        equippedWeapons[slotIndex] = weapon;
        SaveInventory();
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void UnequipSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return;
        equippedWeapons[slotIndex] = null;
        SaveInventory();
        OnInventoryChanged?.Invoke();
    }

    public void SwitchToSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return;
        if (slotIndex == currentSlot) return;
        
        currentSlot = slotIndex;
        PlayerPrefs.SetInt("CurrentSlot", currentSlot);
        OnSlotChanged?.Invoke(currentSlot);
    }

    public void NextSlot()
    {
        int next = (currentSlot + 1) % maxSlots;
        SwitchToSlot(next);
    }

    public void PreviousSlot()
    {
        int prev = currentSlot - 1;
        if (prev < 0) prev = maxSlots - 1;
        SwitchToSlot(prev);
    }

    public WeaponData[] GetAllWeapons()
    {
        return allWeapons;
    }

    public WeaponData GetWeaponByName(string weaponName)
    {
        if (allWeapons == null) return null;
        foreach (var weapon in allWeapons)
        {
            if (weapon != null && weapon.weaponName == weaponName)
                return weapon;
        }
        return null;
    }

    public List<WeaponData> GetOwnedWeapons()
    {
        List<WeaponData> owned = new List<WeaponData>();
        if (allWeapons == null) return owned;
        
        foreach (var weapon in allWeapons)
        {
            if (weapon != null && IsOwned(weapon))
                owned.Add(weapon);
        }
        return owned;
    }
}
