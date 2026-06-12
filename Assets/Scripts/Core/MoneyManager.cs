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
        
        LoadMoney();
    }

    private void LoadMoney()
    {
        currentMoney = PlayerPrefs.GetInt("PlayerMoney", 0);
        OnMoneyChanged?.Invoke(currentMoney);
    }

    public void AddMoney(int amount, Vector3 worldPosition = default)
    {
        if (amount <= 0) return;
        currentMoney += amount;
        PlayerPrefs.SetInt("PlayerMoney", currentMoney);
        Debug.Log($"[MoneyManager] AddMoney: +{amount}, total={currentMoney}, subscribers={OnMoneyEarned?.GetInvocationList().Length ?? 0}");
        OnMoneyChanged?.Invoke(currentMoney);
        OnMoneyEarned?.Invoke(amount, worldPosition);
    }

    public bool SpendMoney(int amount)
    {
        if (amount <= 0 || currentMoney < amount) return false;
        currentMoney -= amount;
        PlayerPrefs.SetInt("PlayerMoney", currentMoney);
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
        PlayerPrefs.SetInt("PlayerMoney", 0);
        OnMoneyChanged?.Invoke(currentMoney);
    }
}
