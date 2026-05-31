using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 100f;
    [SerializeField] private float maxLifeTime = 5f;
    [SerializeField] private float gravity = 0f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float impactForce = 10f;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private LayerMask hitMask = ~0;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void OnEnable()
    {
        Destroy(gameObject, maxLifeTime);
    }

    public void Launch(Vector3 velocity, float dmg, float force, GameObject impactFx, LayerMask mask)
    {
        damage = dmg;
        impactForce = force;
        impactPrefab = impactFx;
        hitMask = mask;
        rb.velocity = velocity;
    }

    private void FixedUpdate()
    {
        if (gravity != 0f)
        {
            rb.velocity += Vector3.up * Physics.gravity.y * gravity * Time.fixedDeltaTime;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & hitMask) == 0)
        {
            return;
        }

        var contact = collision.contacts.Length > 0 ? collision.contacts[0] : default;
        var dmg = collision.collider.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage, contact.point, contact.normal);
        }
        if (collision.rigidbody != null)
        {
            collision.rigidbody.AddForceAtPosition(rb.velocity.normalized * impactForce, contact.point, ForceMode.Impulse);
        }
        if (impactPrefab != null)
        {
            var fx = Instantiate(impactPrefab, contact.point, Quaternion.LookRotation(contact.normal));
            Destroy(fx, 3f);
        }
        Destroy(gameObject);
    }
}
