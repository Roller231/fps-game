using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Менеджер базы игрока. Собирает все части базы (объекты с Health + BasePart),
/// считает суммарное здоровье базы как процент 0..1, и обрабатывает поражение,
/// когда все части уничтожены.
/// </summary>
public class BaseManager : MonoBehaviour
{
    public static BaseManager Instance { get; private set; }

    [Header("Defeat")]
    [Tooltip("Перезагрузить текущую сцену при уничтожении базы.")]
    [SerializeField] private bool reloadSceneOnDefeat = true;
    [SerializeField] private float defeatDelay = 1.5f;

    [Header("Events")]
    [Tooltip("Передаёт процент здоровья базы (0..1).")]
    public UnityEvent<float> OnBaseHealthChanged;
    public UnityEvent OnBaseDestroyed;

    private readonly List<BasePart> parts = new List<BasePart>();
    private float totalMaxHealth;
    private bool defeated;

    /// <summary>Текущее здоровье базы в долях (0..1).</summary>
    public float HealthPercent { get; private set; } = 1f;
    public int AlivePartCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] != null && parts[i].Health != null && !parts[i].Health.IsDead) n++;
            return n;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Register(BasePart part)
    {
        if (part == null || parts.Contains(part)) return;
        parts.Add(part);
        RecalculateTotals();
    }

    public void Unregister(BasePart part)
    {
        if (parts.Remove(part)) RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        totalMaxHealth = 0f;
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null && parts[i].Health != null)
                totalMaxHealth += parts[i].Health.Max;
        }
        RecalculatePercent();
    }

    /// <summary>Вызывается частями базы при изменении их здоровья.</summary>
    public void NotifyPartChanged()
    {
        RecalculatePercent();
    }

    /// <summary>Вызывается частью базы при её гибели.</summary>
    public void NotifyPartDestroyed(BasePart part)
    {
        RecalculatePercent();
        if (defeated) return;
        if (AlivePartCount <= 0)
        {
            Defeat();
        }
    }

    private void RecalculatePercent()
    {
        if (totalMaxHealth <= 0f)
        {
            HealthPercent = AlivePartCount > 0 ? 1f : 0f;
        }
        else
        {
            float current = 0f;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null && parts[i].Health != null)
                    current += parts[i].Health.Current;
            }
            HealthPercent = Mathf.Clamp01(current / totalMaxHealth);
        }
        OnBaseHealthChanged?.Invoke(HealthPercent);
    }

    /// <summary>Ближайшая живая часть базы относительно позиции.</summary>
    public Health GetClosestAlive(Vector3 fromPosition)
    {
        Health best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < parts.Count; i++)
        {
            var p = parts[i];
            if (p == null || p.Health == null || p.Health.IsDead) continue;
            float sqr = (p.transform.position - fromPosition).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = p.Health;
            }
        }
        return best;
    }

    private void Defeat()
    {
        defeated = true;
        OnBaseDestroyed?.Invoke();
        if (reloadSceneOnDefeat)
        {
            StartCoroutine(ReloadAfterDelay());
        }
    }

    private IEnumerator ReloadAfterDelay()
    {
        yield return new WaitForSeconds(defeatDelay);
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
