using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStats", menuName = "Scriptable Objects/Character Stats")]
public class CharacterStats : ScriptableObject
{
    public string characterName = "Character";

    [Header("Audio")]
    public AudioClip hurtSound;   // Al recibir daño
    public AudioClip deathSound;  // Al morir
    public AudioClip blockSound;  // Al bloquear ataque (Jugador)

    [Header("Base Stats")]
    public int maxHealth = 20;
    public int damage = 5;
    public float moveSpeed = 2f;

    [Header("Stamina System")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 15f; // Cuánta energía recupera por segundo
    public float dashStaminaCost = 25f;
    public float blockStaminaCost = 10f; // Costo por golpe bloqueado

    [Header("Combat")]
    public float attackRange = 1f;
    public float attackCooldown = 1.2f;

    [Header("AI Only (opcional)")]
    public float detectionRadius = 5f;

    [Header("Rewards (NUEVO)")]
    [Tooltip("Puntos de Score que da al morir")]
    public int scoreValue = 10;
    [Tooltip("Cantidad de oro que suelta al morir")]
    public int goldDrop = 5;

    [Header("Movement Audio (Jugador)")]
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip dashSound;
    public AudioClip fastFallSound;
    public AudioClip[] footstepSounds; // Array para variedad en pasos
    public float footstepRate = 0.4f; // Tiempo entre pasos

    [Header("Defense")]
    [Tooltip("Cantidad de daño que bloquea al cubrirse (0 = nada, 100 = invencible, o valor fijo de daño)")]
    // Por simplicidad, usaremos esto como "Daño plano reducido" o "Porcentaje".
    // Para tu petición de "cubrirse en su totalidad", asumiremos que si blockPower >= daño enemigo, no recibe daño.
    public int blockDefense = 1000; // Valor alto por defecto para bloqueo total inicial
}