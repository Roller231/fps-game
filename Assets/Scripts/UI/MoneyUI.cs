using UnityEngine;
using UnityEngine.UI;

public class MoneyUI : MonoBehaviour
{
    [Header("Main Display")]
    [SerializeField] private Text moneyText;
    [SerializeField] private string moneyPrefix = "$";
    
    [Header("Floating Text")]
    [SerializeField] private GameObject floatingTextObject; // Объект на сцене (не префаб!)
    [SerializeField] private Text floatingText; // Text компонент внутри объекта
    [SerializeField] private Animation floatingAnimation; // Animation компонент
    
    private void Awake()
    {
        if (moneyText == null)
            moneyText = GetComponent<Text>();
    }

    private void OnEnable()
    {
        TrySubscribeToMoneyManager();
        
        // Выключить объект при старте
        if (floatingTextObject != null)
            floatingTextObject.SetActive(false);
    }
    
    private void Start()
    {
        // Повторная попытка подписки на случай если MoneyManager создался позже
        TrySubscribeToMoneyManager();
    }
    
    private void TrySubscribeToMoneyManager()
    {
        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("[MoneyUI] MoneyManager.Instance is null, waiting...");
            return;
        }
        
        // Отписываемся на случай если уже были подписаны
        MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
        MoneyManager.Instance.OnMoneyEarned -= ShowFloatingText;
        
        // Подписываемся заново
        MoneyManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
        MoneyManager.Instance.OnMoneyEarned += ShowFloatingText;
        
        Debug.Log("[MoneyUI] Successfully subscribed to MoneyManager");
        
        // Обновляем отображение текущих денег
        UpdateMoneyDisplay(MoneyManager.Instance.CurrentMoney);
    }

    private void OnDisable()
    {
        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
            MoneyManager.Instance.OnMoneyEarned -= ShowFloatingText;
        }
    }

    private void UpdateMoneyDisplay(int amount)
    {
        if (moneyText != null)
            moneyText.text = "Balance: " + moneyPrefix + amount.ToString("N0");
    }

    private void ShowFloatingText(int amount, Vector3 worldPosition)
    {
        Debug.Log($"[MoneyUI] ShowFloatingText called: amount={amount}, floatingTextObject={floatingTextObject != null}");
        
        if (floatingTextObject == null)
        {
            Debug.LogError("[MoneyUI] floatingTextObject is NULL! Назначи объект в инспекторе.");
            return;
        }

        // Устанавливаем текст награды
        if (floatingText != null)
        {
            floatingText.text = "+" + amount;
            Debug.Log($"[MoneyUI] Text set to: +{amount}");
        }
        else
        {
            Debug.LogWarning("[MoneyUI] floatingText is NULL!");
        }

        // Включаем объект
        floatingTextObject.SetActive(true);
        Debug.Log($"[MoneyUI] floatingTextObject activated: {floatingTextObject.activeSelf}");

        // Запускаем анимацию
        if (floatingAnimation != null)
        {
            floatingAnimation.Stop();
            floatingAnimation.Play();
            Debug.Log("[MoneyUI] Animation played");
        }
        else
        {
            Debug.LogWarning("[MoneyUI] floatingAnimation is NULL!");
        }
    }
}
