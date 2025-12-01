using UnityEngine;

[CreateAssetMenu(fileName = "WaveConfig", menuName = "Scriptable Objects/Wave Config")]
public class WaveConfig : ScriptableObject //Simplemente describe las características de una oleada, crear multiples para variedad
{
    [Header("Wave Info")]
    public string waveName = "Wave 1";

    [Header("Story Mode (Opcional)")]
    [TextArea] public string storyMessage; // Lo que dice el científico/narrador
    public Sprite narratorSprite; // La cara del que habla (si cambia)
    [Tooltip("Duración del mensaje en pantalla (0 = hasta que termine la oleada)")]
    public float messageDuration = 5f;
    // NUEVO: El sonido de la voz del doctor
    public AudioClip narratorVoice;

    [Header("Enemy Composition")]
    [Tooltip("Lista de tipos de enemigos que aparecerán en esta oleada")]
    public EnemySpawnData[] enemyTypes;

    [Header("Spawn Behavior")]
    [Tooltip("Cuántos enemigos pueden estar vivos al mismo tiempo")]
    public int maxConcurrentEnemies = 5;

    [Tooltip("Tiempo de espera entre spawns individuales")]
    public float spawnInterval = 2f;

    [Header("Wave Timing")]
    [Tooltip("Tiempo de espera antes de iniciar esta oleada")]
    public float delayBeforeWave = 5f;

    [Tooltip("Tiempo de calma después de completar la oleada")]
    public float delayAfterWave = 5f;

    [Header("Difficulty")]
    [Tooltip("Multiplicador de vida para todos los enemigos (1 = normal, 1.5 = +50% HP)")]
    [Range(0.5f, 5f)]
    public float healthMultiplier = 1f;

    [Tooltip("Multiplicador de daño para todos los enemigos")]
    [Range(0.5f, 5f)]
    public float damageMultiplier = 1f;

    // Calcula el total de enemigos en la oleada
    public int GetTotalEnemyCount()
    {
        int total = 0;
        foreach (var enemyData in enemyTypes)
        {
            total += enemyData.count;
        }
        return total;
    }
}

[System.Serializable]
public class EnemySpawnData //Define un tipo de enemigo y cuántos spawnear
{
    [Tooltip("Prefab del enemigo a spawnear")]
    public GameObject enemyPrefab;

    [Tooltip("Stats específicos para este tipo de enemigo")]
    public CharacterStats enemyStats;

    [Tooltip("Cuántos enemigos de este tipo spawear")]
    public int count = 1;

    [Tooltip("Peso para selección aleatoria (mayor = más probable)")]
    [Range(1, 10)]
    public int spawnWeight = 5;
}