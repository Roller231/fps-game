using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Validates access token passed via URL (?token=) against backend API before enabling gameplay.
/// </summary>
public class AuthManager : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string backendBaseUrl = "http://localhost:8000";
    [SerializeField] private bool autoDetectBackendUrl = true;

    [Header("UI")]
    [SerializeField] private GameObject gatePanel;
    [SerializeField] private Text gateMessage;
    [SerializeField] private string landingPageUrl = "http://localhost:8000";

    [Header("Gameplay Locks")]
    [SerializeField] private Behaviour[] disabledBehaviours;
    [SerializeField] private GameObject[] disabledObjects;

    public static string CurrentToken { get; private set; }
    public static bool IsTokenMissing { get; private set; }

    public event Action<string> OnTokenValidated;
    public event Action OnTokenMissing;

    private void Awake()
    {
        SetGateState(true, "Validating access token...");
        ToggleGameplay(false);

        // Auto-detect backend URL from page URL (WebGL only)
        if (autoDetectBackendUrl)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            string pageUrl = Application.absoluteURL;
            if (!string.IsNullOrEmpty(pageUrl))
            {
                System.Uri uri = new System.Uri(pageUrl);
                backendBaseUrl = $"{uri.Scheme}://{uri.Host}:8000";
                landingPageUrl = $"{uri.Scheme}://{uri.Host}:8000";
                Debug.Log($"[AuthManager] Auto-detected backend URL: {backendBaseUrl}");
            }
#endif
        }
    }

    private void Start()
    {
        IsTokenMissing = false;

        string token = ExtractTokenFromUrl();
        if (string.IsNullOrEmpty(token))
        {
            IsTokenMissing = true;
            SetGateState(true, $"Access token not found.\n\nPlease log in at:\n{landingPageUrl}\n\nThen press PLAY to launch the game.");
            OnTokenMissing?.Invoke();
            return;
        }

        StartCoroutine(ValidateToken(token));
    }

    private string ExtractTokenFromUrl()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        string url = Application.absoluteURL;
#else
        string url = Application.absoluteURL;
        if (string.IsNullOrEmpty(url))
        {
            url = System.Environment.GetEnvironmentVariable("GAME_LAUNCH_URL");
            Debug.Log($"[AuthManager] absoluteURL empty, env GAME_LAUNCH_URL='{url}'");
            if (string.IsNullOrEmpty(url))
            {
                url = "http://localhost:8000/?token=KWssV6lMegxpwYsZB9oXcXrh5ZGn6tHgiOm4bES3YUjun7hgi6IT937QJ5njoXbo";
                Debug.Log("[AuthManager] Using fallback editor token");
            }
        }
#endif
        if (string.IsNullOrEmpty(url))
            return null;

        Debug.Log($"[AuthManager] ExtractTokenFromUrl raw url: {url}");

        int queryIndex = url.IndexOf('?');
        if (queryIndex < 0)
            return null;

        string query = url.Substring(queryIndex + 1);
        var parts = query.Split('&');
        foreach (var part in parts)
        {
            var kv = part.Split('=');
            if (kv.Length == 2 && kv[0].Equals("token", StringComparison.OrdinalIgnoreCase))
            {
                string token = Uri.UnescapeDataString(kv[1]);
                Debug.Log($"[AuthManager] Token found in url: {token}");
                return token;
            }
        }
        return null;
    }

    private IEnumerator ValidateToken(string token)
    {
        string url = $"{backendBaseUrl}/api/token/validate?token={UnityWebRequest.EscapeURL(token)}";
        using (var request = UnityWebRequest.Get(url))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool hasError = request.result == UnityWebRequest.Result.ConnectionError ||
                            request.result == UnityWebRequest.Result.ProtocolError;
#else
            bool hasError = request.isNetworkError || request.isHttpError;
#endif
            if (hasError)
            {
                Debug.LogWarning($"[AuthManager] Token validation failed: {request.error} | {request.downloadHandler?.text}");
                IsTokenMissing = true;
                SetGateState(true, $"Invalid or expired access token.\n\nPlease log in again at:\n{landingPageUrl}\n\nThen press PLAY to launch the game.");
                OnTokenMissing?.Invoke();
                yield break;
            }

            CurrentToken = token;
            IsTokenMissing = false;
            SetGateState(false, "Token validated. Welcome back.");
            ToggleGameplay(true);
            OnTokenValidated?.Invoke(token);
        }
    }

    private void SetGateState(bool showGate, string message)
    {
        if (gatePanel != null)
            gatePanel.SetActive(showGate);

        if (gateMessage != null)
            gateMessage.text = message;
    }

    private void ToggleGameplay(bool enable)
    {
        if (disabledBehaviours != null)
        {
            foreach (var behaviour in disabledBehaviours)
            {
                if (behaviour != null)
                    behaviour.enabled = enable;
            }
        }

        if (disabledObjects != null)
        {
            foreach (var go in disabledObjects)
            {
                if (go != null)
                    go.SetActive(enable);
            }
        }
    }
}
