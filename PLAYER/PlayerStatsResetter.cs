using UnityEngine;

// Struct para guardar los datos del jugador
[System.Serializable]
public struct PlayerStatsBackup
{
    public int maxHealth;
    public int damage;
    public float moveSpeed;
    public float attackCooldown;

    public float maxStamina;
    public float staminaRegenRate;
    public float dashStaminaCost;
    public int blockDefense;
    public float blockStaminaCost;

    public float moneyMultiplier;
    public float critChance;
    public float critMultiplier;

    public float pickupRange;
    public int revives;
}

public class PlayerStatsResetter : MonoBehaviour
{
    [Header("Reference")]
    public PlayerStats stats; // Arrastra aquí tu ScriptableObject PlayerStats

    // Backup estático (Sobrevive al reinicio de escena)
    private static PlayerStatsBackup backup;
    private static bool hasBackup = false;

    void Awake()
    {
        if (stats == null) return;

        if (!hasBackup)
        {
            // PRIMERA VEZ: Guardar Backup
            backup = new PlayerStatsBackup
            {
                maxHealth = stats.maxHealth,
                damage = stats.damage,
                moveSpeed = stats.moveSpeed,
                attackCooldown = stats.attackCooldown,

                maxStamina = stats.maxStamina,
                staminaRegenRate = stats.staminaRegenRate,
                dashStaminaCost = stats.dashStaminaCost,
                blockDefense = stats.blockDefense,
                blockStaminaCost = stats.blockStaminaCost,

                moneyMultiplier = stats.moneyMultiplier,
                critChance = stats.critChance,
                critMultiplier = stats.critMultiplier,

                pickupRange = stats.pickupRange,
                revives = stats.revives
            };
            hasBackup = true;
            Debug.Log("PlayerStats: Backup creado.");
        }
        else
        {
            // REINICIO: Restaurar Backup
            RestoreStats();
        }
    }

    private void RestoreStats()
    {
        stats.maxHealth = backup.maxHealth;
        stats.damage = backup.damage;
        stats.moveSpeed = backup.moveSpeed;
        stats.attackCooldown = backup.attackCooldown;

        stats.maxStamina = backup.maxStamina;
        stats.staminaRegenRate = backup.staminaRegenRate;
        stats.dashStaminaCost = backup.dashStaminaCost;
        stats.blockDefense = backup.blockDefense;
        stats.blockStaminaCost = backup.blockStaminaCost;

        stats.moneyMultiplier = backup.moneyMultiplier;
        stats.critChance = backup.critChance;
        stats.critMultiplier = backup.critMultiplier;

        stats.pickupRange = backup.pickupRange;
        stats.revives = backup.revives;

        Debug.Log("PlayerStats: Stats restaurados al original.");
    }

    // Opcional: Llamar a esto manualmente si necesitas resetear a mitad de partida
    public void ForceReset()
    {
        if (hasBackup) RestoreStats();
    }
}