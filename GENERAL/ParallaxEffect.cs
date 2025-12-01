using UnityEngine;

public class ParallaxEffect : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("0 = Se mueve con la cámara (Cielo muy lejano)\n1 = Se mueve normal (Suelo donde pisas)")]
    [Range(0f, 1f)]
    public float parallaxFactor;

    [Tooltip("Si es true, la imagen se repetirá infinitamente (necesita ser 3 veces el ancho de la camara)")]
    public bool infiniteHorizontal = false;

    [Tooltip("Si es true, el efecto también se aplica verticalmente (útil para juegos de plataformas altos)")]
    public bool applyVertical = true;

    // Referencias internas
    private Transform cameraTransform;
    private Vector3 lastCameraPosition;
    private float textureUnitSizeX;

    void Start()
    {
        // Configurar cámara automáticamente
        if (Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
            lastCameraPosition = cameraTransform.position;
        }
        else
        {
            Debug.LogError("ParallaxEffect: ¡No encuentro la MainCamera!");
            enabled = false;
            return;
        }

        // Configurar repetición infinita (opcional)
        if (infiniteHorizontal)
        {
            SpriteRenderer sprite = GetComponent<SpriteRenderer>();
            if (sprite != null)
            {
                textureUnitSizeX = sprite.bounds.size.x;
            }
        }
    }

    void LateUpdate() // LateUpdate para ir suave con la cámara (igual que CameraFollow)
    {
        if (cameraTransform == null) return;

        // 1. Calcular cuánto se movió la cámara desde el último frame
        Vector3 deltaMovement = cameraTransform.position - lastCameraPosition;

        // 2. Calcular cuánto debe moverse este objeto basado en el factor parallax
        // Si factor es 0 (fondo lejano), se mueve IGUAL que la cámara (parece quieto)
        // Si factor es 1 (primer plano), no se mueve extra (se queda fijo en el mundo)
        float parallaxX = deltaMovement.x * parallaxFactor;
        float parallaxY = applyVertical ? deltaMovement.y * parallaxFactor : 0f;

        // 3. Aplicar movimiento
        transform.position += new Vector3(parallaxX, parallaxY, 0);

        // 4. Lógica de fondo infinito (Tileable)
        if (infiniteHorizontal)
        {
            // Distancia absoluta de la cámara respecto al objeto
            float temp = (cameraTransform.position.x * (1 - parallaxFactor));

            // Si la cámara se ha movido más allá del borde de la textura, reposicionamos el fondo
            if (Mathf.Abs(cameraTransform.position.x - transform.position.x) >= textureUnitSizeX)
            {
                float offsetPositionX = (cameraTransform.position.x - transform.position.x) % textureUnitSizeX;
                transform.position = new Vector3(cameraTransform.position.x + offsetPositionX, transform.position.y);
            }
        }

        // Actualizar última posición
        lastCameraPosition = cameraTransform.position;
    }
}