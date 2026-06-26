using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles interaction between settings UI controls and the GameSettings singleton.
/// </summary>
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private Text mouseSensitivityLabel;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Text masterVolumeLabel;
    [SerializeField] private Toggle invertYToggle;
    [SerializeField] private Toggle hitmarkerSoundToggle;

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;
    }

    private void RegisterUiEvents()
    {
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (invertYToggle != null)
            invertYToggle.onValueChanged.AddListener(OnInvertYChanged);
        if (hitmarkerSoundToggle != null)
            hitmarkerSoundToggle.onValueChanged.AddListener(OnHitmarkerToggleChanged);
    }

    private void UnregisterUiEvents()
    {
        if (mouseSensitivitySlider != null)
            mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
        if (invertYToggle != null)
            invertYToggle.onValueChanged.RemoveListener(OnInvertYChanged);
        if (hitmarkerSoundToggle != null)
            hitmarkerSoundToggle.onValueChanged.RemoveListener(OnHitmarkerToggleChanged);
    }

    private void OnEnable()
    {
        Subscribe();
        RegisterUiEvents();
        RefreshUI();
    }

    private void OnDisable()
    {
        UnregisterUiEvents();
        Unsubscribe();
    }

    public void Show()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
        RefreshUI();
    }

    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void OnMouseSensitivityChanged(float value)
    {
        GameSettings.Instance?.SetMouseSensitivity(value);
        UpdateMouseSensitivityLabel(value);
    }

    public void OnMasterVolumeChanged(float value)
    {
        GameSettings.Instance?.SetMasterVolume(value);
        UpdateMasterVolumeLabel(value);
    }

    public void OnInvertYChanged(bool value)
    {
        GameSettings.Instance?.SetInvertYAxis(value);
    }

    public void OnHitmarkerToggleChanged(bool value)
    {
        GameSettings.Instance?.SetHitmarkerSoundsEnabled(value);
    }

    public void OnResetButtonPressed()
    {
        GameSettings.Instance?.ResetToDefaults();
        RefreshUI();
    }

    private void Subscribe()
    {
        if (GameSettings.Instance == null) return;
        GameSettings.Instance.OnMouseSensitivityChanged += HandleMouseSensitivityEvent;
        GameSettings.Instance.OnMasterVolumeChanged += HandleMasterVolumeEvent;
        GameSettings.Instance.OnInvertYChanged += HandleInvertYEvent;
        GameSettings.Instance.OnHitmarkerSoundsChanged += HandleHitmarkerEvent;
    }

    private void Unsubscribe()
    {
        if (GameSettings.Instance == null) return;
        GameSettings.Instance.OnMouseSensitivityChanged -= HandleMouseSensitivityEvent;
        GameSettings.Instance.OnMasterVolumeChanged -= HandleMasterVolumeEvent;
        GameSettings.Instance.OnInvertYChanged -= HandleInvertYEvent;
        GameSettings.Instance.OnHitmarkerSoundsChanged -= HandleHitmarkerEvent;
    }

    private void RefreshUI()
    {
        var settings = GameSettings.Instance;
        if (settings == null)
            return;

        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.minValue = settings.MinMouseSensitivity;
            mouseSensitivitySlider.maxValue = settings.MaxMouseSensitivity;
            mouseSensitivitySlider.value = settings.MouseSensitivity;
            UpdateMouseSensitivityLabel(settings.MouseSensitivity);
        }

        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = settings.MasterVolume;
            UpdateMasterVolumeLabel(settings.MasterVolume);
        }

        if (invertYToggle != null)
            invertYToggle.isOn = settings.InvertYAxis;

        if (hitmarkerSoundToggle != null)
            hitmarkerSoundToggle.isOn = settings.HitmarkerSoundsEnabled;
    }

    private void UpdateMouseSensitivityLabel(float value)
    {
        if (mouseSensitivityLabel != null)
            mouseSensitivityLabel.text = $"{value:F1}";
    }

    private void UpdateMasterVolumeLabel(float value)
    {
        if (masterVolumeLabel != null)
            masterVolumeLabel.text = $"{Mathf.RoundToInt(value * 100f)}%";
    }

    private void HandleMouseSensitivityEvent(float value)
    {
        if (mouseSensitivitySlider != null && !Mathf.Approximately(mouseSensitivitySlider.value, value))
            mouseSensitivitySlider.value = value;
        UpdateMouseSensitivityLabel(value);
    }

    private void HandleMasterVolumeEvent(float value)
    {
        if (masterVolumeSlider != null && !Mathf.Approximately(masterVolumeSlider.value, value))
            masterVolumeSlider.value = value;
        UpdateMasterVolumeLabel(value);
    }

    private void HandleInvertYEvent(bool value)
    {
        if (invertYToggle != null && invertYToggle.isOn != value)
            invertYToggle.isOn = value;
    }

    private void HandleHitmarkerEvent(bool value)
    {
        if (hitmarkerSoundToggle != null && hitmarkerSoundToggle.isOn != value)
            hitmarkerSoundToggle.isOn = value;
    }
}
