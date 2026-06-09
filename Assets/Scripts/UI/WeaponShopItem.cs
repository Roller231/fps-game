using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Элемент списка оружия в магазине
/// </summary>
public class WeaponShopItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image weaponIcon;
    [SerializeField] private Text weaponNameText;
    [SerializeField] private Text priceText;
    [SerializeField] private Text damageText;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Button equipSlot1Button;
    [SerializeField] private Button equipSlot2Button;
    [SerializeField] private GameObject ownedIndicator;

    private WeaponData weapon;
    private int price;
    private bool isOwned;
    private WeaponShop shop;

    public void Setup(WeaponData weaponData, int weaponPrice, bool owned, WeaponShop weaponShop)
    {
        weapon = weaponData;
        price = weaponPrice;
        isOwned = owned;
        shop = weaponShop;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (weapon == null) return;

        // Иконка
        if (weaponIcon != null && weapon.icon != null)
        {
            weaponIcon.sprite = weapon.icon;
            weaponIcon.enabled = true;
        }
        else if (weaponIcon != null)
        {
            weaponIcon.enabled = false;
        }

        // Название
        if (weaponNameText != null)
        {
            weaponNameText.text = weapon.weaponName;
        }

        // Урон
        if (damageText != null)
        {
            string damageInfo = $"Damage: {weapon.damage}";
            if (weapon.pelletsPerShot > 1)
            {
                damageInfo = $"Damage: {weapon.damage}x{weapon.pelletsPerShot}";
            }
            damageText.text = damageInfo;
        }

        // Цена
        if (priceText != null)
        {
            if (isOwned)
            {
                priceText.text = "OWNED";
                priceText.color = Color.green;
            }
            else
            {
                priceText.text = $"${price}";
                priceText.color = Color.white;
            }
        }

        // Кнопка покупки
        if (purchaseButton != null)
        {
            purchaseButton.gameObject.SetActive(!isOwned);
            purchaseButton.interactable = GameManager.Instance != null && GameManager.Instance.Money >= price;
            purchaseButton.onClick.RemoveAllListeners();
            purchaseButton.onClick.AddListener(OnPurchaseClicked);
        }

        // Кнопки экипировки
        var equipped = WeaponInventory.Instance?.EquippedWeapons;
        
        if (equipSlot1Button != null)
        {
            equipSlot1Button.gameObject.SetActive(isOwned);
            equipSlot1Button.onClick.RemoveAllListeners();
            equipSlot1Button.onClick.AddListener(() => OnEquipClicked(0));
            
            // Подсветить если экипировано, иначе сбросить
            var colors = equipSlot1Button.colors;
            if (equipped != null && equipped[0] == weapon)
            {
                colors.normalColor = Color.green;
                colors.highlightedColor = new Color(0f, 0.8f, 0f);
            }
            else
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            }
            equipSlot1Button.colors = colors;
        }

        if (equipSlot2Button != null)
        {
            equipSlot2Button.gameObject.SetActive(isOwned);
            equipSlot2Button.onClick.RemoveAllListeners();
            equipSlot2Button.onClick.AddListener(() => OnEquipClicked(1));
            
            // Подсветить если экипировано, иначе сбросить
            var colors = equipSlot2Button.colors;
            if (equipped != null && equipped[1] == weapon)
            {
                colors.normalColor = Color.green;
                colors.highlightedColor = new Color(0f, 0.8f, 0f);
            }
            else
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            }
            equipSlot2Button.colors = colors;
        }

        // Индикатор владения
        if (ownedIndicator != null)
        {
            ownedIndicator.SetActive(isOwned);
        }
    }

    private void OnPurchaseClicked()
    {
        if (shop != null && weapon != null)
        {
            shop.OnPurchaseWeapon(weapon, price);
        }
    }

    private void OnEquipClicked(int slotIndex)
    {
        if (shop != null && weapon != null)
        {
            shop.OnEquipWeapon(weapon, slotIndex);
        }
    }
}
