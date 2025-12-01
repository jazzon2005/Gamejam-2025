using UnityEngine;
using TMPro; // Necesario para TextMeshPro
using System.Collections;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TypewriterEffect : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Velocidad de escritura (segundos por letra). Menor = Más rápido.")]
    public float typingSpeed = 0.05f;

    [Tooltip("Si es TRUE, inicia automáticamente al activar el objeto.")]
    public bool playOnEnable = true;

    [Header("Audio (Opcional)")]
    public AudioClip typingSound;
    public int soundFrequency = 2;

    public TMP_Text textComponent;
    private Coroutine typingCoroutine;

    void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();

        if (playOnEnable)
        {
            StartTypewriter();
        }
    }

    public void StartTypewriter()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);

        // CRUCIAL: Forzar actualización de la malla para que TMP sepa cuántas letras hay
        textComponent.ForceMeshUpdate();

        typingCoroutine = StartCoroutine(TypeTextRoutine());
    }

    private IEnumerator TypeTextRoutine()
    {
        // 1. Asegurarnos de que TMP tenga info válida
        textComponent.maxVisibleCharacters = 0;
        yield return null; // Esperar un frame para que Unity dibuje y TMP calcule

        int totalVisibleCharacters = textComponent.textInfo.characterCount;

        // Debug para verificar si está leyendo el texto
        // Debug.Log($"Typewriter: Escribiendo {totalVisibleCharacters} caracteres.");

        int counter = 0;

        while (counter <= totalVisibleCharacters)
        {
            textComponent.maxVisibleCharacters = counter;

            // Sonido
            if (counter > 0 && counter % soundFrequency == 0 && typingSound != null)
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayUISound(typingSound);
                }
            }

            yield return new WaitForSeconds(typingSpeed);
            counter++;
        }
    }

    // Método público para saltar la animación (conectar a botón transparente encima del texto)
    public void SkipAnimation()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (textComponent != null)
        {
            textComponent.maxVisibleCharacters = 99999; // Mostrar todo
        }
    }
}