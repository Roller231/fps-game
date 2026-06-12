using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Триггер для взаимодействия с ящиком припасов
/// </summary>
public class SupplyBoxTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SupplyBox supplyBox;
    [SerializeField] private GameObject promptUI;
    [SerializeField] private Text promptText;

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float triggerRadius = 3f;

    [Header("Messages")]
    [SerializeField] private string healthPrompt = "Press E to restore Health (${0})";
    [SerializeField] private string ammoPrompt = "Press F to refill Ammo (${0})";
    [SerializeField] private string fullRestockPrompt = "Press Q to Full Restock (${0})";
    [SerializeField] private string notEnoughMoneyMessage = "Not enough money! Need ${0}";
    [SerializeField] private string healthFullMessage = "Health already full!";

    private bool playerInRange = false;
    private float messageDisplayTime = 0f;
    private string currentMessage = "";

    private void Start()
    {
        if (supplyBox == null) supplyBox = GetComponent<SupplyBox>();
        if (promptUI != null) promptUI.SetActive(false);
    }

    private void Update()
    {
        CheckPlayerDistance();

        if (playerInRange)
        {
            UpdatePromptText();

            // E - купить HP
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryBuyHealth();
            }

            // F - купить боезапас
            if (Input.GetKeyDown(KeyCode.F))
            {
                TryBuyAmmo();
            }

            // Q - купить всё вместе
            if (Input.GetKeyDown(KeyCode.Q))
            {
                TryBuyFullRestock();
            }
        }

        // Показать временное сообщение
        if (Time.time < messageDisplayTime && promptText != null)
        {
            promptText.text = currentMessage;
        }
    }

    private void CheckPlayerDistance()
    {
        if (GameManager.Instance == null || GameManager.Instance.Player == null)
        {
            SetPlayerInRange(false);
            return;
        }

        float distance = Vector3.Distance(transform.position, GameManager.Instance.Player.position);
        SetPlayerInRange(distance <= triggerRadius);
    }

    private void SetPlayerInRange(bool inRange)
    {
        if (playerInRange == inRange) return;

        playerInRange = inRange;
        if (promptUI != null) promptUI.SetActive(playerInRange);
    }

    private void UpdatePromptText()
    {
        if (promptText == null || supplyBox == null) return;
        if (Time.time < messageDisplayTime) return; // Показываем временное сообщение

        string text = "";

        // Всегда показываем опцию HP, но помечаем если полное
        if (supplyBox.IsHealthFull())
        {
            text += $"Health FULL (can't restore)\n";
        }
        else
        {
            text += string.Format(healthPrompt, supplyBox.HealthPrice) + "\n";
        }
        
        text += string.Format(ammoPrompt, supplyBox.AmmoPrice) + "\n";
        text += string.Format(fullRestockPrompt, supplyBox.FullRestockPrice);

        promptText.text = text;
    }

    private void TryBuyHealth()
    {
        if (supplyBox == null) return;

        if (supplyBox.IsHealthFull())
        {
            ShowMessage(healthFullMessage, 2f);
            return;
        }

        if (!supplyBox.CanAffordHealth())
        {
            ShowMessage(string.Format(notEnoughMoneyMessage, supplyBox.HealthPrice), 2f);
            return;
        }

        if (supplyBox.BuyHealth())
        {
            ShowMessage("Health restored!", 1.5f);
        }
    }

    private void TryBuyAmmo()
    {
        if (supplyBox == null) return;

        if (!supplyBox.CanAffordAmmo())
        {
            ShowMessage(string.Format(notEnoughMoneyMessage, supplyBox.AmmoPrice), 2f);
            return;
        }

        if (supplyBox.BuyAmmo())
        {
            ShowMessage("Ammo refilled!", 1.5f);
        }
    }

    private void TryBuyFullRestock()
    {
        if (supplyBox == null) return;

        if (!supplyBox.CanAffordFullRestock())
        {
            ShowMessage(string.Format(notEnoughMoneyMessage, supplyBox.FullRestockPrice), 2f);
            return;
        }

        if (supplyBox.BuyFullRestock())
        {
            ShowMessage("Full restock complete!", 1.5f);
        }
    }

    private void ShowMessage(string message, float duration)
    {
        currentMessage = message;
        messageDisplayTime = Time.time + duration;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
