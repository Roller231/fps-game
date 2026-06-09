using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Триггер для открытия магазина с подсказкой "Нажмите E"
/// </summary>
public class ShopTrigger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponShop shop;
    [SerializeField] private GameObject promptUI;
    [SerializeField] private Text promptText;
    [SerializeField] private string promptMessage = "Press E to open Shop";

    [Header("Settings")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float triggerRadius = 3f;

    private bool playerInRange = false;

    private void Start()
    {
        if (shop == null) shop = FindObjectOfType<WeaponShop>();
        if (promptUI != null) promptUI.SetActive(false);
        if (promptText != null) promptText.text = promptMessage;
    }

    private void Update()
    {
        CheckPlayerDistance();

        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (shop != null)
            {
                shop.OpenShop();
                if (promptUI != null) promptUI.SetActive(false);
            }
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
    }
}
