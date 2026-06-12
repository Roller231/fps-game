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
        // Если сохранённый слот пустой — выбрать первый непустой
        if (currentSlot < 0 || currentSlot >= maxSlots || equippedWeapons[currentSlot] == null)
        {
            currentSlot = FindNextNonEmptySlot(-1); // начнём поиск с -1, чтобы взять первый доступный
        }
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
        
        if (MoneyManager.Instance != null && MoneyManager.Instance.SpendMoney(price))
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
                // Если активный слот опустел — позже переключимся на новый слот
            }
        }

        equippedWeapons[slotIndex] = weapon;
        // Автопереключение на слот, куда экипировали, если текущий стал пуст или если хотим сразу активировать новое оружие
        if (currentSlot != slotIndex && (equippedWeapons[currentSlot] == null))
        {
            SwitchToSlot(slotIndex);
        }
        else if (currentSlot != slotIndex)
        {
            // По UX: сразу сделать активным вновь экипированное оружие
            SwitchToSlot(slotIndex);
        }

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

        // Если сняли оружие с активного слота — перейти к ближайшему непустому
        if (slotIndex == currentSlot)
        {
            int next = FindNextNonEmptySlot(currentSlot);
            if (next != currentSlot)
            {
                SwitchToSlot(next);
            }
        }
    }

    public void SwitchToSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return;
        if (slotIndex == currentSlot) return;
        if (equippedWeapons[slotIndex] == null) return; // Нельзя переключаться на пустой слот
        
        currentSlot = slotIndex;
        PlayerPrefs.SetInt("CurrentSlot", currentSlot);
        OnSlotChanged?.Invoke(currentSlot);
    }

    public void NextSlot()
    {
        int next = FindNextNonEmptySlot(currentSlot);
        if (next != currentSlot) SwitchToSlot(next);
    }

    public void PreviousSlot()
    {
        int prev = FindPrevNonEmptySlot(currentSlot);
        if (prev != currentSlot) SwitchToSlot(prev);
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

    private int FindNextNonEmptySlot(int from)
    {
        if (maxSlots <= 0) return 0;
        int start = (from + 1 + maxSlots) % maxSlots;
        int i = start;
        for (int count = 0; count < maxSlots; count++)
        {
            if (equippedWeapons[i] != null) return i;
            i = (i + 1) % maxSlots;
        }
        // все пустые — вернуть текущий или 0
        return currentSlot >= 0 && currentSlot < maxSlots ? currentSlot : 0;
    }

    private int FindPrevNonEmptySlot(int from)
    {
        if (maxSlots <= 0) return 0;
        int i = (from - 1 + maxSlots) % maxSlots;
        for (int count = 0; count < maxSlots; count++)
        {
            if (equippedWeapons[i] != null) return i;
            i = (i - 1 + maxSlots) % maxSlots;
        }
        return currentSlot >= 0 && currentSlot < maxSlots ? currentSlot : 0;
    }
}
