using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    [Header("References")]
    public CharacterStats stats; //Obtiene SO de estadísticas del enemigo
    public LayerMask playerLayer; // Capa del jugador

    [Header("Detection Settings")]
    public bool detectOnStart = false; // Si es true, busca al jugador desde el inicio (enemigos agresivos)
    public float detectionUpdateRate = 0.2f; // Cada cuántos segundos verifica (optimización)

    [Header("Lost Target Settings")]
    public bool canLoseTarget = true; // Si puede perder de vista al jugador
    public float loseTargetDistance = 0f; // Distancia extra para perder target (0 = usa detectionRadius * 1.5)

    // Estado actual
    private Transform currentTarget; // Target actual (jugador), privada para evitar manipulaciones externas
    private float detectionTimer; // Timer para optimizar detección
    private float actualLoseDistance; // Distancia real para perder target

    // Propiedades públicas
    public Transform CurrentTarget => currentTarget; // Getter para el target actual
    public bool HasTarget => currentTarget != null; // Verifica si tiene un target actual

    // Eventos para que otros scripts escuchen
    public System.Action<Transform> OnPlayerDetected; // Se dispara cuando detecta al jugador
    public System.Action OnPlayerLost; // Se dispara cuando pierde al jugador

    void Start()
    {
        // Calcular distancia para perder target (si no está configurada manualmente)
        if (stats != null)
        {
            actualLoseDistance = loseTargetDistance > 0 ? loseTargetDistance : stats.detectionRadius * 1.5f; // Por defecto, 1.5 veces el radio de detección
        }

        // Si es enemigo agresivo, busca al jugador inmediatamente
        if (detectOnStart)
        {
            DetectPlayer(); // Intentar detectar al jugador al inicio
        }
    }

    void Update()
    {
        // Sistema de timer para optimizar (no verifica cada frame)
        detectionTimer += Time.deltaTime;

        if (detectionTimer >= detectionUpdateRate) // Cada cierto tiempo
        {
            detectionTimer = 0f; // Resetear timer

            // Si ya tiene target, verificar si lo perdió
            if (HasTarget)
            {
                CheckIfLostTarget();
            }
            else
            {
                // Si no tiene target, buscar jugador
                DetectPlayer();
            }
        }
    }

    // ========== MÉTODOS PRINCIPALES ==========

    private void DetectPlayer()
    {
        if (stats == null) return; // Asegurarse de tener estadísticas

        // Buscar jugador en el radio de detección
        Collider2D playerCollider = Physics2D.OverlapCircle(
            transform.position,
            stats.detectionRadius,
            playerLayer
        ); //Simplemente dibuja un circulo alrededor de el y verifica si hay algun collider en la capa del jugador dentro de este

        // Si encontró al jugador
        if (playerCollider != null)
        {
            Transform detectedPlayer = playerCollider.transform; // Obtener transform del jugador detectado

            // Solo disparar evento si es un nuevo target
            if (currentTarget != detectedPlayer)
            {
                currentTarget = detectedPlayer; // Asignar nuevo target
                OnPlayerDetected?.Invoke(currentTarget); // Disparar evento de detección
            }
        }
    }

    private void CheckIfLostTarget()
    {
        if (!canLoseTarget || currentTarget == null) return; // Si no puede perder target o no hay target, salir

        // Verificar distancia al target actual
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);

        // Si se alejó demasiado, perder el target
        if (distanceToTarget > actualLoseDistance)
        {
            LoseTarget();
        }
    }

    private void LoseTarget()
    {
        currentTarget = null; // Limpiar target actual
        OnPlayerLost?.Invoke();     // Disparar evento de pérdida de target
    }

    // ========== MÉTODOS PÚBLICOS ==========

    // Forzar detección inmediata (útil para cuando el jugador ataca al enemigo)
    public void ForceDetection()
    {
        DetectPlayer(); // Intentar detectar al jugador inmediatamente
    }

    // Forzar perder target (útil para enemigos que se distraen o son stuneados)
    public void ForceLoseTarget()
    {
        if (HasTarget) // Solo si tiene target
        {
            LoseTarget(); // Forzar pérdida de target
        }
    }

    // Asignar target manualmente (útil para enemigos que son alertados por otros)
    public void SetTarget(Transform newTarget)
    {
        if (currentTarget != newTarget) // Solo si es un target diferente
        {
            currentTarget = newTarget; // Asignar nuevo target
            OnPlayerDetected?.Invoke(currentTarget); // Disparar evento de detección
        }
    }

    // Cambiar si puede detectar o no (útil para enemigos dormidos/desactivados)
    public void SetCanDetect(bool value)
    {
        detectOnStart = value; // Actualizar estado de detección

        if (!value && HasTarget) // Si desactiva detección y tiene target
        {
            LoseTarget(); // Perder target
        }
    }

    // ========== DEBUG ==========

    private void OnDrawGizmosSelected()
    {
        if (stats == null) return;

        // Radio de detección - Color AMARILLO
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stats.detectionRadius);

        // Radio para perder target - Color NARANJA
        if (canLoseTarget)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Naranja transparente
            float loseRadius = loseTargetDistance > 0 ? loseTargetDistance : stats.detectionRadius * 1.5f;
            Gizmos.DrawWireSphere(transform.position, loseRadius);
        }

        // Línea hacia el target actual - Color CYAN
        if (HasTarget)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}