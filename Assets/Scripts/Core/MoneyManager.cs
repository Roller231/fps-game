using UnityEngine;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    private int currentMoney;
    
    public int CurrentMoney => currentMoney;
    public System.Action<int> OnMoneyChanged;
    public System.Action<int, Vector3> OnMoneyEarned;

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

    /// <summary>
    /// Устанавливает баланс (вызывается ProfileService при загрузке)
    /// </summary>
    public void SetMoney(int amount)
    {
        currentMoney = amount;
        OnMoneyChanged?.Invoke(currentMoney);
        Debug.Log($"[MoneyManager] Money set to {currentMoney}");
    }

    public void AddMoney(int amount, Vector3 worldPosition = default)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        Debug.Log($"[MoneyManager] AddMoney: +{amount}, total={currentMoney}");
        OnMoneyChanged?.Invoke(currentMoney);
        OnMoneyEarned?.Invoke(amount, worldPosition);
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0 || currentMoney < amount) return false;
        currentMoney -= amount;
        OnMoneyChanged?.Invoke(currentMoney);
        return true;
    }

    public bool CanAfford(int amount)
    {
        return currentMoney >= amount;
    }

    public void ResetMoney()
    {
        currentMoney = 0;
        OnMoneyChanged?.Invoke(currentMoney);
    }
}
