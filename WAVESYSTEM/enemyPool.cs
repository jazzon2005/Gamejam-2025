using System.Collections.Generic;
using UnityEngine;

public class EnemyPool : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
    }

    [Header("Pool Configuration")]
    public List<Pool> pools;

    // Diccionario para acceso rápido a pools
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    void Start()
    {
        InitializePools();
    }

    private void InitializePools()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            if (pool.prefab == null)
            {
                Debug.LogError($"Pool con tag '{pool.tag}' no tiene prefab asignado!");
                continue;
            }

            Queue<GameObject> objectQueue = new Queue<GameObject>();

            // Pre-instanciar objetos
            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = CreateNewEnemy(pool.prefab);
                objectQueue.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectQueue);
            Debug.Log($"Pool '{pool.tag}' creado con {pool.initialSize} objetos");
        }

        Debug.Log($"EnemyPool inicializado con {pools.Count} pools");
    }

    // Crear nuevo enemigo e inicializarlo
    private GameObject CreateNewEnemy(GameObject prefab)
    {
        GameObject obj = Instantiate(prefab, transform);
        obj.SetActive(false);

        // --- CAMBIO CRÍTICO AQUÍ ---
        // ANTES: health.OnDeath += () => ReturnToPool(obj);
        // PROBLEMA: Esto desactivaba el objeto inmediatamente.

        // AHORA: Usamos un componente auxiliar o modificamos CharacterHealth.
        // Pero la forma más limpia en Unity para Pools sin tocar mucho código es:

        PoolableObject poolable = obj.AddComponent<PoolableObject>();
        poolable.Setup(this);

        return obj;
    }

    // Obtener enemigo del pool
    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"Pool con tag '{tag}' no existe");
            return null;
        }

        GameObject obj;

        // Si no hay objetos disponibles, crear uno nuevo
        if (poolDictionary[tag].Count == 0)
        {
            Pool pool = pools.Find(p => p.tag == tag);
            obj = CreateNewEnemy(pool.prefab);
            Debug.Log($"Pool '{tag}' expandido - Creando nuevo enemigo");
        }
        else
        {
            obj = poolDictionary[tag].Dequeue();
        }

        // Configurar posición y activar
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        // Reiniciar componentes del enemigo
        ResetEnemy(obj);

        return obj;
    }

    public void ReturnEnemyToPool(GameObject obj)
    {
        ReturnToPool(obj);
    }

    // Devolver enemigo al pool
    private void ReturnToPool(GameObject obj)
    {

        // Encontrar a qué pool pertenece
        string poolTag = FindPoolTag(obj);

        if (poolTag != null)
        {
            obj.SetActive(false);
            poolDictionary[poolTag].Enqueue(obj);
        }
    }

    // Reiniciar estado del enemigo
    private void ResetEnemy(GameObject enemy)
    {
        // Reiniciar salud
        CharacterHealth health = enemy.GetComponent<CharacterHealth>();
        if (health != null)
        {
            health.ResetHealth();
        }

        // Reiniciar controller
        EnemyController controller = enemy.GetComponent<EnemyController>();
        if (controller != null)
        {
            controller.enabled = true;
            // El controller se reiniciará con su Start() cuando se active
        }

        // Reiniciar ataque
        EnemyAttack attack = enemy.GetComponent<EnemyAttack>();
        if (attack != null)
        {
            attack.SetCanAttack(true);
            attack.ResetCooldown();
        }

        // Reiniciar movimiento
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
        if (movement != null)
        {
            movement.SetCanMove(true);
            movement.SetTarget(null);
        }

        // Reiniciar detección
        EnemyDetection detection = enemy.GetComponent<EnemyDetection>();
        if (detection != null)
        {
            detection.ForceLoseTarget();
        }

        Rigidbody2D rb = enemy.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // IMPORTANTE: Volver a Dynamic para que le afecte la física y gravedad normal
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    // Encontrar el tag del pool al que pertenece un objeto
    private string FindPoolTag(GameObject obj)
    {
        foreach (var pool in pools)
        {
            // Comparar por nombre del prefab
            if (obj.name.StartsWith(pool.prefab.name))
            {
                return pool.tag;
            }
        }
        return null;
    }

    // Aplicar multiplicadores de stats
    public void ApplyStatsMultipliers(GameObject enemy, float healthMultiplier, float damageMultiplier)
    {
        CharacterHealth health = enemy.GetComponent<CharacterHealth>();
        CharacterStats stats = enemy.GetComponent<EnemyController>()?.stats;

        if (health != null && stats != null)
        {
            // Ajustar salud actual y máxima
            int newMaxHealth = Mathf.RoundToInt(stats.maxHealth * healthMultiplier);
            health.currentHealth = newMaxHealth;

            // NOTA: No modificamos el ScriptableObject directamente para no afectar otras instancias
            // Si necesitas modificar daño, considera crear copias runtime de CharacterStats
        }
    }

    // Desactivar todos los enemigos activos (útil para Game Over)
    public void DespawnAllEnemies()
    {
        foreach (var poolEntry in poolDictionary)
        {
            // Los enemigos activos están fuera de la queue
            // Buscar todos los hijos del pool que estén activos
            foreach (Transform child in transform)
            {
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                    poolEntry.Value.Enqueue(child.gameObject);
                }
            }
        }

        Debug.Log("Todos los enemigos despawneados");
    }
}