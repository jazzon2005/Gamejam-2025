using UnityEngine;

public enum PlayerAttackBehavior
{
    Melee,
    Projectile,
    Area,
    Lobbed,
    Zone
}

[CreateAssetMenu(fileName = "PlayerAttack", menuName = "Scriptable Objects/Player/Attack")]
public class PlayerAttackType : ScriptableObject
{
    [Header("Audio")]
    public AudioClip launchSound; // Sonido al disparar
    public AudioClip emptyAmmoSound; // Sonido al intentar disparar sin munición
    public AudioClip hitSound;    // Sonido al impactar (opcional)

    [Header("UI & Info")]
    public string attackName = "Basic Melee";
    public Sprite icon; // <--- ¡ESTO FALTABA! Arrastra aquí el dibujo del arma en Unity

    [Tooltip("ID para el Animator. 0 = Melee, 1 = Pistola, 2 = Magia, etc.")]
    public int weaponAnimationID = 0; // <--- NUEVO CAMPO

    [Header("Attack Settings")]
    public PlayerAttackBehavior attackBehavior = PlayerAttackBehavior.Melee;
    public int damage = 10;
    public float range = 1f;
    public float cooldown = 0.4f;

    [Header("Ammo / Overheat")]
    [Tooltip("Si es TRUE, usa sistema de munición/calor. Si es FALSE, es infinito.")]
    public bool useAmmo = false;
    public float maxAmmo = 100f; // 100% de calor o 10 balas
    public float ammoCostPerShot = 10f;
    public float ammoRegenRate = 20f; // Qué tan rápido se enfría/recarga por segundo

    [Header("Visual Effects (NUEVO)")]
    [Tooltip("Prefab de partículas que aparece al golpear un enemigo (Sangre/Chispas)")]
    public GameObject hitEffectPrefab;
    [Tooltip("Prefab de partículas que aparece al disparar/atacar (Muzzle Flash)")]
    public GameObject muzzleFlashPrefab;

    [Header("Knockback Settings")]
    public bool hasHitReaction = true;
    public float hitStunDuration = 0.15f;
    public float knockbackForce = 10f;
    [Range(0f, 1f)]
    public float knockbackUpwardForce = 0.3f;

    [Header("Projectile / Lobbed Settings")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 10f;
    public float projectileLifetime = 3f;
    public int pierceCount = 0;
    public float throwArc = 5f;
    public GameObject impactPrefab;

    [Header("Area / Zone Settings")]
    public float areaFinalSize = 5f;
    public float areaGrowthSpeed = 10f;
    public bool isDamageOverTime = false;
    public float tickRate = 0.5f;

    [Header("Unlock Settings")]
    public bool unlockedByDefault = true;
    [HideInInspector] public bool unlockedRuntime = false;

    public bool IsUnlocked => unlockedByDefault || unlockedRuntime;
}