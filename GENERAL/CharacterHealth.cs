using UnityEngine;
using System.Collections;

public class CharacterHealth : MonoBehaviour
{
    public CharacterStats stats;

    [Header("Death Settings")]
    [Tooltip("Tiempo que espera antes de desaparecer")]
    public float deathDelay = 1.5f;

    [Tooltip("Si es true, destruye/desactiva el objeto automáticamente.")]
    public bool autoDespawn = true;

    public int currentHealth;
    public System.Action OnDeath;
    public System.Action OnHit; // <--- NUEVO: Evento genérico al recibir daño

    public bool IsDead { get; private set; }

    // Referencia opcional para detectar bloqueo
    private PlayerMovement playerMovement;

    void Awake()
    {
        // Intentamos obtener PlayerMovement por si este es el jugador
        playerMovement = GetComponent<PlayerMovement>();
    }

    void OnEnable()
    {
        ResetHealth();
    }

    public void ResetHealth()
    {
        IsDead = false;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        if (stats == null)
        {
            currentHealth = 1;
            return;
        }
        currentHealth = stats.maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        // --- LÓGICA DE BLOQUEO CON STAMINA ---
        if (playerMovement != null && playerMovement.IsCrouching)
        {
            PlayerController pc = GetComponent<PlayerController>();

            // Costo de bloqueo desde stats
            float blockCost = (pc != null && pc.stats != null) ? pc.stats.blockStaminaCost : 10f;

            // Intentar consumir stamina para bloquear
            bool canBlock = true;
            if (pc != null)
            {
                // Si tienes energía, gastas y bloqueas. Si no, te pegan full.
                canBlock = pc.TryConsumeStamina(blockCost);
            }

            if (canBlock)
            {
                int damageBlocked = stats != null ? stats.blockDefense : 0;
                int reducedDamage = Mathf.Max(0, amount - damageBlocked);

                if (reducedDamage == 0)
                {
                    // SONIDO BLOQUEO
                    if (AudioManager.Instance != null && stats.blockSound != null)
                        AudioManager.Instance.PlaySoundAtPosition(stats.blockSound, transform.position);
                    return;
                }
                else                {
                    
                    amount = reducedDamage; // Bloqueo parcial
                }
            }
            else
            {
                Debug.Log("¡GUARDIA ROTA! (Sin energía)");
                // Opcional: Stun extra por romper guardia
                if (pc != null) pc.OnStun(0.5f);
            }
        }

        // --- FEEDBACK JUGADOR: CAMERA SHAKE (NUEVO) ---
        // Verificamos si somos el jugador (tenemos PlayerController o PlayerMovement)
        if (GetComponent<PlayerController>() != null)
        {
            // Buscamos la cámara (optimización: podrías cachearla en Start o usar GameManager.Instance.cameraFollow)
            CameraFollow cam = FindFirstObjectByType<CameraFollow>();
            if (cam != null)
            {
                // Sacudida pequeña: 0.2 segundos, 0.3 magnitud
                cam.TriggerShake(0.1f, 0.1f);
            }
        }
        // ---------------------------------

        if (AudioManager.Instance != null && stats.hurtSound != null)
        {
            AudioManager.Instance.PlaySoundAtPosition(stats.hurtSound, transform.position);
        }

        currentHealth -= amount;
        OnHit?.Invoke(); 

        // Feedback visual
        EnemyController enemyController = GetComponent<EnemyController>();
        if (enemyController != null)
        {
            enemyController.OnTakeDamage();
        }

        // Si es el jugador, quizás quieras un feedback visual diferente (pantalla roja)
        // pero eso iría en PlayerController o HUD

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;

        OnDeath?.Invoke();

        // SONIDO MUERTE
        if (AudioManager.Instance != null && stats.deathSound != null)
        {
            AudioManager.Instance.PlaySoundAtPosition(stats.deathSound, transform.position);
        }

        DisableEnemyLogic();

        if (autoDespawn)
        {
            StartCoroutine(DeathRoutine());
        }
    }

    private void DisableEnemyLogic()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        EnemyController ec = GetComponent<EnemyController>();
        if (ec != null) ec.enabled = false;

        // Si es jugador, deshabilitar control también
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false; // O usar pc.SetCanControl(false)
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(deathDelay);
        gameObject.SetActive(false);
    }

    public void Heal(int amount)
    {
        if (IsDead) return;
        currentHealth = Mathf.Min(currentHealth + amount, stats.maxHealth);
    }
}