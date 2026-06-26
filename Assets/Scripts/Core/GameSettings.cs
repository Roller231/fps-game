using System;
using UnityEngine;

/// <summary>
/// Holds player-adjustable settings (mouse sensitivity, audio, etc.) and persists them via PlayerPrefs.
/// Automatically bootstraps itself before the first scene loads.
/// </summary>
public class GameSettings : MonoBehaviour
{
    private const string MouseSensitivityKey = "settings_mouse_sensitivity";
    private const string MasterVolumeKey = "settings_master_volume";
    private const string InvertYKey = "settings_invert_y";
    private const string HitmarkerSoundKey = "settings_hitmarker_sound";

    [Header("Default Values")]
    [SerializeField] private float defaultMouseSensitivity = 2f;
    [SerializeField] private float minMouseSensitivity = 0.5f;
    [SerializeField] private float maxMouseSensitivity = 6f;
    [SerializeField] private float defaultMasterVolume = 0.8f;

    public static GameSettings Instance { get; private set; }

    public event Action<float> OnMouseSensitivityChanged;
    public event Action<float> OnMasterVolumeChanged;
    public event Action<bool> OnInvertYChanged;
    public event Action<bool> OnHitmarkerSoundsChanged;

    public float MouseSensitivity { get; private set; }
    public float MasterVolume { get; private set; }
    public bool InvertYAxis { get; private set; }
    public bool HitmarkerSoundsEnabled { get; private set; } = true;

    public float MinMouseSensitivity => minMouseSensitivity;
    public float MaxMouseSensitivity => maxMouseSensitivity;
    public float DefaultMouseSensitivity => defaultMouseSensitivity;
    public float DefaultMasterVolume => defaultMasterVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (Instance == null)
        {
            var go = new GameObject("GameSettings");
            go.AddComponent<GameSettings>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    private void LoadSettings()
    {
        MouseSensitivity = PlayerPrefs.GetFloat(MouseSensitivityKey, defaultMouseSensitivity);
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume);
        InvertYAxis = PlayerPrefs.GetInt(InvertYKey, 0) == 1;
        HitmarkerSoundsEnabled = PlayerPrefs.GetInt(HitmarkerSoundKey, 1) == 1;

        ApplyMasterVolume();
    }

    public void SetMouseSensitivity(float value)
    {
        value = Mathf.Clamp(value, minMouseSensitivity, maxMouseSensitivity);
        if (Mathf.Approximately(MouseSensitivity, value)) return;

        MouseSensitivity = value;
        PlayerPrefs.SetFloat(MouseSensitivityKey, value);
        PlayerPrefs.Save();
        OnMouseSensitivityChanged?.Invoke(value);
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);
        if (Mathf.Approximately(MasterVolume, value)) return;

        MasterVolume = value;
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
        PlayerPrefs.Save();
        ApplyMasterVolume();
        OnMasterVolumeChanged?.Invoke(value);
    }

    public void SetInvertYAxis(bool enabled)
    {
        if (InvertYAxis == enabled) return;

        InvertYAxis = enabled;
        PlayerPrefs.SetInt(InvertYKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        OnInvertYChanged?.Invoke(enabled);
    }

    public void SetHitmarkerSoundsEnabled(bool enabled)
    {
        if (HitmarkerSoundsEnabled == enabled) return;

        HitmarkerSoundsEnabled = enabled;
        PlayerPrefs.SetInt(HitmarkerSoundKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        OnHitmarkerSoundsChanged?.Invoke(enabled);
    }

    private void ApplyMasterVolume()
    {
        AudioListener.volume = MasterVolume;
    }

    public void ResetToDefaults()
    {
        SetMouseSensitivity(defaultMouseSensitivity);
        SetMasterVolume(defaultMasterVolume);
        SetInvertYAxis(false);
        SetHitmarkerSoundsEnabled(true);
    }
}
