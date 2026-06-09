using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup crosshairGroup;
    [SerializeField] private WeaponHolder weaponHolder;
    [SerializeField] private float fadeSpeed = 12f;
    [SerializeField] private bool hideWhenAiming = true;

    private void Awake()
    {
        if (weaponHolder == null) weaponHolder = FindObjectOfType<WeaponHolder>();
        if (crosshairGroup == null)
        {
            crosshairGroup = GetComponent<CanvasGroup>();
            if (crosshairGroup == null)
            {
                var img = GetComponent<Image>();
                if (img != null)
                {
                    crosshairGroup = gameObject.AddComponent<CanvasGroup>();
                    crosshairGroup.alpha = 1f;
                }
            }
        }
    }

    private void Update()
    {
        if (crosshairGroup == null) return;
        bool aiming = weaponHolder != null && weaponHolder.IsAiming;
        float target = (hideWhenAiming && aiming) ? 0f : 1f;
        crosshairGroup.alpha = Mathf.MoveTowards(crosshairGroup.alpha, target, Time.unscaledDeltaTime * fadeSpeed);
        crosshairGroup.interactable = crosshairGroup.blocksRaycasts = crosshairGroup.alpha > 0.001f;
    }
}
