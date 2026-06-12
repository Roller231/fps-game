using System.Collections.Generic;
using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Camera viewModelCamera; // optional weapon-only camera provided by user
    [SerializeField] private Transform weaponSocket;
    [SerializeField] private int maxWeapons = 2;
    [SerializeField] private float recoilRecoverySpeed = 6f;
    [SerializeField] private bool enableAds = true;

    private readonly List<Weapon> weapons = new List<Weapon>();
    private int currentIndex = -1;

    private Vector2 currentRecoil;
    private Vector2 recoilTarget;
    private float adsBlend;
    private bool isAiming;
    private float defaultFov;

    public Weapon CurrentWeapon => (currentIndex >= 0 && currentIndex < weapons.Count) ? weapons[currentIndex] : null;
    public IReadOnlyList<Weapon> Weapons => weapons;
    public Camera PlayerCamera => playerCamera;

    public System.Action<Weapon> OnWeaponChanged;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (playerCamera != null) defaultFov = playerCamera.fieldOfView;
        if (weaponSocket == null)
        {
            // Auto-create a socket under the camera so the weapon always follows camera orientation
            var socketGO = new GameObject("WeaponSocket");
            if (viewModelCamera != null)
            {
                socketGO.transform.SetParent(viewModelCamera.transform, false);
                socketGO.transform.localPosition = new Vector3(0.25f, -0.25f, 0.5f);
                socketGO.transform.localRotation = Quaternion.identity;
            }
            else if (playerCamera != null)
            {
                socketGO.transform.SetParent(playerCamera.transform, false);
                socketGO.transform.localPosition = new Vector3(0.25f, -0.25f, 0.5f);
                socketGO.transform.localRotation = Quaternion.identity;
            }
            else
            {
                socketGO.transform.SetParent(transform, false);
            }
            weaponSocket = socketGO.transform;
        }
    }

    private void Update()
    {
        HandleInput();
        HandleRecoilRecovery();
        HandleAds();
    }

    private void HandleInput()
    {
        // Блокируем стрельбу и переключение в паузе
        if (PauseMenu.IsPaused)
            return;

        var weapon = CurrentWeapon;
        if (weapon != null)
        {
            // Используем явные кнопки мыши, чтобы LeftControl (стандартный альтернативный Fire1) не стрелял
            if (Input.GetMouseButtonDown(0)) weapon.HandleTriggerPressed();
            if (Input.GetMouseButton(0)) weapon.HandleTriggerHeld();
            if (Input.GetKeyDown(KeyCode.R)) weapon.TryReload();
        }

        if (enableAds)
        {
            if (Input.GetMouseButtonDown(1)) isAiming = true;
            if (Input.GetMouseButtonUp(1)) isAiming = false;
        }

        // Переключение оружия теперь управляется через WeaponInventory/WeaponSlotsUI
        // Оставляем только для совместимости, если WeaponInventory не используется
        if (WeaponInventory.Instance == null)
        {
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            if (scroll > 0f) SwitchWeapon(currentIndex + 1);
            else if (scroll < 0f) SwitchWeapon(currentIndex - 1);

            for (int i = 0; i < 9 && i < weapons.Count; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) SwitchWeapon(i);
            }
        }

        if (Input.GetKeyDown(KeyCode.G)) DropCurrent();
    }

    private void HandleRecoilRecovery()
    {
        currentRecoil = Vector2.Lerp(currentRecoil, recoilTarget, Time.deltaTime * recoilRecoverySpeed);
        recoilTarget = Vector2.Lerp(recoilTarget, Vector2.zero, Time.deltaTime * recoilRecoverySpeed * 0.5f);
    }

    private void HandleAds()
    {
        var w = CurrentWeapon;
        if (!enableAds || w == null || w.Data == null || weaponSocket == null) return;

        float target = isAiming ? 1f : 0f;
        float t = (w.Data.adsBlendTime > 0f) ? Time.deltaTime / w.Data.adsBlendTime : 1f;
        adsBlend = Mathf.MoveTowards(adsBlend, target, t);

        Vector3 hip = w.Data.hipLocalPos;
        Vector3 ads = w.Data.adsLocalPos;
        weaponSocket.localPosition = Vector3.Lerp(hip, ads, adsBlend);

        if (playerCamera != null)
        {
            float hipFov = Mathf.Approximately(w.Data.hipFov, 0f) ? defaultFov : w.Data.hipFov; // now intended to be NARROW
            float adsFov = Mathf.Approximately(w.Data.adsFov, 0f) ? defaultFov : w.Data.adsFov; // now intended to be WIDE
            // Not aiming (adsBlend=0): use hipFov (narrow). Aiming (adsBlend=1): use adsFov (wide).
            playerCamera.fieldOfView = Mathf.Lerp(hipFov, adsFov, adsBlend);
        }
        if (viewModelCamera != null && playerCamera != null)
        {
            // Keep viewmodel FOV in sync with main for consistent look
            viewModelCamera.fieldOfView = playerCamera.fieldOfView;
        }
    }

    public Vector2 ConsumeRecoilDelta()
    {
        Vector2 delta = currentRecoil;
        currentRecoil = Vector2.zero;
        return delta;
    }

    public bool TryPickup(WeaponData data)
    {
        if (data == null) return false;

        foreach (var w in weapons)
        {
            if (w.Data == data)
            {
                w.AddReserveAmmo(data.magazineSize);
                return true;
            }
        }

        if (weapons.Count >= maxWeapons)
        {
            DropCurrent();
        }

        var prefab = data.viewModelPrefab;
        GameObject instance;
        if (prefab != null)
        {
            instance = Instantiate(prefab, weaponSocket);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
        }
        else
        {
            instance = new GameObject(data.weaponName);
            instance.transform.SetParent(weaponSocket, false);
        }

        var weapon = instance.GetComponent<Weapon>();
        if (weapon == null) weapon = instance.AddComponent<Weapon>();
        weapon.Initialize(playerCamera, data);
        weapon.OnShot += HandleShot;

        weapons.Add(weapon);
        SwitchWeapon(weapons.Count - 1);
        return true;
    }

    private void HandleShot()
    {
        var w = CurrentWeapon;
        if (w == null) return;
        float recoilMul = enableAds && isAiming ? w.Data.adsRecoilMultiplier : 1f;
        recoilTarget += new Vector2(-w.Data.recoilPerShot.y * recoilMul, Random.Range(-w.Data.recoilPerShot.x, w.Data.recoilPerShot.x) * recoilMul);
        currentRecoil = recoilTarget;
    }

    public bool IsAiming => enableAds && isAiming;

    public void SwitchWeapon(int index)
    {
        if (weapons.Count == 0)
        {
            currentIndex = -1;
            OnWeaponChanged?.Invoke(null);
            return;
        }

        index = (index % weapons.Count + weapons.Count) % weapons.Count;
        if (index == currentIndex) return;

        for (int i = 0; i < weapons.Count; i++)
        {
            weapons[i].gameObject.SetActive(i == index);
        }
        currentIndex = index;
        OnWeaponChanged?.Invoke(CurrentWeapon);
    }

    public void DropCurrent()
    {
        var w = CurrentWeapon;
        if (w == null) return;

        weapons.RemoveAt(currentIndex);
        Destroy(w.gameObject);

        if (weapons.Count == 0)
        {
            currentIndex = -1;
            OnWeaponChanged?.Invoke(null);
        }
        else
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, weapons.Count - 1);
            for (int i = 0; i < weapons.Count; i++) weapons[i].gameObject.SetActive(i == currentIndex);
            OnWeaponChanged?.Invoke(CurrentWeapon);
        }
    }
}
