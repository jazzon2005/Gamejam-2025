using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    [Header("UI References")]
    public GameObject shopWindow;
    public Transform itemsContainer;
    public GameObject itemCardPrefab;
    public TextMeshProUGUI currentGoldText;

    [Header("Settings")]
    public int optionsCount = 3;
    public int reshuffleCost = 10; // <<-- AÑADIDA
    public TextMeshProUGUI reshuffleCostText; // <<-- NUEVA: Para mostrar el costo en el botón

    private List<ShopItemUI> activeCards = new List<ShopItemUI>();

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // IMPORTANTE: No desactivamos aquí si el UIManager lo va a hacer al inicio.
        // Pero por seguridad, si quieres que empiece cerrado:
        // gameObject.SetActive(false); 
        // Nota: Si desactivas el gameObject aquí, el Instance podría no asignarse si Awake no corre.
        // Mejor dejar que UIManager apague todo en su Start.
    }

    // ESTE ES EL CAMBIO MÁGICO
    // Se ejecuta AUTOMÁTICAMENTE cuando UIManager hace shopPanel.SetActive(true)
    private void OnEnable()
    {
        Debug.Log("ShopUI: ¡Panel activado! Generando cartas...");
        GenerateShopOptions();

        if (CurrencySystem.Instance != null)
        {
            UpdateGoldText(CurrencySystem.Instance.CurrentGold);
        }

        // NUEVO: Actualizar el texto del costo de Reshuffle
        if (reshuffleCostText != null)
        {
            reshuffleCostText.text = reshuffleCost.ToString();
        }
    }

    private void Start()
    {
        if (CurrencySystem.Instance != null)
        {
            CurrencySystem.Instance.OnGoldChanged += UpdateGoldText;
            // No llamamos UpdateGoldText aquí porque OnEnable ya lo hizo al activarse
        }
    }

    public void CloseShop()
    {
        // Simplemente le decimos al GameManager que cambie de estado.
        // El GameManager avisará al UIManager, y el UIManager hará shopPanel.SetActive(false)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        }
        // No hacemos SetActive(false) aquí manualmente para mantener el flujo circular limpio
    }

    public void OnReshuffleButtonClicked()
    {
        if (CurrencySystem.Instance != null)
        {
            // 1. Intentar gastar el oro
            if (CurrencySystem.Instance.TrySpendGold(reshuffleCost))
            {
                Debug.Log($"ShopUI: Reshuffle comprado por {reshuffleCost} de oro.");

                // 2. Limpiar las cartas existentes
                ClearShopOptions();

                // 3. Generar nuevas opciones
                GenerateShopOptions();
            }
            else
            {
                Debug.Log("ShopUI: No tienes suficiente oro para el reshuffle.");
                // Aquí podrías añadir un feedback visual de "No hay oro"
            }
        }
    }

    private void ClearShopOptions()
    {
        // Destruir todas las cartas que ya existen
        foreach (var cardUI in activeCards)
        {
            // Usar DestroyImmediate si estás en un editor script o una situación especial,
            // pero para juego normal, Destroy(gameObject) es lo correcto.
            if (cardUI != null)
            {
                Destroy(cardUI.gameObject);
            }
        }
        activeCards.Clear();
    }

    private void GenerateShopOptions()
    {
        // Limpiar cartas viejas
        foreach (Transform child in itemsContainer) Destroy(child.gameObject);
        activeCards.Clear();

        if (ShopManager.Instance == null)
        {
            Debug.LogError("ShopUI: Falta ShopManager");
            return;
        }

        if (itemCardPrefab == null)
        {
            Debug.LogError("ShopUI: Falta Item Card Prefab");
            return;
        }

        // Obtener datos
        List<ShopItemSO> options = ShopManager.Instance.GetRandomItems(optionsCount);
        Debug.Log($"ShopUI: Generando {options.Count} cartas.");

        // Crear cartas
        foreach (var item in options)
        {
            GameObject cardObj = Instantiate(itemCardPrefab, itemsContainer);

            // --- CORRECCIÓN CRÍTICA ---
            // 1. Resetear la escala a 1. A veces Unity la pone en valores locos al instanciar en UI.
            cardObj.transform.localScale = Vector3.one;

            // 2. Resetear la posición (El Layout Group se encargará, pero esto ayuda)
            cardObj.transform.localPosition = Vector3.zero;
            // --------------------------

            ShopItemUI cardUI = cardObj.GetComponent<ShopItemUI>(); // Busca el script corregido

            if (cardUI != null)
            {
                cardUI.Setup(item);
                activeCards.Add(cardUI);
            }
        }
    }

    public void RefreshAllCards()
    {
        foreach (var card in activeCards)
        {
            card.RefreshUI(); // Recuerda hacer público el método RefreshUI en ShopItemUI
        }
        if (CurrencySystem.Instance != null)
            UpdateGoldText(CurrencySystem.Instance.CurrentGold);
    }

    private void UpdateGoldText(int amount)
    {
        if (currentGoldText != null) currentGoldText.text = $"Oro: {amount}";
    }
}