using UnityEngine;

public class CompassTarget : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform targetCamera;
    [SerializeField] private Sprite iconSprite;

    // Settings
    [SerializeField] private bool hideWhenOutsideOfRange;
    [SerializeField] private float visibilityRange;
    [SerializeField] private bool showDistanceToTarget;
    [SerializeField] private int distanceTextRoundDecimals;

    #region Public Properties
    public Sprite IconSprite => iconSprite;
    public float VisibilityRange => visibilityRange;
    public bool HideWhenOutsideOfRange => hideWhenOutsideOfRange;
    public bool ShowDistanceToTarget => showDistanceToTarget;
    public int DistanceTextRoundDecimals => distanceTextRoundDecimals;
    #endregion

    private void Awake()
    {
        // Auto-assign player if not set
        if (player == null)
        {
            // Try GameManager if present
            var gm = FindObjectOfType<GameManager>();
            if (gm != null && gm.Player != null) player = gm.Player;
            if (player == null)
            {
                var pm = FindObjectOfType<PlayerMovement>();
                if (pm != null) player = pm.transform;
            }
            if (player == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go != null) player = go.transform;
            }
        }

        // Auto-assign camera to MainCamera if not set
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
    }

    private void Start()
    {
        if (CompassIconController.Instance != null)
            CompassIconController.Instance.AddCompassTargetIcon(this);
    }

    private void OnEnable()
    {
        if (CompassIconController.Instance != null)
            CompassIconController.Instance.AddCompassTargetIcon(this);
    }

    private void OnDisable()
    {
        if (CompassIconController.Instance != null)
            CompassIconController.Instance.RemoveCompassTargetIcon(this);
    }

    public void Setup(Transform player, Transform cameraTransform)
    {
        this.player = player;
        this.targetCamera = cameraTransform;
    }

    public float DistanceBetweenPlayerAndTarget()
    {
        if (player == null) return 0f;
        return (player.position - this.transform.position).magnitude;
    }

    public float SignedHorizontalAngleFromCameraToTarget()
    {
        if (targetCamera == null) return 0f;
        Vector3 playerToTargetVector = (new Vector3(transform.position.x, 0f, transform.position.z) - new Vector3(targetCamera.position.x, 0f, targetCamera.position.z)).normalized;

        Vector3 forwardVector = new Vector3(targetCamera.forward.x, 0f, targetCamera.forward.z);

        return Vector3.SignedAngle(forwardVector, playerToTargetVector, Vector3.up);
    }

    public bool TargetIsInFrontOfCamera()
    {
        if (targetCamera == null) return false;
        return Vector3.Dot(targetCamera.forward, (transform.position - targetCamera.position).normalized) > 0;
    }
}