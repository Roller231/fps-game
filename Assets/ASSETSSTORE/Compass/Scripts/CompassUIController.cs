using System.Collections.Generic;
using UnityEngine;

public class CompassIconController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private CompassTargetIcon compassTargetIconPrefab;
    [SerializeField] private RectTransform iconContainer;

    private RectTransform rectTransform;

    public static CompassIconController Instance;

    private Dictionary<CompassTarget, CompassTargetIcon> compassTargetAndIconPairs = new Dictionary<CompassTarget, CompassTargetIcon>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        else
            Destroy(this.gameObject);

        rectTransform = this.GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main;
        }
        UpdateIconPositionOnCompass();
    }

    public void AddCompassTargetIcon(CompassTarget compassTarget)
    {
        if (compassTargetAndIconPairs.TryGetValue(compassTarget, out CompassTargetIcon icon) == true)
            return;

        CompassTargetIcon targetIcon = Instantiate(compassTargetIconPrefab, parent: iconContainer);
        targetIcon.Setup(compassTarget);

        // Place icon immediately at correct position to avoid initial pop
        if (targetCamera == null && Camera.main != null) targetCamera = Camera.main;
        if (targetCamera != null)
        {
            float horizontalFOV = HorizontalFOV(targetCamera.fieldOfView, targetCamera.aspect);
            // Temporarily disable image to avoid one-frame flicker, then enable after positioning
            var img = targetIcon.GetComponent<UnityEngine.UI.Image>();
            bool prev = img != null && img.enabled;
            if (img != null) img.enabled = false;
            targetIcon.UpdateIcon(Screen.width, horizontalFOV, compassUIHorizontalOffSet: rectTransform.anchoredPosition.x, iconContainer);
            if (img != null) img.enabled = prev;
        }

        compassTargetAndIconPairs.Add(compassTarget, targetIcon);
    }

    public void RemoveCompassTargetIcon(CompassTarget compassTarget)
    {
        if (!compassTargetAndIconPairs.TryGetValue(compassTarget, out var targetIcon)) return;
        compassTargetAndIconPairs.Remove(compassTarget);
        if (targetIcon != null) Destroy(targetIcon.gameObject);
    }

    private void UpdateIconPositionOnCompass()
    {
        if (targetCamera == null) return;
        float horizontalFOV = HorizontalFOV(targetCamera.fieldOfView, targetCamera.aspect);

        foreach (CompassTargetIcon targetIcon in compassTargetAndIconPairs.Values)
        {
            targetIcon.UpdateIcon(Screen.width, horizontalFOV, compassUIHorizontalOffSet: rectTransform.anchoredPosition.x, iconContainer);
        }
    }

    private float HorizontalFOV(float verticalFOV, float aspectRatio)
    {
        return 2f * Mathf.Atan(Mathf.Tan(verticalFOV * Mathf.Deg2Rad / 2f) * aspectRatio) * Mathf.Rad2Deg;
    }
}