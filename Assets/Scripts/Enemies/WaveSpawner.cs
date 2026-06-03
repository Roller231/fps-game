using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Система волн. Спавнит врагов из пула, масштабирует их характеристики с ростом
/// номера волны (через EnemyData.healthPerWave / damagePerWave) и направляет их
/// атаковать базу игрока. Легко расширяется новыми типами врагов: достаточно
/// добавить новый EnemyData в enemyPool (с указанием минимальной волны появления).
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [System.Serializable]
    public class WaveEntry
    {
        public EnemyData data;
        [Tooltip("С какой волны этот тип врага начинает появляться.")]
        public int minWave = 1;
        [Tooltip("Относительный вес при случайном выборе.")]
        public float weight = 1f;
    }

    [Header("Spawn")]
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Запасная цель, если база/игрок ещё не найдены.")]
    [SerializeField] private Transform defendPoint;
    [SerializeField] private WaveEntry[] enemyPool;
    [SerializeField] private float spawnSampleRadius = 4f;
    [Tooltip("Небольшой разброс вокруг спавн-поинта по XZ, чтобы враги не появлялись в одной точке.")]
    [SerializeField] private float spawnJitterRadius = 1.5f;
    [Tooltip("Сколько попыток сделать с разными смещениями, чтобы найти валидную точку на NavMesh.")]
    [SerializeField] private int spawnJitterTries = 6;

    [Header("Wave Timing")]
    [Tooltip("Пауза между волнами.")]
    [SerializeField] private float timeBetweenWaves = 8f;
    [Tooltip("Задержка между спавном отдельных врагов внутри волны.")]
    [SerializeField] private float spawnInterval = 0.4f;
    [Tooltip("Начинать следующую волну сразу, как только текущая зачищена.")]
    [SerializeField] private bool startNextWaveWhenCleared = true;

    [Header("Wave Size")]
    [SerializeField] private int startWaveSize = 5;
    [Tooltip("Сколько врагов добавляется к размеру волны за каждую следующую волну.")]
    [SerializeField] private int enemiesPerWaveGrowth = 1;

    [Header("Boss")]
    [SerializeField] private int bossEveryNWaves = 5;

    [Header("Events")]
    public UnityEvent<int> OnWaveStarted;
    public UnityEvent<int> OnWaveCleared;

    public int CurrentWave => currentWave;
    public int AliveCount
    {
        get
        {
            alive.RemoveAll(e => e == null);
            return alive.Count;
        }
    }

    private int currentWave = 0;
    private float nextWaveTime;
    private bool spawning;
    private bool waveActive;
    private readonly List<EnemyAI> alive = new List<EnemyAI>();

    private void Start()
    {
        ScheduleNextWave();
    }

    private void Update()
    {
        alive.RemoveAll(e => e == null);

        if (spawning) return;

        if (waveActive)
        {
            if (startNextWaveWhenCleared && alive.Count == 0)
            {
                OnWaveCleared?.Invoke(currentWave);
                waveActive = false;
                ScheduleNextWave();
            }
            return;
        }

        if (Time.time >= nextWaveTime)
        {
            StartCoroutine(SpawnWaveRoutine());
        }
    }

    private void ScheduleNextWave()
    {
        nextWaveTime = Time.time + timeBetweenWaves;
    }

    private IEnumerator SpawnWaveRoutine()
    {
        spawning = true;
        waveActive = true;
        currentWave++;
        OnWaveStarted?.Invoke(currentWave);

        int waveSize = startWaveSize + (currentWave - 1) * enemiesPerWaveGrowth;
        for (int i = 0; i < waveSize; i++)
        {
            bool lastOfWave = i == waveSize - 1;
            var data = PickEnemyData(currentWave, lastOfWave);
            SpawnEnemy(data, GetSpawnPosition());
            if (spawnInterval > 0f) yield return new WaitForSeconds(spawnInterval);
        }

        spawning = false;
    }

    private Vector3 GetSpawnPosition()
    {
        Transform spawn = (spawnPoints != null && spawnPoints.Length > 0)
            ? spawnPoints[Random.Range(0, spawnPoints.Length)]
            : transform;
        Vector3 basePos = spawn.position;

        // Try several jittered positions around the spawn point
        for (int i = 0; i < Mathf.Max(1, spawnJitterTries); i++)
        {
            Vector2 rnd = spawnJitterRadius > 0f ? Random.insideUnitCircle * spawnJitterRadius : Vector2.zero;
            Vector3 pos = basePos + new Vector3(rnd.x, 0f, rnd.y);
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, spawnSampleRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }
        }

        // Fallback: original point sampled
        if (NavMesh.SamplePosition(basePos, out NavMeshHit fallback, spawnSampleRadius, NavMesh.AllAreas))
        {
            return fallback.position;
        }
        return basePos;
    }

    private EnemyData PickEnemyData(int wave, bool lastOfWave)
    {
        if (enemyPool == null || enemyPool.Length == 0) return null;

        // Босс в конце каждой N-й волны, если такой тип доступен.
        if (lastOfWave && bossEveryNWaves > 0 && wave % bossEveryNWaves == 0)
        {
            foreach (var e in enemyPool)
            {
                if (e != null && e.data != null && e.data.archetype == EnemyArchetype.Boss && wave >= e.minWave)
                    return e.data;
            }
        }

        // Взвешенный случайный выбор среди доступных на этой волне.
        float totalWeight = 0f;
        for (int i = 0; i < enemyPool.Length; i++)
        {
            var e = enemyPool[i];
            if (e == null || e.data == null || wave < e.minWave) continue;
            if (e.data.archetype == EnemyArchetype.Boss) continue; // боссов спавним только как lastOfWave
            totalWeight += Mathf.Max(0f, e.weight);
        }
        if (totalWeight <= 0f) return null;

        float roll = Random.value * totalWeight;
        for (int i = 0; i < enemyPool.Length; i++)
        {
            var e = enemyPool[i];
            if (e == null || e.data == null || wave < e.minWave) continue;
            if (e.data.archetype == EnemyArchetype.Boss) continue;
            roll -= Mathf.Max(0f, e.weight);
            if (roll <= 0f) return e.data;
        }
        return null;
    }

    private void SpawnEnemy(EnemyData data, Vector3 pos)
    {
        if (data == null || data.prefab == null) return;
        var go = Instantiate(data.prefab, pos, Quaternion.identity);
        var ai = go.GetComponent<EnemyAI>();
        if (ai == null) ai = go.AddComponent<EnemyAI>();
        ai.ApplyData(data, currentWave);
        if (defendPoint != null) ai.SetTarget(defendPoint);
        alive.Add(ai);
    }
}
