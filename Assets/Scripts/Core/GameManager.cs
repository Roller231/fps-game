using UnityEngine;

/// <summary>
/// Центральная точка доступа к игроку и базе.
/// Враги (EnemyAI) используют этот синглтон для выбора цели.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private Transform player;
    [Tooltip("Если Player не задан вручную, будет найден по тегу.")]
    [SerializeField] private string playerTag = "Player";

    [Header("Base")]
    [SerializeField] private BaseManager baseManager;

    [Header("Economy")]
    [SerializeField] private int startingMoney = 500;

    private int currentMoney;

    public Transform Player => player;
    public BaseManager Base => baseManager;
    public int Money => currentMoney;

    private void Awake()
    {
        // Сброс timeScale при старте (на случай если остался 0 после выхода в меню)
        Time.timeScale = 1f;
        
        // Сброс статического флага паузы
        PauseMenu.IsPaused = false;

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go != null) player = go.transform;
        }
        if (baseManager == null) baseManager = FindObjectOfType<BaseManager>();
        
        // Баланс загружается через ProfileService
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Регистрация игрока во время выполнения (например, после спавна).</summary>
    public void SetPlayer(Transform t) => player = t;

    /// <summary>Ближайшая живая часть базы относительно позиции (для наведения врагов).</summary>
    public Health GetClosestAliveBase(Vector3 fromPosition)
    {
        return baseManager != null ? baseManager.GetClosestAlive(fromPosition) : null;
    }

    public bool SpendMoney(int amount)
    {
        if (currentMoney >= amount)
        {
            currentMoney -= amount;
            return true;
        }
        return false;
    }

    public void AddMoney(int amount)
    {
        currentMoney += amount;
    }
}
