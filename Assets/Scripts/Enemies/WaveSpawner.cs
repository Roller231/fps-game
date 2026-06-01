using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform defendPoint;
    [SerializeField] private EnemyData[] enemyPool;
    [SerializeField] private float timeBetweenWaves = 8f;
    [SerializeField] private int startWaveSize = 5;
    [SerializeField] private float bossEveryNWaves = 5;

    private int currentWave = 0;
    private float nextWaveTime;
    private readonly List<EnemyAI> alive = new List<EnemyAI>();

    private void Start()
    {
        ScheduleNextWave();
    }

    private void Update()
    {
        alive.RemoveAll(e => e == null);
        if (Time.time >= nextWaveTime)
        {
            SpawnWave();
            ScheduleNextWave();
        }
    }

    private void ScheduleNextWave()
    {
        nextWaveTime = Time.time + timeBetweenWaves;
    }

    private void SpawnWave()
    {
        currentWave++;
        int waveSize = startWaveSize + currentWave;
        for (int i = 0; i < waveSize; i++)
        {
            var data = PickEnemyData(currentWave, i == waveSize - 1);
            var spawn = spawnPoints.Length > 0 ? spawnPoints[Random.Range(0, spawnPoints.Length)] : transform;
            SpawnEnemy(data, spawn.position);
        }
    }

    private EnemyData PickEnemyData(int wave, bool lastOfWave)
    {
        if (enemyPool == null || enemyPool.Length == 0) return null;
        if (lastOfWave && bossEveryNWaves > 0 && wave % bossEveryNWaves == 0)
        {
            // pick a boss archetype if exists
            foreach (var e in enemyPool)
            {
                if (e != null && e.archetype == EnemyArchetype.Boss) return e;
            }
        }
        return enemyPool[Random.Range(0, enemyPool.Length)];
    }

    private void SpawnEnemy(EnemyData data, Vector3 pos)
    {
        if (data == null || data.prefab == null) return;
        var go = Instantiate(data.prefab, pos, Quaternion.identity);
        var ai = go.GetComponent<EnemyAI>();
        if (ai == null) ai = go.AddComponent<EnemyAI>();
        ai.ApplyData(data);
        ai.SetTarget(defendPoint);
        alive.Add(ai);
    }
}
