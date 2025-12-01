using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SpriteRenderer))]
public class GhostFader : MonoBehaviour
{
    private SpriteRenderer sr;
    private float fadeSpeed; // Declarado aquí

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // Añadir una comprobación de seguridad extra para Awake
        if (sr == null)
        {
            Debug.LogError("GhostFader: ¡No se encontró SpriteRenderer en el prefab del fantasma! El script no funcionará correctamente.", this);
            enabled = false; // Deshabilitar el script si no hay SpriteRenderer
            return;
        }
    }

    public void Setup(Sprite sprite, bool flipX, Vector3 position, Quaternion rotation, float startAlpha, float speed, Color tintColor)
    {
        // Si sr es null por algún motivo (ej. Awake falló), no hacemos nada
        if (sr == null) return;

        transform.position = position;
        transform.rotation = rotation;

        sr.sprite = sprite;
        sr.flipX = flipX;

        // Asegurarse de que el color base se aplique correctamente
        // y que el alpha inicial sea el que queremos
        sr.color = new Color(tintColor.r, tintColor.g, tintColor.b, startAlpha);

        fadeSpeed = speed; // Asignar fadeSpeed aquí

        gameObject.SetActive(true);
        // Detener cualquier rutina de desvanecimiento previa antes de iniciar una nueva
        StopAllCoroutines();
        StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        if (sr == null) yield break; // Salir si sr es nulo

        Color currentColor = sr.color;

        // Bucle while para desvanecer
        while (currentColor.a > 0f)
        {
            // Reducir alpha gradualmente
            currentColor.a -= fadeSpeed * Time.deltaTime; // Usar Time.deltaTime
            sr.color = currentColor;
            yield return null; // Esperar un frame
        }

        // Asegurarse de que el alpha sea exactamente 0 al final, por si acaso
        currentColor.a = 0f;
        sr.color = currentColor;

        // Al terminar, desactivar para reciclar
        gameObject.SetActive(false);
    }
}