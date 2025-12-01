using UnityEngine;

[CreateAssetMenu(fileName = "New Shop Item", menuName = "Scriptable Objects/Shop/Item")]
public class ShopItemSO : ScriptableObject //Este objeto permite crear objetos simples (la lista tendria un solo efecto o arma a desbloquear) o objetos complejos para la tienda (posiones con tradeoffs, bundles de armas... todo dependiendo de lo que se inserte en la lista
{
    [Header("Información Visual")]
    public string title;
    [TextArea] public string description;
    public Sprite icon; // Arrastrarás aquí el sprite de tu arma/poción

    [Header("Economía")]
    public int baseCost;
    [Tooltip("Cuánto sube el precio cada vez que lo compras (1.2 = 20% más caro)")]
    public float costMultiplierPerLevel = 1.2f;
    [Tooltip("Probabilidad de aparecer. Alto = Común, Bajo = Raro. Ej: Común=60, Raro=20, Legendario=5")]
    public int spawnWeight = 50; // <--- NUEVO

    [Header("Lógica")]
    [Tooltip("Si es TRUE (armas), solo se puede comprar una vez. Si es FALSE (pociones), es infinito.")]
    public bool isOneTimePurchase = false;

    // AQUÍ ESTÁ LA MAGIA: Una lista de efectos. 
    // Puedes arrastrar aquí tu 'Effect_MoreDamage' o 'Effect_UnlockFireball'
    public UpgradeEffect[] effects;

    // Función matemática para calcular precio actual
    public int GetCost(int currentLevel)
    {
        if (isOneTimePurchase && currentLevel > 0) return 999999; // "Agotado"

        // Fórmula: PrecioBase * (Multiplicador ^ NivelActual)
        // Ej: 100 * (1.2 ^ 2) = 144 oro
        return Mathf.RoundToInt(baseCost * Mathf.Pow(costMultiplierPerLevel, currentLevel));
    }
}