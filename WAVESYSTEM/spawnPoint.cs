using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Si es true, spawnea en posiciones aleatorias dentro del radio")]
    public bool randomizePosition = true;

    [Tooltip("Radio de aleatorización de spawn")]
    public float spawnRadius = 2f;

    [Header("Visual")] //Debug, eliminar al finalizar
    public Color gizmoColor = Color.red;

    // Obtener posición de spawn (aleatoria o fija)
    public Vector3 GetSpawnPosition()
    {
        if (randomizePosition)
        {
            // Posición aleatoria dentro del radio
            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            return transform.position + (Vector3)randomOffset;
        }
        else
        {
            // Posición exacta del punto
            return transform.position;
        }
    }

    // Obtener rotación (por defecto la del transform)
    public Quaternion GetSpawnRotation()
    {
        return transform.rotation;
    }

    // Debug visual eliminar al finalizar
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        if (randomizePosition)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.2f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, 0.3f);

        if (randomizePosition)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }
    }
}