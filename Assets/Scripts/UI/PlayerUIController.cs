using UnityEngine;
using UnityEngine.UI;

// Central HUD controller: base health, player health, ammo/weapon info
public class PlayerUIController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Health[] baseHealthParts;
    [SerializeField] private Health playerHealth;
    [SerializeField] private WeaponHolder weaponHolder;

    [Header("Base Health UI")]
    [SerializeField] private Image baseHealthFill;

    [Header("Player Health UI")]
    [SerializeField] private Image playerHealthFill;

    [Header("Ammo UI")]
    [SerializeField] private Text weaponNameText;
    [SerializeField] private Text ammoText;

    private Weapon subscribedWeapon;

    private void Awake()
    {
        if (weaponHolder == null) weaponHolder = FindObjectOfType<WeaponHolder>();
    }

    private void OnEnable()
    {
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
        float max = 0f;
        if (baseHealthParts != null)
        {
            foreach (var h in baseHealthParts)
            {
                if (h == null) continue;
                current += Mathf.Max(0f, h.Current);
                max += Mathf.Max(0f, h.Max);
            }
        }

        if (baseHealthFill != null)
        {
            float fill = (max > 0f) ? Mathf.Clamp01(current / max) : 0f;
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
