using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Отображает HP цели, в которую смотрит игрок
/// </summary>
public class TargetHealthUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject targetPanel;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image healthBackground;
    [SerializeField] private Text targetNameText;
    [SerializeField] private Text healthText;

    [Header("Settings")]
    [SerializeField] private float raycastDistance = 1000f;
    [SerializeField] private LayerMask targetMask = ~0;
    [SerializeField] private float healthBarSmoothSpeed = 5f;
    [SerializeField] private float panelFadeSpeed = 5f;

    private Health currentTarget;
    private float healthTarget;
    private float panelAlpha = 0f;
    private CanvasGroup canvasGroup;

    private void Start()
    {
        if (targetPanel != null)
        {
            targetPanel.SetActive(true); // Оставляем активным, но невидимым
            canvasGroup = targetPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = targetPanel.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        CheckTarget();
        UpdatePanelAlpha();
    }

    private void UpdatePanelAlpha()
    {
        if (canvasGroup == null) return;

        // Плавно переходим к целевой alpha
        canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, panelAlpha, Time.deltaTime * panelFadeSpeed);
    }

    private void CheckTarget()
    {
        // Проверяем что текущая цель не была удалена
        if (currentTarget != null && currentTarget.gameObject == null)
        {
            currentTarget = null;
            UpdateTargetDisplay();
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        // Raycast из центра камеры
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Health newTarget = null;

        if (Physics.Raycast(ray, out RaycastHit hit, raycastDistance, targetMask))
        {
            newTarget = hit.collider.GetComponent<Health>();
        }

        // Если цель изменилась
        if (newTarget != currentTarget)
        {
            currentTarget = newTarget;
            UpdateTargetDisplay();
        }

        // Обновляем HP текущей цели каждый кадр
        if (currentTarget != null)
        {
            UpdateHealthDisplay();
        }
    }

    private void UpdateTargetDisplay()
    {
        // Отписываемся от старой цели
        if (currentTarget != null)
        {
            currentTarget.OnDied.RemoveListener(OnTargetDied);
        }

        if (currentTarget == null)
        {
            // Плавно скрываем панель
            panelAlpha = 0f;
            return;
        }

        // Подписываемся на смерть новой цели
        currentTarget.OnDied.AddListener(OnTargetDied);

        // Плавно показываем панель
        panelAlpha = 1f;

        // Показываем имя цели
        if (targetNameText != null)
        {
            // Пытаемся найти имя через EnemyAI или BasePart
            string targetName = "Unknown";
            
            var enemyAI = currentTarget.GetComponent<EnemyAI>();
            if (enemyAI != null && enemyAI.Data != null)
            {
                targetName = enemyAI.Data.enemyName;
            }
            else
            {
                var basePart = currentTarget.GetComponent<BasePart>();
                if (basePart != null)
                {
                    targetName = "Base: " + basePart.gameObject.name;
                }
                else
                {
                    targetName = currentTarget.gameObject.name;
                }
            }

            targetNameText.text = targetName;
        }

        // Инициализируем целевое значение для плавной анимации
        healthTarget = currentTarget.Current / currentTarget.Max;
    }

    private void UpdateHealthDisplay()
    {
        if (currentTarget == null) return;

        // Обновляем текст HP
        if (healthText != null)
        {
            healthText.text = $"{currentTarget.Current:F0} / {currentTarget.Max:F0}";
        }

        // Обновляем заполнение бара
        float fillAmount = currentTarget.Current / currentTarget.Max;

        if (healthFill != null)
        {
            healthFill.fillAmount = fillAmount;
        }

        if (healthBackground != null)
        {
            // Плавная анимация красного фона (как у игрока)
            healthTarget = Mathf.Lerp(healthTarget, fillAmount, Time.deltaTime * healthBarSmoothSpeed);
            healthBackground.fillAmount = healthTarget;
        }
    }

    private void OnTargetDied()
    {
        // Цель умерла - плавно скрываем панель
        currentTarget = null;
        panelAlpha = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Показываем raycast линию в редакторе
        Gizmos.color = Color.red;
        Gizmos.DrawRay(cam.transform.position, cam.transform.forward * raycastDistance);
    }
}
