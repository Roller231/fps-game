using UnityEngine;
using UnityEngine.UI;

public class WeaponHud : MonoBehaviour
{
    [SerializeField] private WeaponHolder weaponHolder;
    [SerializeField] private Text weaponNameText;
    [SerializeField] private Text ammoText;

    private Weapon subscribedWeapon;

    private void Awake()
    {
        if (weaponHolder == null) weaponHolder = FindObjectOfType<WeaponHolder>();
    }

    private void OnEnable()
    {
        if (weaponHolder != null)
        {
            weaponHolder.OnWeaponChanged += HandleWeaponChanged;
            HandleWeaponChanged(weaponHolder.CurrentWeapon);
        }
    }

    private void OnDisable()
    {
        if (weaponHolder != null) weaponHolder.OnWeaponChanged -= HandleWeaponChanged;
        UnsubscribeWeapon();
    }

    private void HandleWeaponChanged(Weapon weapon)
    {
        UnsubscribeWeapon();
        subscribedWeapon = weapon;
        if (subscribedWeapon != null) subscribedWeapon.OnAmmoChanged += UpdateHud;
        UpdateHud();
    }

    private void UnsubscribeWeapon()
    {
        if (subscribedWeapon != null) subscribedWeapon.OnAmmoChanged -= UpdateHud;
        subscribedWeapon = null;
    }

    private void UpdateHud()
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
