using UnityEngine;

public class BackgroundFollower : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Si lo dejas vacío, buscará la Main Camera automáticamente")]
    public Transform targetToFollow;

    [Header("Follow Settings")]
    [Tooltip("Para OJO: Sensibilidad de movimiento (0.1 = sutil, 0.5 = sigue mucho)\nPara LUNA: 1 = pegado a cámara, 0 = estático")]
    [Range(0f, 1.5f)]
    public float followFactor = 0.1f; // Reducido por defecto para ojos

    [Tooltip("Suavizado del movimiento")]
    public float smoothSpeed = 5f;

    [Header("Eye / Local Mode")]
    [Tooltip("Si es > 0, el objeto solo reaccionará cuando el jugador esté a esta distancia (Ideal para Ojos). Si es 0, funciona en todo el mapa (Ideal para Lunas/Fondos).")]
    public float activationRange = 15f;

    [Header("Movement Limits")]
    public bool enableLimits = true;
    [Tooltip("Distancia máxima que la pupila se puede mover del centro del ojo")]
    public Vector2 rangeLimit = new Vector2(0.5f, 0.3f); // Valores pequeños para un ojo

    // Estado interno
    private Vector3 startPosition;
    private Vector3 initialTargetPos;
    private Vector3 offset;

    void Start()
    {
        if (targetToFollow == null)
        {
            if (Camera.main != null) targetToFollow = Camera.main.transform;
            else Debug.LogError("BackgroundFollower: ¡No encuentro a quién seguir!");
        }

        startPosition = transform.position;

        if (targetToFollow != null)
        {
            initialTargetPos = targetToFollow.position;
            offset = transform.position - targetToFollow.position;
        }
    }

    void LateUpdate()
    {
        if (targetToFollow == null) return;

        Vector3 desiredPosition = startPosition;

        // --- MODO LOCAL (OJO / EYE FOLLOW) ---
        if (activationRange > 0)
        {
            // 1. Comprobar distancia
            float distanceToTarget = Vector2.Distance(targetToFollow.position, startPosition);

            if (distanceToTarget <= activationRange)
            {
                // 2. Calcular dirección hacia el jugador desde el CENTRO del ojo
                Vector3 directionToTarget = targetToFollow.position - startPosition;

                // 3. Calcular posición de la pupila
                // followFactor aquí actúa como "cuánto se mueve la pupila por cada metro que se mueve el jugador"
                Vector3 moveAmount = directionToTarget * followFactor;

                // 4. Aplicar límites (Clamp)
                // Esto evita que la pupila se salga del ojo
                float clampedX = Mathf.Clamp(moveAmount.x, -rangeLimit.x, rangeLimit.x);
                float clampedY = Mathf.Clamp(moveAmount.y, -rangeLimit.y, rangeLimit.y);

                desiredPosition = startPosition + new Vector3(clampedX, clampedY, 0);
            }
            else
            {
                // Fuera de rango: Volver al centro
                desiredPosition = startPosition;
            }
        }
        // --- MODO GLOBAL (LUNA / PARALLAX) ---
        else
        {
            Vector3 delta = targetToFollow.position - initialTargetPos;
            Vector3 relativeMovement = delta * followFactor;

            // El modo global usa lógica relativa al inicio del juego
            Vector3 rawPosition = startPosition + relativeMovement;

            if (enableLimits)
            {
                float clampedX = Mathf.Clamp(rawPosition.x, startPosition.x - rangeLimit.x, startPosition.x + rangeLimit.x);
                float clampedY = Mathf.Clamp(rawPosition.y, startPosition.y - rangeLimit.y, startPosition.y + rangeLimit.y);
                desiredPosition = new Vector3(clampedX, clampedY, transform.position.z);
            }
            else
            {
                desiredPosition = rawPosition;
            }
        }

        // Movimiento Suave
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }

    private void OnDrawGizmosSelected()
    {
        // Dibujar límites de movimiento (Caja verde)
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        if (enableLimits || activationRange > 0)
            Gizmos.DrawWireCube(center, new Vector3(rangeLimit.x * 2, rangeLimit.y * 2, 0));

        // Dibujar rango de activación (Círculo amarillo) - Solo para modo Ojo
        if (activationRange > 0)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, activationRange);
        }
    }
}