using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// UI магазина оружия с покупкой и экипировкой
/// </summary>
public class WeaponShop : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform weaponListContainer;
    [SerializeField] private GameObject weaponItemPrefab;
    [SerializeField] private Text moneyText;

    private List<WeaponShopItem> shopItems = new List<WeaponShopItem>();
    private bool isOpen = false;

    public bool IsOpen => isOpen;

    private void Start()
    {
        if (shopPanel != null) shopPanel.SetActive(false);

        if (WeaponInventory.Instance != null)
        {
            WeaponInventory.Instance.OnInventoryChanged += RefreshShop;
        }
    }

    private void OnDestroy()
    {
        if (WeaponInventory.Instance != null)
        {
            WeaponInventory.Instance.OnInventoryChanged -= RefreshShop;
        }
    }

    private void Update()
    {
        // Открытие по E (закрытие теперь через PauseMenu на Escape)
        if (Input.GetKeyDown(KeyCode.E) && !isOpen)
        {
            OpenShop();
        }
    }

    public void OpenShop()
    {
        if (shopPanel == null) return;
        
        isOpen = true;
        shopPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Отключить управление игрока
        DisablePlayerControls(true);
        
        RefreshShop();
    }

    public void CloseShop()
    {
        if (shopPanel == null) return;
        
        isOpen = false;
        shopPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Включить управление игрока
        DisablePlayerControls(false);
    }

    private void DisablePlayerControls(bool disable)
    {
        // Отключить WeaponHolder
        var weaponHolder = FindObjectOfType<WeaponHolder>();
        if (weaponHolder != null)
        {
            weaponHolder.enabled = !disable;
        }

        // Отключить PlayerMovement
        var playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = !disable;
        }

        // Отключить SimpleFreeLook (камера)
        var freeLook = FindObjectOfType<SimpleFreeLook>();
        if (freeLook != null)
        {
            freeLook.enabled = !disable;
        }
    }

    private void RefreshShop()
    {
        if (!isOpen) return;

        // Обновить деньги
        if (moneyText != null && MoneyManager.Instance != null)
        {
            moneyText.text = $"${MoneyManager.Instance.CurrentMoney}";
        }

        // Очистить список
        foreach (var item in shopItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        shopItems.Clear();

        // Создать элементы для всех оружий
        var allWeapons = WeaponInventory.Instance?.GetAllWeapons();
        if (allWeapons == null || weaponListContainer == null || weaponItemPrefab == null) return;

        foreach (var weapon in allWeapons)
        {
            if (weapon == null) continue;

            GameObject itemObj = Instantiate(weaponItemPrefab, weaponListContainer);
            var shopItem = itemObj.GetComponent<WeaponShopItem>();
            if (shopItem == null) shopItem = itemObj.AddComponent<WeaponShopItem>();

            bool owned = WeaponInventory.Instance.IsOwned(weapon);
            
            shopItem.Setup(weapon, weapon.price, owned, this);
            shopItems.Add(shopItem);
        }
    }

    public void OnPurchaseWeapon(WeaponData weapon, int price)
    {
        if (WeaponInventory.Instance.PurchaseWeapon(weapon, price))
        {
            RefreshShop();
        }
    }

    public void OnEquipWeapon(WeaponData weapon, int slotIndex)
    {
        WeaponInventory.Instance.EquipWeapon(weapon, slotIndex);
        RefreshShop();
    }
}
