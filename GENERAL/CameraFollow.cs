using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 offset = new Vector3(0, 0, -10);

    [Header("Settings")]
    public float smoothSpeed = 5f;

    [Header("Horizontal Control")]
    public bool useXLimits = true;
    public Vector2 xLimits = new Vector2(-10f, 100f);

    [Header("Vertical Control")]
    public bool lockY = true;
    public float fixedY = 0f;
    public bool useYLimits = false;
    public Vector2 yLimits = new Vector2(0f, 5f);

    // --- SHAKE VARIABLES ---
    private float shakeDuration = 0f;
    private float shakeMagnitude = 0.1f;
    private float shakeDampingSpeed = 1.0f;
    Vector3 shakeOffset = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Calcular posición deseada base
        Vector3 targetPos = target.position + offset;

        // 2. Control Vertical (Y)
        if (lockY)
        {
            targetPos.y = fixedY;
        }
        else if (useYLimits)
        {
            targetPos.y = Mathf.Clamp(targetPos.y, yLimits.x, yLimits.y);
        }

        // 3. Control Horizontal (X)
        if (useXLimits)
        {
            targetPos.x = Mathf.Clamp(targetPos.x, xLimits.x, xLimits.y);
        }

        // 4. Calcular movimiento suavizado
        Vector3 smoothedPos = Vector3.Lerp(transform.position, targetPos, smoothSpeed * Time.deltaTime);

        // 5. APLICAR SHAKE
        if (shakeDuration > 0)
        {
            shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            shakeOffset.z = 0; // No queremos mover Z

            shakeDuration -= Time.deltaTime * shakeDampingSpeed;
        }
        else
        {
            shakeOffset = Vector3.zero;
        }

        // Sumar el shake a la posición final
        transform.position = smoothedPos + shakeOffset;

        // Asegurar Z final por si el lerp lo cambió
        transform.position = new Vector3(transform.position.x, transform.position.y, offset.z);
    }

    // --- MÉTODO PÚBLICO PARA ACTIVAR EL TEMBLOR ---
    public void TriggerShake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
        shakeDampingSpeed = 1.0f; // Puedes parametrizar esto si quieres que decaiga más lento/rápido
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (useXLimits)
        {
            Gizmos.DrawLine(new Vector3(xLimits.x, -100, 0), new Vector3(xLimits.x, 100, 0));
            Gizmos.DrawLine(new Vector3(xLimits.y, -100, 0), new Vector3(xLimits.y, 100, 0));
        }
        Gizmos.color = Color.blue;
        if (lockY)
        {
            Gizmos.DrawLine(new Vector3(-100, fixedY, 0), new Vector3(100, fixedY, 0));
        }
    }
}