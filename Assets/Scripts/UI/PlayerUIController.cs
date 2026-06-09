using UnityEngine;
using UnityEngine.UI;

// Central HUD controller: base health, player health, ammo/weapon info
public class PlayerUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private WeaponHolder weaponHolder;

    private Health[] baseHealthParts;

    [Header("Base Health UI")]
    [SerializeField] private Image baseHealthFill;
    [SerializeField] private Image baseHealthBackground;

    [Header("Player Health UI")]
    [SerializeField] private Image playerHealthFill;
    [SerializeField] private Image playerHealthBackground;

    [Header("Health Bar Settings")]
    [SerializeField] private float healthBarSmoothSpeed = 5f;

    [Header("Ammo UI")]
    [SerializeField] private Text weaponNameText;
    [SerializeField] private Text ammoText;

    private Weapon subscribedWeapon;
    
    // Для плавной анимации красного фона
    private float baseHealthTarget;
    private float playerHealthTarget;
    
    // Сохранённый изначальный максимум HP базы
    private float baseMaxHealthInitial;

    private void Awake()
    {
        if (weaponHolder == null) weaponHolder = FindObjectOfType<WeaponHolder>();
    }

    private void Start()
    {
        // Найти части базы в Start, когда все BasePart уже зарегистрированы
        if (GameManager.Instance != null && GameManager.Instance.Base != null)
        {
            baseHealthParts = GameManager.Instance.Base.GetAllBaseParts();
            Debug.Log($"PlayerUIController: Found {baseHealthParts?.Length ?? 0} base parts");
        }
        else
        {
            Debug.LogWarning("PlayerUIController: GameManager or BaseManager not found!");
        }
        
        // Подписаться на события
        SubscribeBaseHealth();
        if (playerHealth != null) playerHealth.OnHealthChanged.AddListener(UpdatePlayerHealth);

        if (weaponHolder != null)
        {
            weaponHolder.OnWeaponChanged += OnWeaponChanged;
            OnWeaponChanged(weaponHolder.CurrentWeapon);
        }

        // initial update
        UpdateBaseHealthAggregate();
        UpdatePlayerHealth(playerHealth != null ? playerHealth.Current : 0f, playerHealth != null ? playerHealth.Max : 1f);
        UpdateAmmo();
        
        // Инициализировать целевые значения
        baseHealthTarget = baseHealthFill != null ? baseHealthFill.fillAmount : 1f;
        playerHealthTarget = playerHealthFill != null ? playerHealthFill.fillAmount : 1f;
    }

    private void OnEnable()
    {
        // Пустой, всё перенесено в Start
    }

    private void Update()
    {
        // Плавная анимация красного фона
        if (baseHealthBackground != null && baseHealthFill != null)
        {
            baseHealthTarget = Mathf.Lerp(baseHealthTarget, baseHealthFill.fillAmount, Time.deltaTime * healthBarSmoothSpeed);
            baseHealthBackground.fillAmount = baseHealthTarget;
        }

        if (playerHealthBackground != null && playerHealthFill != null)
        {
            playerHealthTarget = Mathf.Lerp(playerHealthTarget, playerHealthFill.fillAmount, Time.deltaTime * healthBarSmoothSpeed);
            playerHealthBackground.fillAmount = playerHealthTarget;
        }
    }

    private void OnDisable()
    {
        UnsubscribeBaseHealth();
        if (playerHealth != null) playerHealth.OnHealthChanged.RemoveListener(UpdatePlayerHealth);

        if (weaponHolder != null) weaponHolder.OnWeaponChanged -= OnWeaponChanged;
        UnsubscribeWeapon();
    }

    private void SubscribeBaseHealth()
    {
        if (baseHealthParts == null) return;
        foreach (var h in baseHealthParts)
        {
            if (h != null) h.OnHealthChanged.AddListener(OnBasePartChanged);
        }
    }

    private void UnsubscribeBaseHealth()
    {
        if (baseHealthParts == null) return;
        foreach (var h in baseHealthParts)
        {
            if (h != null) h.OnHealthChanged.RemoveListener(OnBasePartChanged);
        }
    }

    private void OnBasePartChanged(float _, float __)
    {
        UpdateBaseHealthAggregate();
    }

    private void UpdateBaseHealthAggregate()
    {
        float current = 0f;
        if (baseHealthParts != null)
        {
            foreach (var h in baseHealthParts)
            {
                if (h == null) continue;
                current += Mathf.Max(0f, h.Current);
            }
        }

        // Сохранить изначальный максимум при первом вызове
        if (baseMaxHealthInitial <= 0f && baseHealthParts != null)
        {
            foreach (var h in baseHealthParts)
            {
                if (h == null) continue;
                baseMaxHealthInitial += Mathf.Max(0f, h.Max);
            }
        }

        if (baseHealthFill != null)
        {
            float fill = (baseMaxHealthInitial > 0f) ? Mathf.Clamp01(current / baseMaxHealthInitial) : 0f;
            baseHealthFill.fillAmount = fill;
        }
    }

    private void UpdatePlayerHealth(float current, float max)
    {
        if (playerHealthFill != null)
        {
            float fill = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
            playerHealthFill.fillAmount = fill;
        }
    }

    private void OnWeaponChanged(Weapon weapon)
    {
        UnsubscribeWeapon();
        subscribedWeapon = weapon;
        if (subscribedWeapon != null)
        {
            subscribedWeapon.OnAmmoChanged += UpdateAmmo;
        }
        UpdateAmmo();
    }

    private void UnsubscribeWeapon()
    {
        if (subscribedWeapon != null)
        {
            subscribedWeapon.OnAmmoChanged -= UpdateAmmo;
            subscribedWeapon = null;
        }
    }

    private void UpdateAmmo()
    {
        var weapon = weaponHolder != null ? weaponHolder.CurrentWeapon : null;
        if (weapon == null || weapon.Data == null)
        {
            if (weaponNameText != null) weaponNameText.text = "No Weapon";
            if (ammoText != null) ammoText.text = "-- / --";
            return;
        }

        if (weaponNameText != null) weaponNameText.text = weapon.Data.weaponName;
        if (ammoText != null) ammoText.text = $"{weapon.CurrentAmmo} / {weapon.ReserveAmmo}";
    }
}
