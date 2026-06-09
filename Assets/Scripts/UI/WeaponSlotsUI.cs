using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Отображение слотов оружия в HUD (1, 2 с иконками)
/// </summary>
public class WeaponSlotsUI : MonoBehaviour
{
    [Header("Slot 1")]
    [SerializeField] private Image slot1Icon;
    [SerializeField] private Text slot1Number;
    [SerializeField] private GameObject slot1Highlight;
    [SerializeField] private CanvasGroup slot1CanvasGroup;

    [Header("Slot 2")]
    [SerializeField] private Image slot2Icon;
    [SerializeField] private Text slot2Number;
    [SerializeField] private GameObject slot2Highlight;
    [SerializeField] private CanvasGroup slot2CanvasGroup;

    [Header("Settings")]
    [SerializeField] private float activeAlpha = 1f;
    [SerializeField] private float inactiveAlpha = 0.5f;
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private void Start()
    {
        if (slot1Number != null) slot1Number.text = "1";
        if (slot2Number != null) slot2Number.text = "2";

        if (WeaponInventory.Instance != null)
        {
            WeaponInventory.Instance.OnInventoryChanged += UpdateUI;
            WeaponInventory.Instance.OnSlotChanged += OnSlotChanged;
        }

        UpdateUI();
    }

    private void OnDestroy()
    {
        if (WeaponInventory.Instance != null)
        {
            WeaponInventory.Instance.OnInventoryChanged -= UpdateUI;
            WeaponInventory.Instance.OnSlotChanged -= OnSlotChanged;
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (WeaponInventory.Instance == null) return;

        // Переключение на цифры
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            WeaponInventory.Instance.SwitchToSlot(0);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            WeaponInventory.Instance.SwitchToSlot(1);
        }

        // Переключение колесиком мыши
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f)
        {
            WeaponInventory.Instance.PreviousSlot();
        }
        else if (scroll < 0f)
        {
            WeaponInventory.Instance.NextSlot();
        }
    }

    private void UpdateUI()
    {
        if (WeaponInventory.Instance == null) return;

        var equipped = WeaponInventory.Instance.EquippedWeapons;
        int currentSlot = WeaponInventory.Instance.CurrentSlot;

        // Слот 1
        UpdateSlot(0, equipped[0], slot1Icon, slot1Highlight, slot1CanvasGroup, currentSlot == 0);
        
        // Слот 2
        UpdateSlot(1, equipped[1], slot2Icon, slot2Highlight, slot2CanvasGroup, currentSlot == 1);
    }

    private void UpdateSlot(int slotIndex, WeaponData weapon, Image icon, GameObject highlight, CanvasGroup canvasGroup, bool isActive)
    {
        // Иконка
        if (icon != null)
        {
            if (weapon != null && weapon.icon != null)
            {
                icon.sprite = weapon.icon;
                icon.enabled = true;
                icon.color = isActive ? activeColor : inactiveColor;
            }
            else
            {
                icon.enabled = false;
            }
        }

        // Подсветка активного слота
        if (highlight != null)
        {
            highlight.SetActive(isActive && weapon != null);
        }

        // Прозрачность
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isActive ? activeAlpha : inactiveAlpha;
        }
    }

    private void OnSlotChanged(int newSlot)
    {
        UpdateUI();
    }
}
