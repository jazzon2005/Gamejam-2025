using UnityEngine;
using UnityEngine.UI;

// Este script debe estar en el mismo GameObject que el componente Image.
public class AlphaHitTest : MonoBehaviour, ICanvasRaycastFilter
{
    private Image image;

    [Tooltip("El raycast solo pasará si la transparencia (alpha) del píxel es mayor a este valor.")]
    [Range(0.001f, 1f)]
    public float alphaThreshold = 0.1f;

    void Start()
    {
        image = GetComponent<Image>();
        if (image == null)
        {
            Debug.LogError("AlphaHitTest requiere el componente Image.");
            enabled = false;
        }

        // Asegurarse de que el sprite esté configurado correctamente para lectura de píxeles
        if (image.sprite != null && !image.sprite.texture.isReadable)
        {
            Debug.LogError("El Sprite del botón debe tener activado Read/Write Enabled en su configuración de importación.");
            // Esto es crucial para poder leer los píxeles.
        }
    }

    // La interfaz ICanvasRaycastFilter usa este método para decidir si el rayo ha golpeado.
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        // 1. Verificar si el punto de la pantalla está dentro del RectTransform (hitbox amplio)
        if (!RectTransformUtility.RectangleContainsScreenPoint(
            (RectTransform)transform, screenPoint, eventCamera))
        {
            return false; // Está fuera del área general.
        }

        // Si no tenemos sprite o no podemos leer los píxeles, volvemos al comportamiento por defecto.
        if (image.sprite == null || !image.sprite.texture.isReadable)
        {
            return true;
        }

        // 2. Traducir el punto de pantalla a coordenadas locales del Sprite
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform, screenPoint, eventCamera, out Vector2 localPoint);

        // 3. Ajustar el punto local a la posición real del píxel en el sprite
        Rect rect = image.sprite.rect;
        Vector2 normalizedPoint = new Vector2(
            localPoint.x / ((RectTransform)transform).rect.width,
            localPoint.y / ((RectTransform)transform).rect.height
        );

        // Ajuste a las coordenadas de píxel del sprite
        int x = Mathf.FloorToInt(rect.x + rect.width * (normalizedPoint.x + 0.5f));
        int y = Mathf.FloorToInt(rect.y + rect.height * (normalizedPoint.y + 0.5f));

        // 4. Leer el color del píxel y comprobar el canal Alpha
        Color pixelColor = image.sprite.texture.GetPixel(x, y);

        // Si el alpha es menor que el umbral, no es un clic válido.
        return pixelColor.a >= alphaThreshold;
    }
}