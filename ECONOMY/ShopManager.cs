using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [Header("Inventario de la Tienda")]
    [Tooltip("Arrastra aquí todos los ScriptableObjects de ítems que quieres que existan en el juego")]
    public List<ShopItemSO> allAvailableItems;

    [Header("Referencias")]
    // Referencia al jugador para aplicarle las mejoras. 
    // Se busca automáticamente en Start.
    public GameObject player;

    // Estado interno: Guardamos cuántas veces has comprado cada ítem
    // Clave: El ItemSO, Valor: Nivel actual (0 = no comprado)
    private Dictionary<ShopItemSO, int> itemLevels = new Dictionary<ShopItemSO, int>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");

        // Inicializar el diccionario de niveles
        foreach (var item in allAvailableItems)
        {
            if (!itemLevels.ContainsKey(item))
            {
                itemLevels.Add(item, 0);
            }
        }
    }

    // ==================================================
    // LÓGICA DE COMPRA
    // ==================================================

    public bool TryPurchaseItem(ShopItemSO item)
    {
        if (player == null)
        {
            Debug.LogError("ShopManager: ¡No encuentro al jugador para darle la mejora!");
            return false;
        }

        // 1. Verificar estado del ítem
        int currentLvl = GetItemLevel(item);

        // Si es de compra única y ya lo tenemos (nivel > 0), abortar.
        if (item.isOneTimePurchase && currentLvl > 0)
        {
            Debug.Log("¡Ya tienes este ítem!");
            return false;
        }

        // 2. Calcular Costo
        int cost = item.GetCost(currentLvl);

        // 3. Verificar Dinero (Usamos el CurrencySystem de la Fase 2)
        if (CurrencySystem.Instance.TrySpendGold(cost))
        {
            // ¡COMPRA EXITOSA!
            Debug.Log($"<color=green>COMPRADO: {item.title} por {cost} de oro.</color>");

            // 4. Aplicar Efectos (Usamos la lista de la Fase 4)
            ApplyItemEffects(item);

            // 5. Subir nivel (para que el próximo cueste más o se bloquee)
            itemLevels[item]++;

            return true;
        }
        else
        {
            Debug.Log("<color=red>No tienes suficiente oro.</color>");
            return false;
        }
    }

    private void ApplyItemEffects(ShopItemSO item)
    {
        foreach (var effect in item.effects)
        {
            if (effect != null)
            {
                effect.Apply(player);
            }
        }
    }

    // MÉTODO ACTUALIZADO CON PROBABILIDAD(WEIGHTED RANDOM)
    public List<ShopItemSO> GetRandomItems(int count)
    {
        List<ShopItemSO> validItems = new List<ShopItemSO>();

        // 1. Filtrar ítems válidos (que no estén agotados)
        foreach (var item in allAvailableItems)
        {
            if (item.isOneTimePurchase && GetItemLevel(item) > 0) continue;
            validItems.Add(item);
        }

        List<ShopItemSO> selectedItems = new List<ShopItemSO>();

        // Seguridad: Si hay menos items que huecos, devolvemos todos
        if (validItems.Count <= count) return validItems;

        for (int i = 0; i < count; i++)
        {
            if (validItems.Count == 0) break;

            // ALGORITMO DE RULETA
            int totalWeight = 0;
            foreach (var item in validItems) totalWeight += item.spawnWeight;

            int randomValue = Random.Range(0, totalWeight);
            int currentSum = 0;
            ShopItemSO pickedItem = null;

            foreach (var item in validItems)
            {
                currentSum += item.spawnWeight;
                if (randomValue < currentSum)
                {
                    pickedItem = item;
                    break;
                }
            }

            // Si falló algo (raro), tomamos el último
            if (pickedItem == null) pickedItem = validItems[validItems.Count - 1];

            selectedItems.Add(pickedItem);
            validItems.Remove(pickedItem); // Remover para no repetir en la misma mano
        }

        return selectedItems;
    }

    // ==================================================
    // UTILIDADES (Para la UI más adelante)
    // ==================================================

    public int GetItemLevel(ShopItemSO item)
    {
        if (itemLevels.ContainsKey(item)) return itemLevels[item];
        return 0;
    }

    public int GetCurrentCost(ShopItemSO item)
    {
        return item.GetCost(GetItemLevel(item));
    }

    // ==================================================
    // HERRAMIENTAS DE PRUEBA (Hacker Mode)
    // ==================================================

    // Esto nos permite probar sin UI haciendo click derecho en el componente
    [ContextMenu("Debug: Comprar Primer Item de la Lista")]
    public void DebugBuyFirstItem()
    {
        if (allAvailableItems.Count > 0)
        {
            TryPurchaseItem(allAvailableItems[0]);
        }
    }

    [ContextMenu("Debug: Comprar Segundo Item de la Lista")]
    public void DebugBuySecondItem()
    {
        if (allAvailableItems.Count > 1)
        {
            TryPurchaseItem(allAvailableItems[1]);
        }
    }
}