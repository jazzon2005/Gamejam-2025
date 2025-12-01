using UnityEngine;
using UnityEngine.EventSystems; // Necesario para detectar Hover/Click
using UnityEngine.UI; // Para acceder al componente Button

public class UISoundEvents : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    [Header("Sonidos")]
    public AudioClip hoverSound;
    public AudioClip clickSound;
    public AudioClip errorSound;

    [Header("Configuración")]
    [Tooltip("Si es TRUE, usa el componente Button para saber si es interactuable.")]
    public bool checkButtonInteractable = true;

    private Button button;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    // Se llama cuando el mouse entra al objeto (Hover)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (CheckInteractable())
        {
            PlaySound(hoverSound);
        }
    }

    // Se llama cuando haces click (Down)
    public void OnPointerDown(PointerEventData eventData)
    {
        if (CheckInteractable())
        {
            PlaySound(clickSound);
        }
        else
        {
            // Si el botón está deshabilitado o no interactuable, suena error
            PlaySound(errorSound);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUISound(clip);
        }
    }

    private bool CheckInteractable()
    {
        // Si tenemos la opción activada y hay un botón, revisamos si es interactuable
        if (checkButtonInteractable && button != null)
        {
            return button.interactable;
        }
        // Si no es un botón (es una imagen) o no chequeamos, asumimos que siempre suena
        return true;
    }
}