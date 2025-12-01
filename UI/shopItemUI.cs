using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemUI : MonoBehaviour // Renombrado a PascalCase
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;
    public Button purchaseButton;
    public TextMeshProUGUI levelText;

    // Datos internos
    private ShopItemSO currentItem;

    // Setup inicial llamado por el ShopUIManager
    public void Setup(ShopItemSO item)
    {
        currentItem = item;
        RefreshUI();

        // Configurar botón: Limpiar listeners viejos y añadir el nuevo
        purchaseButton.onClick.RemoveAllListeners();
        purchaseButton.onClick.AddListener(OnPurchaseClicked);
    }

    public void RefreshUI()
    {
        if (currentItem == null) return;

        // 1. Datos estáticos visuales
        if (iconImage != null) iconImage.sprite = currentItem.icon;
        if (titleText != null) titleText.text = currentItem.title;
        if (descriptionText != null) descriptionText.text = currentItem.description;

        // 2. Datos dinámicos (Precio y Nivel desde el Backend)
        // Asegúrate que ShopManager exista y sea accesible
        if (ShopManager.Instance != null)
        {
            int level = ShopManager.Instance.GetItemLevel(currentItem);
            int cost = currentItem.GetCost(level);

            if (costText != null) costText.text = cost.ToString();
            if (levelText != null) levelText.text = $"Lvl {level}"; // Nivel actual

            // 3. Validar si tenemos dinero (Feedback visual en el botón)
            if (CurrencySystem.Instance != null)
            {
                bool canAfford = CurrencySystem.Instance.CurrentGold >= cost;

                // Si es compra única y ya se tiene, o si no alcanza el dinero -> Desactivar
                bool isMaxedOut = currentItem.isOneTimePurchase && level > 0;

                purchaseButton.interactable = canAfford && !isMaxedOut;

                if (isMaxedOut && descriptionText != null)
                    descriptionText.text = "¡AGOTADO!";
            }
        }
    }

    private void OnPurchaseClicked()
    {
        if (ShopManager.Instance == null) return;

        // Intentar comprar usando la lógica del Backend
        bool success = ShopManager.Instance.TryPurchaseItem(currentItem);

        if (success)
        {
            // Si compramos, refrescar visualmente esta carta (precio nuevo, nivel nuevo)
            RefreshUI();

            // Avisar al Manager de UI para que actualice el texto de Oro global y quizas otras cartas
            ShopUIManager.Instance.RefreshAllCards();
        }
    }
}