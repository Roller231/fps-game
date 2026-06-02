using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 100f;
    [SerializeField] private float maxLifeTime = 5f;
    [SerializeField] private float gravity = 0f;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float headshotMultiplier = 2f;
    [SerializeField] private float impactForce = 10f;
    [SerializeField] private GameObject impactPrefab;
    [SerializeField] private GameObject enemyImpactPrefab;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private LineRenderer lineTracer;
    [Header("Trail")]
    [SerializeField] private bool autoAddTrail = true;
    [SerializeField] private float trailTime = 0.2f;
    [SerializeField] private float trailStartWidth = 0.03f;
    [SerializeField] private float trailEndWidth = 0.0f;
    [SerializeField] private Material trailMaterial;

    private Rigidbody rb;
    private Vector3 prevPos;
    private TrailRenderer trail;
    private static Material defaultTrailMaterial;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Ensure there's a collider to receive collisions
        if (GetComponent<Collider>() == null)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 0.02f;
            sc.isTrigger = false;
        }

        trail = GetComponent<TrailRenderer>();
        if (trail == null && autoAddTrail)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.startWidth = trailStartWidth;
            trail.endWidth = trailEndWidth;
            if (trailMaterial != null) trail.material = trailMaterial;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.alignment = LineAlignment.View;
        }

        // Fallback material to avoid magenta if none assigned
        if (defaultTrailMaterial == null)
        {
            var shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader != null)
            {
                defaultTrailMaterial = new Material(shader);
                defaultTrailMaterial.color = Color.white;
            }
        }

        if (trail != null && trail.material == null && defaultTrailMaterial != null)
        {
            trail.material = defaultTrailMaterial;
        }
    }

    private void OnEnable()
    {
        prevPos = transform.position;
        if (lineTracer == null) lineTracer = GetComponent<LineRenderer>();
        if (lineTracer != null)
        {
            lineTracer.positionCount = 2;
            lineTracer.useWorldSpace = true;
            lineTracer.SetPosition(0, prevPos);
            lineTracer.SetPosition(1, prevPos);
            if (lineTracer.material == null && defaultTrailMaterial != null)
            {
                lineTracer.material = defaultTrailMaterial;
            }
        }
        Destroy(gameObject, maxLifeTime);
    }

    private void LateUpdate()
    {
        if (lineTracer != null)
        {
            lineTracer.SetPosition(0, prevPos);
            lineTracer.SetPosition(1, transform.position);
            prevPos = transform.position;
        }
    }

    public void Launch(Vector3 velocity, float dmg, float force, GameObject impactFx, LayerMask mask, GameObject enemyImpactFx = null, float headshotMul = 1f)
    {
        damage = dmg;
        impactForce = force;
        impactPrefab = impactFx;
        enemyImpactPrefab = enemyImpactFx;
        headshotMultiplier = headshotMul;
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
        bool allowed = ((1 << collision.gameObject.layer) & hitMask) != 0;

        var contact = collision.contacts.Length > 0 ? collision.contacts[0] : default;
        if (allowed)
        {
            var dmg = collision.collider.GetComponentInParent<IDamageable>();
            bool isHead = collision.collider.CompareTag("Head");
            bool hitEnemy = dmg != null;
            if (hitEnemy)
            {
                float appliedDamage = isHead ? damage * headshotMultiplier : damage;
                dmg.TakeDamage(appliedDamage, contact.point, contact.normal);
            }
            if (collision.rigidbody != null)
            {
                collision.rigidbody.AddForceAtPosition(rb.velocity.normalized * impactForce, contact.point, ForceMode.Impulse);
            }
            if (hitEnemy && enemyImpactPrefab != null)
            {
                var fx = Instantiate(enemyImpactPrefab, contact.point, Quaternion.LookRotation(contact.normal));
                Destroy(fx, 3f);
            }
            else if (!hitEnemy && impactPrefab != null)
            {
                var fx = Instantiate(impactPrefab, contact.point, Quaternion.LookRotation(contact.normal));
                Destroy(fx, 3f);
            }
        }

        Destroy(gameObject);
    }
}
