using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rebinds ProfileService UI references whenever the main menu scene loads.
/// Place this on a GameObject in the menu and assign the UI elements in the inspector.
/// </summary>
public class ProfileUIBinder : MonoBehaviour
{
    [SerializeField] private Image avatarImage;
    [SerializeField] private Text displayNameText;
    [SerializeField] private Text balanceText;
    [SerializeField] private GameObject unauthorizedPanel;

    private void OnEnable()
    {
        ApplyBindings();
    }

    private void Start()
    {
        ApplyBindings();
    }

    private void ApplyBindings()
    {
        if (ProfileService.Instance == null)
            return;

        ProfileService.Instance.SetUiBindings(avatarImage, displayNameText, balanceText, unauthorizedPanel);
    }
}
