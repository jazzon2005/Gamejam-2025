using UnityEngine;

// Cambiamos el menuName para que sea fácil de encontrar
[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "Scriptable Objects/Player Stats")]
public class PlayerStats : CharacterStats // <--- ¡HERENCIA! Hereda todo lo de CharacterStats
{
    [Header("Economy & Luck")]
    [Tooltip("Multiplicador de dinero ganado (1 = 100%, 1.5 = 150%)")]
    public float moneyMultiplier = 1f;

    [Tooltip("Probabilidad de golpe crítico (0 a 1)")]
    [Range(0f, 1f)]
    public float critChance = 0.05f; // 5% base

    [Tooltip("Daño extra al hacer crítico (1.5 = 150% daño total)")]
    public float critMultiplier = 1.5f;

    [Header("Utility")]
    [Tooltip("Rango para recoger experiencia/monedas")]
    public float pickupRange = 3f;

    [Tooltip("Cuántas veces puede revivir por partida")]
    public int revives = 0;

    // Aquí podríamos añadir un método para resetear valores al iniciar partida
    // si queremos que sean "Roguelike" (se pierden al morir) o "RPG" (permanentes)
    public void ResetSessionStats()
    {
        // Valores por defecto para una nueva partida
        moneyMultiplier = 1f;
        critChance = 0.05f;
        // etc...
    }
}