using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("1. Configuración de Modo Historia (Secuencial)")]
    [Tooltip("Las oleadas en orden. Se usan hasta que se alcanza el maxWaves del GameManager.")]
    public List<WaveConfig> storyWaves;

    [Header("2. Configuración de Modo Survival (Progresivo)")]
    [Tooltip("Oleadas diseñadas para Survival. ¡Deben ir de más fácil a más difícil!")]
    public List<WaveConfig> survivalWaves;

    public EnemyPool enemyPool;

    [Header("Spawn Settings")]
    public List<SpawnPoint> spawnPoints;
    public Transform spawnCenter;
    public float spawnRadius = 10f;

    [Header("Visuals")]
    public Animator waveAnimator;
    public string waveAnimationBoolName = "IsWaveStarting";

    // Estado interno
    private int enemiesAlive = 0;
    private int enemiesSpawned = 0;
    private bool isWaveActive = false;

    // Propiedades para que el GameManager sepa qué pasa
    public bool IsWaveFinished => !isWaveActive && enemiesAlive == 0;
    public int EnemiesAlive => enemiesAlive;

    // Sonido
    public AudioClip waveStartSound;

    void Start()
    {
        if (spawnCenter == null) spawnCenter = transform;
    }

    // ========== MÉTODOS PÚBLICOS PARA EL GAMEMANAGER ==========

    // Método llamado por GameManager para obtener la siguiente oleada
    public WaveConfig GetNextWaveConfig(int waveIndex)
    {
        // Leemos el modo y el límite del GameManager
        bool isInfinite = GameManager.Instance.infiniteWaves;
        int maxStoryWaves = GameManager.Instance.maxWaves;

        // --- 1. MODO HISTORIA (Rondas Exactas) ---
        if (!isInfinite)
        {
            // Si aún estamos dentro del límite de rondas:
            if (waveIndex < maxStoryWaves)
            {
                // Usamos la lista secuencial de Story
                if (storyWaves != null && waveIndex < storyWaves.Count)
                {
                    // **Aseguramos la ronda N:** Si la ronda es 5, devuelve storyWaves[5].
                    return storyWaves[waveIndex];
                }

                // Si llegamos aquí, faltan waves diseñadas
                Debug.LogError($"WaveManager: Falta la WaveConfig para el índice {waveIndex} en Story Mode. Se terminará el juego.");
                // En este caso, dejamos que el GameManager se encargue de la victoria
                return null;
            }
            else
            {
                // Ya se completó la última ola del modo historia (waveIndex == maxWaves)
                return null; // El GameManager debe interpretar 'null' como fin de juego/Victoria
            }
        }

        // --- 2. MODO SURVIVAL (Progresión Justa) ---
        else
        {
            if (survivalWaves == null || survivalWaves.Count == 0) return null;

            // **CORRECCIÓN DE JUSTICIA:**
            // Hacemos que la probabilidad de escoger una ola difícil aumente con el tiempo.
            // Asumimos que 'survivalWaves' está ordenado de fácil a difícil.

            int totalSurvivalWaves = survivalWaves.Count;

            // Tarda 30 rondas en desbloquear el 100% del pool (ejemplo de escalado).
            float progressionRatio = Mathf.Clamp01((float)waveIndex / 30f);

            // Número de olas disponibles (mínimo 1)
            int availableWaveCount = Mathf.Max(1, Mathf.RoundToInt(totalSurvivalWaves * progressionRatio));

            // Elegir aleatoriamente de la sub-lista disponible (las olas más fáciles/justas)
            int randomIndex = Random.Range(0, availableWaveCount);

            return survivalWaves[randomIndex];
        }
    }

    // El GameManager llama a esto cuando termina el tiempo de "Tensión"
    public void StartSpawningWave(WaveConfig config)
    {
        enemiesAlive = 0;
        enemiesSpawned = 0;
        isWaveActive = true;

        StartCoroutine(SpawnRoutine(config));
    }

    public void PlayWaveWarning(bool play)
    {
        if (waveAnimator != null)
        {
            waveAnimator.SetBool(waveAnimationBoolName, play);
        }

        // Sonido de inicio (Solo cuando empieza, es decir, cuando play es true)
        if (play && AudioManager.Instance != null && waveStartSound != null)
        {
            AudioManager.Instance.PlayUISound(waveStartSound);
        }
    }

    public void StopCurrentWave()
    {
        StopAllCoroutines();
        isWaveActive = false;
        if (enemyPool != null) enemyPool.DespawnAllEnemies();
    }

    // ========== LÓGICA INTERNA ==========

    private IEnumerator SpawnRoutine(WaveConfig wave)
    {
        List<EnemySpawnData> enemiesToSpawn = BuildWeightedEnemyList(wave);

        while (enemiesSpawned < enemiesToSpawn.Count && isWaveActive)
        {
            // Control de concurrencia
            if (enemiesAlive >= wave.maxConcurrentEnemies)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            SpawnEnemy(enemiesToSpawn[enemiesSpawned], wave);
            enemiesSpawned++;

            if (enemiesSpawned < enemiesToSpawn.Count)
            {
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        isWaveActive = false;
    }

    private void SpawnEnemy(EnemySpawnData enemyData, WaveConfig wave)
    {
        if (enemyPool == null) return;

        Vector3 pos = GetSpawnPosition();
        GameObject enemy = enemyPool.SpawnFromPool(enemyData.enemyPrefab.name, pos, Quaternion.identity);

        if (enemy != null)
        {
            // Aplicar stats y dificultad
            if (enemyData.enemyStats != null)
            {
                var controller = enemy.GetComponent<EnemyController>();
                if (controller) controller.stats = enemyData.enemyStats;
            }

            float finalHealthMult = wave.healthMultiplier * GameManager.Instance.DifficultyHealthMultiplier;
            float finalDamageMult = wave.damageMultiplier * GameManager.Instance.DifficultyDamageMultiplier;

            enemyPool.ApplyStatsMultipliers(enemy, finalHealthMult, finalDamageMult);

            // Suscribirse a muerte para el conteo
            var health = enemy.GetComponent<CharacterHealth>();
            if (health != null)
            {
                // Desuscribirse primero por seguridad para no duplicar si el pool no limpió bien
                health.OnDeath -= OnEnemyDied;
                health.OnDeath += OnEnemyDied;
            }

            enemiesAlive++;
        }
    }

    private void OnEnemyDied()
    {
        enemiesAlive--;
    }

    // Utilidades
    private List<EnemySpawnData> BuildWeightedEnemyList(WaveConfig wave)
    {
        List<EnemySpawnData> list = new List<EnemySpawnData>();
        if (wave.enemyTypes != null)
        {
            foreach (var t in wave.enemyTypes)
            {
                for (int i = 0; i < t.count; i++) list.Add(t);
            }
        }

        // Shuffle Fisher-Yates
        for (int i = list.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            var tmp = list[i];
            list[i] = list[r];
            list[r] = tmp;
        }
        return list;
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Count > 0)
            return spawnPoints[Random.Range(0, spawnPoints.Count)].GetSpawnPosition();
        return spawnCenter.position + (Vector3)Random.insideUnitCircle * spawnRadius;
    }
}