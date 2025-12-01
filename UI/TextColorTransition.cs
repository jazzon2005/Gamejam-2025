using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using TMPro;

[RequireComponent(typeof(Button))]
public class TextColorTransition : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Referencias")]
    public TextMeshProUGUI targetText;

    [Header("Efecto Hover")]
    public float hoverYOffset = 5f; // Cantidad de unidades que el texto se moverá hacia arriba al hacer hover

    [Header("Colores (Visuales)")]
    [HideInInspector] public Color normalColor;
    [HideInInspector] public Color hoverColor;
    [HideInInspector] public Color pressedColor;
    [HideInInspector] public Color disabledColor;

    private Button button;
    private Vector2 originalPosition; // <-- Guardaremos la posición inicial del RectTransform

    void Awake()
    {
        // Inicialización de colores desde Hexadecimal
        normalColor = HexToColor("0D9A0D");
        hoverColor = HexToColor("00FF00");
        pressedColor = HexToColor("00FF00");
        disabledColor = HexToColor("66AB66");

        // Obtener referencias
        button = GetComponent<Button>();
    }

    void Start()
    {
        if (targetText == null) Debug.LogError("Asigna el componente TextMeshProUGUI al script TextColorTransition.");

        if (targetText != null)
        {
            // ¡GUARDAR LA POSICIÓN INICIAL!
            originalPosition = targetText.rectTransform.anchoredPosition;
            targetText.color = normalColor;
        }
    }

    void Update()
    {
        // Manejar el estado Disabled
        if (!button.interactable)
        {
            if (targetText != null && targetText.color != disabledColor)
            {
                targetText.color = disabledColor;
            }
        }
        else if (targetText != null && targetText.color == disabledColor)
        {
            targetText.color = normalColor;
        }
    }

    // --- MANEJO DE EVENTOS ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.interactable && targetText != null)
        {
            // 1. Aplicar Color Hover
            targetText.color = hoverColor;

            // 2. Aplicar Desplazamiento
            // Creamos una nueva posición sumando el offset a la posición Y original
            Vector2 hoverPosition = originalPosition;
            hoverPosition.y += hoverYOffset;
            targetText.rectTransform.anchoredPosition = hoverPosition;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (button.interactable && targetText != null)
        {
            // 1. Restaurar Color Normal
            targetText.color = normalColor;

            // 2. Restaurar Posición Original
            targetText.rectTransform.anchoredPosition = originalPosition;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (button.interactable && targetText != null)
        {
            targetText.color = pressedColor;
            targetText.rectTransform.anchoredPosition = originalPosition; 
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable && targetText != null)
        {
            if (EventSystem.current.IsPointerOverGameObject(eventData.pointerId))
            {
                // Si el mouse sigue encima, volvemos al estado Hover (Color y Posición)
                targetText.color = hoverColor;
                Vector2 hoverPosition = originalPosition;
                hoverPosition.y += hoverYOffset;
                targetText.rectTransform.anchoredPosition = hoverPosition;
            }
            else
            {
                // Si el mouse no está encima, volvemos al estado Normal
                targetText.color = normalColor;
                targetText.rectTransform.anchoredPosition = originalPosition;
            }
        }
    }

    // --- FUNCIÓN DE CONVERSIÓN ---

    public static Color HexToColor(string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        if (hex.Length != 6 && hex.Length != 8)
        {
            // Mejor no hacer Debug.LogError si la función no está en un MonoBehaviour, pero aquí está bien.
            return Color.black;
        }

        try
        {
            byte r = Convert.ToByte(hex.Substring(0, 2), 16);
            byte g = Convert.ToByte(hex.Substring(2, 2), 16);
            byte b = Convert.ToByte(hex.Substring(4, 2), 16);
            byte a = (hex.Length == 8) ? Convert.ToByte(hex.Substring(6, 2), 16) : (byte)255;

            return new Color((float)r / 255f, (float)g / 255f, (float)b / 255f, (float)a / 255f);
        }
        catch (FormatException)
        {
            return Color.black;
        }
    }
}