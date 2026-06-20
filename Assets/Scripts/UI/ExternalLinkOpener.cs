using UnityEngine;

/// <summary>
/// Simple helper that can be bound to Unity UI Buttons (OnClick)
/// to open an external URL. You can either set the default URL in the inspector
/// or pass it as a parameter from the button click event.
/// </summary>
public class ExternalLinkOpener : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Optional default URL that will be used when OpenLink() is called without parameters.")]
    private string defaultUrl;

    /// <summary>
    /// Opens the default URL defined in the inspector.
    /// </summary>
    public void OpenLink()
    {
        OpenLink(defaultUrl);
    }

    /// <summary>
    /// Opens the provided URL (or falls back to the default URL if empty).
    /// Can be hooked directly from UnityEvents with a string argument.
    /// </summary>
    public void OpenLink(string url)
    {
        var target = string.IsNullOrWhiteSpace(url) ? defaultUrl : url;
        if (string.IsNullOrWhiteSpace(target))
        {
            Debug.LogWarning("[ExternalLinkOpener] Cannot open link: URL is empty");
            return;
        }

        Debug.Log($"[ExternalLinkOpener] Opening external link: {target}");
        Application.OpenURL(target);
    }
}
