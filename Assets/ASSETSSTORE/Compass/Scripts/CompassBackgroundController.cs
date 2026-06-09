using UnityEngine;
using UnityEngine.UI;

public class CompassBackgroundController : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private RawImage compassBackground;

    private void Update()
    {
        UpdateCompassBackGroundPosition();
    }

    private void UpdateCompassBackGroundPosition()
    {
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main;
        }
        if (targetCamera == null || compassBackground == null) return;
        compassBackground.uvRect = new Rect(new Vector2(targetCamera.transform.eulerAngles.y / 360f, 0), compassBackground.uvRect.size);
    }
}
