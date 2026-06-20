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
    private readonly Dictionary<string, WeaponAmmoState> ammoStates = new Dictionary<string, WeaponAmmoState>();
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
        // LoadInventory вызывается через ProfileService.LoadFromProfile
    }

    // PlayerPrefs больше не используется - всё через ProfileService

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Загружает инвентарь из профиля (вызывается ProfileService)
    /// </summary>
    public void LoadFromProfile(ProfileData profile)
    {
        ownedWeapons.Clear();
        ammoStates.Clear();
        System.Array.Clear(equippedWeapons, 0, equippedWeapons.Length);
        currentSlot = 0;
        var assignedWeapons = new HashSet<string>();
        
        // Загружаем купленное оружие из профиля
        if (profile.weapons != null)
        {
            foreach (var w in profile.weapons)
            {
                ownedWeapons.Add(w.weapon_name);
                ammoStates[w.weapon_name] = new WeaponAmmoState
                {
                    currentAmmo = Mathf.Max(0, w.magazine_ammo),
                    reserveAmmo = Mathf.Max(0, w.reserve_ammo)
                };
            }
        }

        // Дать стартовый пистолет если ничего нет
        if (ownedWeapons.Count == 0 && allWeapons != null && allWeapons.Length > 0)
        {
            var pistol = GetWeaponByName("Pistol");
            if (pistol != null)
            {
                ownedWeapons.Add(pistol.weaponName);
                ammoStates[pistol.weaponName] = new WeaponAmmoState
                {
                    currentAmmo = pistol.magazineSize,
                    reserveAmmo = pistol.reservedAmmo
                };
            }
        }

        // Загружаем экипированные слоты из профиля
        if (profile.equipped_slots != null && profile.equipped_slots.Length > 0)
        {
            for (int i = 0; i < maxSlots && i < profile.equipped_slots.Length; i++)
            {
                string slotName = profile.equipped_slots[i];
                if (string.IsNullOrEmpty(slotName) || assignedWeapons.Contains(slotName))
                    continue;

                var weapon = GetWeaponByName(slotName);
                if (weapon != null && IsOwned(weapon))
                {
                    equippedWeapons[i] = weapon;
                    assignedWeapons.Add(weapon.weaponName);
                }
            }
        }

        // Fallback на один слот из старого поля, если ничего не поставили
        if (assignedWeapons.Count == 0 && !string.IsNullOrEmpty(profile.equipped_weapon))
        {
            var weapon = GetWeaponByName(profile.equipped_weapon);
            if (weapon != null && IsOwned(weapon))
            {
                equippedWeapons[0] = weapon;
                assignedWeapons.Add(weapon.weaponName);
            }
        }

        // Заполнить оставшиеся пустые слоты любым доступным оружием
        for (int i = 0; i < maxSlots; i++)
        {
            if (equippedWeapons[i] == null)
            {
                var fallback = FindFirstOwnedWeapon(assignedWeapons);
                if (fallback != null)
                {
                    equippedWeapons[i] = fallback;
                    assignedWeapons.Add(fallback.weaponName);
                }
            }
        }

        // Гарантировать хотя бы одно оружие
        if (assignedWeapons.Count == 0)
        {
            var fallback = FindFirstOwnedWeapon(null);
            if (fallback != null)
            {
                equippedWeapons[0] = fallback;
                assignedWeapons.Add(fallback.weaponName);
            }
        }

        // Синхронизировать текущий слот с первым непустым
        if (equippedWeapons[currentSlot] == null)
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (equippedWeapons[i] != null)
                {
                    currentSlot = i;
                    break;
                }
            }
            if (currentSlot >= maxSlots || equippedWeapons[currentSlot] == null)
            {
                currentSlot = 0;
            }
        }

        OnInventoryChanged?.Invoke();
        Debug.Log($"[WeaponInventory] Loaded from profile: {ownedWeapons.Count} weapons owned");
    }

    /// <summary>
    /// Сохраняет инвентарь в профиль (вызывается ProfileService)
    /// </summary>
    public void SaveToProfile(ProfileData profile)
    {
        var weaponsList = new List<WeaponDataItem>();

        WeaponHolder holder = FindObjectOfType<WeaponHolder>();
        Dictionary<string, WeaponAmmoState> runtimeStates = holder != null ? holder.GetRuntimeStates() : null;

        foreach (var weaponName in ownedWeapons)
        {
            WeaponAmmoState state;
            if (runtimeStates != null && runtimeStates.TryGetValue(weaponName, out state))
            {
                ammoStates[weaponName] = state;
            }
            else if (!ammoStates.TryGetValue(weaponName, out state))
            {
                var weaponData = GetWeaponByName(weaponName);
                state = new WeaponAmmoState
                {
                    currentAmmo = weaponData != null ? weaponData.magazineSize : 0,
                    reserveAmmo = weaponData != null ? weaponData.reservedAmmo : 0
                };
                ammoStates[weaponName] = state;
            }

            weaponsList.Add(new WeaponDataItem
            {
                weapon_name = weaponName,
                reserve_ammo = state.reserveAmmo,
                magazine_ammo = state.currentAmmo
            });
        }

        profile.weapons = weaponsList.ToArray();

        var slotNames = new string[maxSlots];
        for (int i = 0; i < maxSlots; i++)
        {
            slotNames[i] = equippedWeapons[i] != null ? equippedWeapons[i].weaponName : null;
        }
        profile.equipped_slots = slotNames;
        profile.equipped_weapon = CurrentWeapon != null ? CurrentWeapon.weaponName : null;
    }

    public bool TryGetSavedAmmo(string weaponName, out WeaponAmmoState state)
    {
        if (ammoStates.TryGetValue(weaponName, out state))
        {
            return true;
        }

        var data = GetWeaponByName(weaponName);
        if (data != null)
        {
            state = new WeaponAmmoState
            {
                currentAmmo = data.magazineSize,
                reserveAmmo = data.reservedAmmo
            };
            ammoStates[weaponName] = state;
            return true;
        }

        state = default;
        return false;
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
            OnInventoryChanged?.Invoke();
            // Сохранение через ProfileService автоматически
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

        OnInventoryChanged?.Invoke();
        return true;
    }

    public void UnequipSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= maxSlots) return;
        equippedWeapons[slotIndex] = null;
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

    private WeaponData FindFirstOwnedWeapon(HashSet<string> excluded)
    {
        if (allWeapons == null) return null;
        foreach (var weapon in allWeapons)
        {
            if (weapon == null) continue;
            if (!IsOwned(weapon)) continue;
            if (excluded != null && excluded.Contains(weapon.weaponName)) continue;
            return weapon;
        }
        return null;
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

public struct WeaponAmmoState
{
    public int currentAmmo;
    public int reserveAmmo;
}
