using UnityEngine;

/// <summary>
/// Синхронизирует WeaponInventory (слоты) с WeaponHolder (физические оружия)
/// </summary>
[RequireComponent(typeof(WeaponHolder))]
public class WeaponLoadout : MonoBehaviour
{
    private WeaponHolder holder;

    private void Awake()
    {
        holder = GetComponent<WeaponHolder>();
    }

    private void Start()
    {
        if (WeaponInventory.Instance != null)
        {
            WeaponInventory.Instance.OnSlotChanged += OnSlotChanged;
            WeaponInventory.Instance.OnInventoryChanged += OnInventoryChanged;
            
            // Загрузить начальное оружие
            LoadEquippedWeapons();
        }
    }

    private void OnDestroy()
    {
        if (WeaponInventory.Instance != null)
        {
            WeaponInventory.Instance.OnSlotChanged -= OnSlotChanged;
            WeaponInventory.Instance.OnInventoryChanged -= OnInventoryChanged;
        }
    }

    private void LoadEquippedWeapons()
    {
        if (holder == null || WeaponInventory.Instance == null) return;

        // Очистить текущее оружие
        while (holder.Weapons.Count > 0)
        {
            holder.DropCurrent();
        }

        // Загрузить экипированное оружие
        var equipped = WeaponInventory.Instance.EquippedWeapons;
        Debug.Log($"WeaponLoadout: Loading {equipped.Length} slots");
        
        for (int i = 0; i < equipped.Length; i++)
        {
            if (equipped[i] != null)
            {
                Debug.Log($"WeaponLoadout: Slot {i} = {equipped[i].weaponName}");
                holder.TryPickup(equipped[i]);
            }
            else
            {
                Debug.Log($"WeaponLoadout: Slot {i} is empty");
            }
        }
        
        Debug.Log($"WeaponLoadout: After loading, holder has {holder.Weapons.Count} weapons");

        // Переключиться на текущий слот
        int currentSlot = WeaponInventory.Instance.CurrentSlot;
        if (holder.Weapons.Count > 0)
        {
            // Если слот пустой, переключиться на первый доступный
            if (currentSlot >= holder.Weapons.Count || equipped[currentSlot] == null)
            {
                for (int i = 0; i < equipped.Length; i++)
                {
                    if (equipped[i] != null && i < holder.Weapons.Count)
                    {
                        holder.SwitchWeapon(i);
                        WeaponInventory.Instance.SwitchToSlot(i);
                        return;
                    }
                }
            }
            else
            {
                holder.SwitchWeapon(currentSlot);
            }
        }
    }

    private void OnSlotChanged(int newSlot)
    {
        if (holder == null) return;
        
        Debug.Log($"WeaponLoadout: Slot changed to {newSlot}, holder has {holder.Weapons.Count} weapons");
        
        // Переключить оружие в holder
        if (newSlot < holder.Weapons.Count)
        {
            holder.SwitchWeapon(newSlot);
        }
        else
        {
            Debug.LogWarning($"WeaponLoadout: Cannot switch to slot {newSlot}, only {holder.Weapons.Count} weapons available");
        }
    }

    private void OnInventoryChanged()
    {
        // Перезагрузить оружие при изменении инвентаря
        LoadEquippedWeapons();
    }
}
