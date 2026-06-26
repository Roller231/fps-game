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

    /// <summary>
    /// Opens the landing page on the same host where the game is running.
    /// Automatically detects host from Application.absoluteURL (WebGL only).
    /// Falls back to localhost:8000 in Editor.
    /// </summary>
    public void OpenLandingPage()
    {
        string landingUrl = GetLandingPageUrl();
        Debug.Log($"[ExternalLinkOpener] Opening landing page: {landingUrl}");
        Application.OpenURL(landingUrl);
    }

    /// <summary>
    /// Gets the landing page URL based on current host.
    /// </summary>
    private string GetLandingPageUrl()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string pageUrl = Application.absoluteURL;
        if (!string.IsNullOrEmpty(pageUrl))
        {
            try
            {
                System.Uri uri = new System.Uri(pageUrl);
                bool isLocalHost = uri.Host.Equals("localhost", System.StringComparison.OrdinalIgnoreCase) ||
                                   uri.Host.Equals("127.0.0.1");

                if (isLocalHost)
                {
                    // Landing page runs on localhost:8000
                    return $"{uri.Scheme}://{uri.Host}";
                }
                else
                {
                    // Production: same host without port
                    return $"{uri.Scheme}://{uri.Host}";
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ExternalLinkOpener] Failed to parse URL: {ex.Message}");
            }
        }
#elif UNITY_EDITOR
        return "https://fpsmilitarygame.online";
#endif
        // Fallback for Editor or if URL parsing fails
        return "https://fpsmilitarygame.online";
    }
}
