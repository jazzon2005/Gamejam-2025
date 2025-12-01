using UnityEngine;

[CreateAssetMenu(fileName = "NewStatUpgrade", menuName = "Upgrades/Effects/Stat Buff")]
public class StatBuffUpgrade : UpgradeEffect
{
    public enum StatType
    {
        // Básicos
        MaxHealth,
        Damage,
        MoveSpeed,
        AttackCooldown,

        // Avanzados (PlayerStats)
        CritChance,
        CritMultiplier,
        MoneyMultiplier,

        // Stamina & Defensa
        MaxStamina,
        StaminaRegen,
        BlockDefense, // Fuerza de bloqueo
        BlockCost,    // Costo de stamina al bloquear (queremos reducirlo, así que amount negativo es bueno)
        DashCost,     // Costo de dash

        // Utilidad
        PickupRange,
        Revives
    }

    [Header("Configuración")]
    public StatType statToUpgrade;

    [Tooltip("Valor a aumentar. Si usas porcentaje, 0.1 es 10%. Para reducir costos (BlockCost), usa valores negativos.")]
    public float amount;

    [Tooltip("Si es TRUE, 'amount' se trata como un porcentaje del valor actual. Si es FALSE, es una suma plana.")]
    public bool usePercentage = false;

    public override void Apply(GameObject target)
    {
        PlayerController player = target.GetComponent<PlayerController>();

        if (player == null || player.stats == null) return;

        PlayerStats pStats = player.stats as PlayerStats;
        // Si pStats es null, significa que el jugador tiene CharacterStats básicos (enemigo?)
        // Solo aplicamos mejoras básicas si pStats es null.

        switch (statToUpgrade)
        {
            // --- CharacterStats (Básicos) ---
            case StatType.MaxHealth:
                int hpInc = usePercentage ? Mathf.RoundToInt(player.stats.maxHealth * amount) : Mathf.RoundToInt(amount);
                player.stats.maxHealth += hpInc;
                target.GetComponent<CharacterHealth>()?.Heal(hpInc);
                break;

            case StatType.Damage:
                int dmgInc = usePercentage ? Mathf.RoundToInt(player.stats.damage * amount) : Mathf.RoundToInt(amount);
                player.stats.damage += dmgInc;
                break;

            case StatType.MoveSpeed:
                float spdInc = usePercentage ? player.stats.moveSpeed * amount : amount;
                player.stats.moveSpeed += spdInc;
                break;

            case StatType.AttackCooldown:
                float cdDec = usePercentage ? player.stats.attackCooldown * amount : amount;
                // Restamos porque menos cooldown es mejor
                player.stats.attackCooldown = Mathf.Max(0.05f, player.stats.attackCooldown - cdDec);
                break;

            // --- PlayerStats (Avanzados) ---
            // Solo se ejecutan si pStats no es null

            case StatType.CritChance:
                if (pStats) pStats.critChance += amount; // Siempre suma plana para probabilidad (5% + 5%)
                break;

            case StatType.CritMultiplier:
                if (pStats) pStats.critMultiplier += amount;
                break;

            case StatType.MoneyMultiplier:
                if (pStats) pStats.moneyMultiplier += amount;
                break;

            // --- Stamina ---
            case StatType.MaxStamina:
                if (pStats)
                {
                    float stamInc = usePercentage ? pStats.maxStamina * amount : amount;
                    pStats.maxStamina += stamInc;
                }
                break;

            case StatType.StaminaRegen:
                if (pStats)
                {
                    float regInc = usePercentage ? pStats.staminaRegenRate * amount : amount;
                    pStats.staminaRegenRate += regInc;
                }
                break;

            case StatType.DashCost:
                if (pStats)
                {
                    // Reducir costo es bueno. Si amount es positivo (ej: 0.1 para 10% menos), restamos.
                    // Si amount es negativo (ej: -5), sumamos. Asumamos que el designer pone positivo para "Mejorar".
                    float dashDec = usePercentage ? pStats.dashStaminaCost * amount : amount;
                    pStats.dashStaminaCost = Mathf.Max(0, pStats.dashStaminaCost - dashDec);
                }
                break;

            // --- Defensa ---
            case StatType.BlockDefense:
                if (pStats)
                {
                    int blkInc = usePercentage ? Mathf.RoundToInt(pStats.blockDefense * amount) : Mathf.RoundToInt(amount);
                    pStats.blockDefense += blkInc;
                }
                break;

            case StatType.BlockCost:
                if (pStats)
                {
                    float blkCostDec = usePercentage ? pStats.blockStaminaCost * amount : amount;
                    pStats.blockStaminaCost = Mathf.Max(0, pStats.blockStaminaCost - blkCostDec);
                }
                break;

            // --- Utilidad ---
            case StatType.PickupRange:
                if (pStats) pStats.pickupRange += amount;
                break;

            case StatType.Revives:
                if (pStats) pStats.revives += Mathf.RoundToInt(amount);
                break;
        }

        Debug.Log($"UPGRADE APLICADO: {statToUpgrade} {(amount > 0 ? "+" : "")}{amount}{(usePercentage ? "%" : "")}");
    }
}