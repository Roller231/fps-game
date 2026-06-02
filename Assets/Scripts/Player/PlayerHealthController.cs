using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Health))]
public class PlayerHealthController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private GameObject weaponRoot;
    [SerializeField] private Transform spawnPoint;

    [Header("Damage FX")]
    [SerializeField] private Image damageVignette;
    [SerializeField] private float vignetteDuration = 0.35f;
    [SerializeField] private float vignetteMaxAlpha = 0.4f;
    [SerializeField] private float cameraShakeAmplitude = 0.25f;
    [SerializeField] private float cameraShakeDuration = 0.2f;

    [Header("Death")]
    [SerializeField] private GameObject freeLookCamera; // enable on death
    [SerializeField] private float respawnDelay = 3f;

    private Health health;
    private Coroutine vignetteRoutine;
    private Coroutine shakeRoutine;
    private Vector3 camLocalPosDefault;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (characterController == null) characterController = GetComponent<CharacterController>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (playerCamera == null && movement != null) playerCamera = movement.GetComponentInChildren<Camera>();
        if (playerCamera != null) camLocalPosDefault = playerCamera.transform.localPosition;
        health.OnHealthChanged.AddListener(OnHealthChanged);
        health.OnDied.AddListener(OnDied);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged.RemoveListener(OnHealthChanged);
            health.OnDied.RemoveListener(OnDied);
        }
    }

    private void OnHealthChanged(float current, float max)
    {
        if (current <= 0f) return;
        PlayVignette();
        PlayShake();
    }

    private void OnDied()
    {
        if (movement != null) movement.enabled = false;
        if (characterController != null) characterController.enabled = false;
        if (weaponRoot != null) weaponRoot.SetActive(false);
        if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        if (freeLookCamera != null) freeLookCamera.SetActive(true);

        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        if (health != null) health.ResetToFull();
        if (characterController != null && spawnPoint != null)
        {
            characterController.enabled = false;
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
            characterController.enabled = true;
        }
        if (freeLookCamera != null) freeLookCamera.SetActive(false);
        if (playerCamera != null)
        {
            playerCamera.gameObject.SetActive(true);
            playerCamera.transform.localPosition = camLocalPosDefault;
        }
        if (weaponRoot != null) weaponRoot.SetActive(true);
        if (movement != null) movement.enabled = true;
    }

    private void PlayVignette()
    {
        if (damageVignette == null) return;
        if (vignetteRoutine != null) StopCoroutine(vignetteRoutine);
        vignetteRoutine = StartCoroutine(VignettePulse());
    }

    private IEnumerator VignettePulse()
    {
        float t = 0f;
        Color c = damageVignette.color;
        while (t < vignetteDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(vignetteMaxAlpha, 0f, t / vignetteDuration);
            damageVignette.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        damageVignette.color = new Color(c.r, c.g, c.b, 0f);
    }

    private void PlayShake()
    {
        if (playerCamera == null) return;
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine());
    }

    private IEnumerator ShakeRoutine()
    {
        float t = 0f;
        while (t < cameraShakeDuration)
        {
            t += Time.deltaTime;
            Vector3 offset = Random.insideUnitSphere * cameraShakeAmplitude;
            playerCamera.transform.localPosition = camLocalPosDefault + offset;
            yield return null;
        }
        playerCamera.transform.localPosition = camLocalPosDefault;
    }
}
