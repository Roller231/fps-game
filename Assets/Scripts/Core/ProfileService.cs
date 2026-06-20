using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Синхронизирует профиль игрока с бэкендом: баланс, оружие, статистику
/// Заменяет PlayerPrefs для всех игровых данных
/// </summary>
public class ProfileService : MonoBehaviour
{
    [Header("Backend")]
    [SerializeField] private string backendBaseUrl = "http://localhost:8000";

    [Header("Auto-save")]
    [SerializeField] private float autoSaveInterval = 30f;

    [Header("UI Bindings")]
    [SerializeField] private Image profileAvatarImage;
    [SerializeField] private Text profileDisplayNameText;
    [SerializeField] private Text shopBalanceText;

    public static ProfileService Instance { get; private set; }

    private string currentToken;
    private ProfileData cachedProfile;
    private float lastSaveTime;
    private AuthManager authManager;
    private bool profileApplied;
    private bool waitingForManagers;
    private Coroutine avatarLoadRoutine;
    private Sprite avatarSpriteCache;
    private Texture2D avatarTextureCache;
    private string lastAvatarUrl;

    public event Action<ProfileData> OnProfileLoaded;
    public event Action OnProfileSaved;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        authManager = FindObjectOfType<AuthManager>();
        if (authManager != null)
        {
            authManager.OnTokenValidated += HandleTokenValidated;
        }

        if (!string.IsNullOrEmpty(AuthManager.CurrentToken))
        {
            Debug.Log("[ProfileService] Using existing token from AuthManager");
            Initialize(AuthManager.CurrentToken);
        }
        else
        {
            Debug.Log("[ProfileService] Waiting for AuthManager to validate token...");
        }
    }

    private void EnsureMoneyListener()
    {
        if (MoneyManager.Instance == null)
            return;

        MoneyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
        MoneyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        HandleMoneyChanged(MoneyManager.Instance.CurrentMoney);
    }

    private void HandleMoneyChanged(int amount)
    {
        if (cachedProfile != null)
        {
            cachedProfile.balance = amount;
        }

        if (shopBalanceText != null)
        {
            shopBalanceText.text = $"Balance: {amount}";
        }
    }

    private void UpdateUiBindings()
    {
        if (cachedProfile == null)
            return;

        if (profileDisplayNameText != null)
        {
            string displayName = string.IsNullOrWhiteSpace(cachedProfile.display_name)
                ? cachedProfile.username
                : cachedProfile.display_name;
            profileDisplayNameText.text = displayName;
        }

        if (shopBalanceText != null)
        {
            shopBalanceText.text = $"Balance: {cachedProfile.balance}";
        }

        UpdateAvatarImage(cachedProfile.avatar_url);
    }

    private void UpdateAvatarImage(string url)
    {
        if (profileAvatarImage == null)
            return;

        if (avatarLoadRoutine != null)
        {
            StopCoroutine(avatarLoadRoutine);
            avatarLoadRoutine = null;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            lastAvatarUrl = null;
            ClearAvatarImage();
            return;
        }

        if (url == lastAvatarUrl && profileAvatarImage.sprite != null)
            return;

        avatarLoadRoutine = StartCoroutine(DownloadAvatarCoroutine(url));
    }

    private IEnumerator DownloadAvatarCoroutine(string url)
    {
        lastAvatarUrl = url;
        using (var request = UnityWebRequestTexture.GetTexture(url))
        {
            request.timeout = 10;
            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityWebRequest.Result.Success)
#else
            if (request.isNetworkError || request.isHttpError)
#endif
            {
                Debug.LogWarning($"[ProfileService] Failed to load avatar: {request.error}");
                ClearAvatarImage();
                avatarLoadRoutine = null;
                yield break;
            }

            var texture = DownloadHandlerTexture.GetContent(request);
            if (texture == null)
            {
                ClearAvatarImage();
                avatarLoadRoutine = null;
                yield break;
            }

            if (avatarSpriteCache != null)
            {
                Destroy(avatarSpriteCache);
            }
            if (avatarTextureCache != null)
            {
                Destroy(avatarTextureCache);
            }

            avatarTextureCache = texture;
            avatarSpriteCache = Sprite.Create(avatarTextureCache, new Rect(0, 0, avatarTextureCache.width, avatarTextureCache.height), new Vector2(0.5f, 0.5f));
            profileAvatarImage.sprite = avatarSpriteCache;
            profileAvatarImage.enabled = true;
        }

        avatarLoadRoutine = null;
    }

    private void ClearAvatarImage()
    {
        if (profileAvatarImage != null)
        {
            profileAvatarImage.sprite = null;
            profileAvatarImage.enabled = false;
        }

        if (avatarSpriteCache != null)
        {
            Destroy(avatarSpriteCache);
            avatarSpriteCache = null;
        }

        if (avatarTextureCache != null)
        {
            Destroy(avatarTextureCache);
            avatarTextureCache = null;
        }
    }

    private void OnDestroy()
    {
        if (authManager != null)
        {
            authManager.OnTokenValidated -= HandleTokenValidated;
        }

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }

        if (avatarLoadRoutine != null)
        {
            StopCoroutine(avatarLoadRoutine);
            avatarLoadRoutine = null;
        }
        ClearAvatarImage();
    }

    private void HandleTokenValidated(string token)
    {
        Debug.Log("[ProfileService] Received token from AuthManager, initializing profile sync");
        Initialize(token);
    }

    private void Update()
    {
        if (Time.time - lastSaveTime > autoSaveInterval && cachedProfile != null)
        {
            SaveProfile();
        }

        if (cachedProfile != null)
        {
            TryApplyProfile();
        }
    }

    public void Initialize(string token)
    {
        currentToken = token;
        profileApplied = false;
        waitingForManagers = false;
        StartCoroutine(LoadProfileCoroutine());
    }

    private IEnumerator LoadProfileCoroutine()
    {
        string url = $"{backendBaseUrl}/api/profile?token={UnityWebRequest.EscapeURL(currentToken)}";
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
                Debug.LogError($"[ProfileService] Failed to load profile: {request.error}");
                yield break;
            }

            string json = request.downloadHandler.text;
            cachedProfile = JsonUtility.FromJson<ProfileData>(json);

            if (cachedProfile != null)
            {
                Debug.Log($"[ProfileService] Profile loaded for {cachedProfile.username} | display={cachedProfile.display_name} | balance={cachedProfile.balance} | weapon={cachedProfile.equipped_weapon}");
                if (cachedProfile.weapons != null)
                {
                    foreach (var weapon in cachedProfile.weapons)
                    {
                        Debug.Log($"[ProfileService]   weapon={weapon.weapon_name}, mag={weapon.magazine_ammo}, reserve={weapon.reserve_ammo}");
                    }
                }
                if (cachedProfile.stats != null)
                {
                    Debug.Log($"[ProfileService]   stats: kills={cachedProfile.stats.total_kills}, money_earned={cachedProfile.stats.total_money_earned}, max_wave={cachedProfile.stats.max_wave}");
                }
            }

            TryApplyProfile();
            OnProfileLoaded?.Invoke(cachedProfile);
            
            Debug.Log($"[ProfileService] Profile applied at {Time.time:F1}s");
        }
    }

    private bool TryApplyProfile()
    {
        if (cachedProfile == null)
            return false;

        if (profileApplied)
            return true;

        if (MoneyManager.Instance == null || WeaponInventory.Instance == null)
        {
            // Подождём пока синглтоны создадутся
            if (!waitingForManagers)
            {
                Debug.Log("[ProfileService] Cannot apply yet: waiting for MoneyManager/WeaponInventory");
                waitingForManagers = true;
            }
            return false;
        }

        ApplyProfileToGame();
        waitingForManagers = false;
        profileApplied = true;
        return true;
    }

    public void ApplyProfileToGame()
    {
        if (cachedProfile == null) return;

        Debug.Log("[ProfileService] Applying profile data to in-scene systems");
        // Применяем баланс
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.SetMoney(cachedProfile.balance);
            EnsureMoneyListener();
        }

        // Применяем оружие
        if (WeaponInventory.Instance != null)
        {
            WeaponInventory.Instance.LoadFromProfile(cachedProfile);
        }

        UpdateUiBindings();
    }

    public void SaveProfile()
    {
        if (cachedProfile == null || string.IsNullOrEmpty(currentToken))
        {
            Debug.LogWarning("[ProfileService] Cannot save: no profile loaded");
            return;
        }

        CollectGameData();
        StartCoroutine(SaveProfileCoroutine());
    }

    private void CollectGameData()
    {
        // Собираем текущие данные из игры
        if (MoneyManager.Instance != null)
        {
            cachedProfile.balance = MoneyManager.Instance.CurrentMoney;
        }

        if (WeaponInventory.Instance != null)
        {
            WeaponInventory.Instance.SaveToProfile(cachedProfile);
        }

        // Можно добавить сбор статистики из других менеджеров

        if (cachedProfile != null)
        {
            Debug.Log($"[ProfileService] Preparing save: balance={cachedProfile.balance}, equipped={cachedProfile.equipped_weapon}");
            if (cachedProfile.weapons != null)
            {
                foreach (var weapon in cachedProfile.weapons)
                {
                    Debug.Log($"[ProfileService]   weapon={weapon.weapon_name}, mag={weapon.magazine_ammo}, reserve={weapon.reserve_ammo}");
                }
            }
            if (cachedProfile.stats != null)
            {
                Debug.Log($"[ProfileService]   stats: kills={cachedProfile.stats.total_kills}, money_earned={cachedProfile.stats.total_money_earned}, max_wave={cachedProfile.stats.max_wave}");
            }
        }
    }

    private IEnumerator SaveProfileCoroutine()
    {
        string url = $"{backendBaseUrl}/api/profile?token={UnityWebRequest.EscapeURL(currentToken)}";
        string json = JsonUtility.ToJson(cachedProfile);

        using (var request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
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
                Debug.LogError($"[ProfileService] Failed to save profile: {request.error}");
                yield break;
            }

            lastSaveTime = Time.time;
            OnProfileSaved?.Invoke();
            Debug.Log("[ProfileService] Profile saved successfully");
        }
    }

    private void OnApplicationQuit()
    {
        if (cachedProfile != null)
        {
            CollectGameData();
            // Синхронный save перед выходом
            StartCoroutine(SaveProfileCoroutine());
        }
    }
}

[Serializable]
public class ProfileData
{
    public string username;
    public string display_name;
    public string avatar_url;
    public int balance;
    public string equipped_weapon;
    public WeaponDataItem[] weapons;
    public StatsData stats;
}

[Serializable]
public class WeaponDataItem
{
    public string weapon_name;
    public int reserve_ammo;
    public int magazine_ammo;
}

[Serializable]
public class StatsData
{
    public int total_kills;
    public int total_money_earned;
    public int max_wave;
    public int max_survival_time;
    public int total_playtime;
}
