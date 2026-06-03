using UnityEngine;

/// <summary>
/// Помечает объект как часть базы игрока. Требует Health.
/// Автоматически регистрируется в BaseManager и сообщает об изменениях/гибели.
/// Добавьте этот компонент на каждый объект-сооружение, у которого есть Health.
/// </summary>
[RequireComponent(typeof(Health))]
public class BasePart : MonoBehaviour
{
    private Health health;
    private BaseManager manager;
    private bool registered;

    public Health Health => health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void Start()
    {
        manager = BaseManager.Instance;
        if (manager == null) manager = FindObjectOfType<BaseManager>();
        if (manager == null)
        {
            Debug.LogWarning($"[BasePart] BaseManager не найден в сцене. Объект '{name}' не учитывается в базе.", this);
            return;
        }

        manager.Register(this);
        registered = true;

        health.OnHealthChanged.AddListener(OnHealthChanged);
        health.OnDied.AddListener(OnDied);
    }

    private void OnHealthChanged(float current, float max)
    {
        if (manager != null) manager.NotifyPartChanged();
    }

    private void OnDied()
    {
        if (manager != null) manager.NotifyPartDestroyed(this);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged.RemoveListener(OnHealthChanged);
            health.OnDied.RemoveListener(OnDied);
        }
        if (registered && manager != null)
        {
            manager.Unregister(this);
        }
    }
}
