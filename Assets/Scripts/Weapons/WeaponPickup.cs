using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private WeaponData weaponData;
    [SerializeField] private bool autoPickupOnTouch = false;
    [SerializeField] private float interactRange = 3f;
    [SerializeField] private float rotateSpeed = 45f;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerStay(Collider other)
    {
        if (weaponData == null) return;
        if (!other.CompareTag("Player")) return;

        bool wantPickup = autoPickupOnTouch || Input.GetKeyDown(KeyCode.E);
        if (!wantPickup) return;

        var holder = other.GetComponentInChildren<WeaponHolder>();
        if (holder == null) holder = other.GetComponentInParent<WeaponHolder>();
        if (holder == null) return;

        if (TryPickup(holder))
        {
            Destroy(gameObject);
        }
    }

    public bool TryPickup(WeaponHolder holder)
    {
        if (weaponData == null || holder == null) return false;
        return holder.TryPickup(weaponData);
    }

    public float InteractRange => interactRange;
}
