using UnityEngine;

public class WeaponInteractor : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private WeaponHolder weaponHolder;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private LayerMask interactMask = ~0;

    private void Awake()
    {
        if (playerCamera == null) playerCamera = Camera.main;
        if (weaponHolder == null) weaponHolder = GetComponentInChildren<WeaponHolder>();
        if (weaponHolder == null) weaponHolder = GetComponentInParent<WeaponHolder>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        if (playerCamera == null || weaponHolder == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask, QueryTriggerInteraction.Collide)) return;

        var pickup = hit.collider.GetComponentInParent<WeaponPickup>();
        if (pickup == null) return;

        if (pickup.TryPickup(weaponHolder))
        {
            Destroy(pickup.gameObject);
        }
    }
}
