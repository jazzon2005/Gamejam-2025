using UnityEngine;
using System.Collections;
using System.Collections.Generic; // <--- ESTA LIBRERÍA FALTABA Y CAUSABA EL ERROR

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public CharacterStats stats; // Características del jugador (velocidad, etc.)
    public float jumpForce = 3f; // Fuerza de salto del jugador
    public float normalHeight = 1f; // Altura normal del jugador
    public float crouchHeight = 0.5f; // Altura agachada del jugador

    [Header("Ground Detection")]
    public Transform groundCheck; // Referencia al objeto que verifica si el jugador está en el suelo
    public float groundCheckRadius = 0.2f; // Radio del círculo para verificar el suelo
    public LayerMask Ground; // Capa que representa el suelo

    // Componentes privados
    private Rigidbody2D rb2d;
    private BoxCollider2D boxCollider2D;
    private Vector2 normalOffset;
    private Vector2 normalSize;

    // Estados internos
    private bool isGrounded;
    private bool isFastFalling;
    private bool isCrouching;
    private bool canMove = true;

    // Propiedades públicas
    public bool IsGrounded => isGrounded;
    public bool IsCrouching => isCrouching;
    public bool IsFastFalling => isFastFalling;

    [Header("Dash Settings")]
    public float dashSpeed = 20f;      // Velocidad del impulso
    public float dashDuration = 0.2f;  // Cuánto dura el impulso
    public float dashCooldown = 1f;    // Tiempo de espera entre dashes

    [Header("Fast Fall/Vertical Dash Attack")]
    [Tooltip("Daño base del Dash Vertical. Se puede ignorar si solo es knockback.")]
    public int fastFallDamage = 5;
    public float fastFallKnockbackForce = 10f; // Fuerza de empuje
    public float fastFallHitRadius = 0.5f; // Radio de detección del ataque
    public LayerMask enemyLayer; // Capa de los enemigos (debes arrastrarla en el Inspector)
    private Coroutine fastFallRoutine;
    public GameObject fastFallImpactPrefab;

    // Estados internos
    private bool isDashing;
    private float lastDashTime;
    private float originalGravityScale; // Para restaurar la gravedad
    // Knockback (NUEVO ESTADO)
    private bool isKnockedBack;
    public bool IsDashing => isDashing;

    private bool wasGrounded; // Para detectar aterrizaje
    private float footstepTimer; // Para ritmo de pasos

    [Header("Dash Visuals (Afterimage)")]
    [Tooltip("Prefab que tiene el SpriteRenderer y el script GhostFader")]
    public GameObject ghostPrefab;
    [Tooltip("Tiempo entre cada fantasma (menor = más denso). Ej: 0.05f")]
    public float ghostSpawnDelay = 0.05f;
    [Tooltip("Opacidad inicial (0 a 1). 0.3 = 30%")]
    [Range(0f, 1f)] public float ghostInitialAlpha = 0.3f;
    [Tooltip("Qué tan rápido se desvanece. Mayor = más rápido.")]
    public float ghostFadeSpeed = 2f;
    [Tooltip("Color del fantasma (déjalo blanco para color original)")]
    public Color ghostColor = Color.white;
    [Tooltip("Cuántos fantasmas reciclar (Performance). Ej: 10")]
    public int ghostPoolSize = 10;

    // Referencias internas para el efecto
    private SpriteRenderer mySpriteRenderer; // Para copiar el sprite actual
    private Queue<GhostFader> ghostPool = new Queue<GhostFader>(); // El pool

    void Start()
    {
        boxCollider2D = GetComponent<BoxCollider2D>();
        rb2d = GetComponent<Rigidbody2D>();

        if (boxCollider2D != null)
        {
            normalSize = boxCollider2D.size;
            normalOffset = boxCollider2D.offset;
        }

        mySpriteRenderer = GetComponent<SpriteRenderer>();
        if (mySpriteRenderer == null) mySpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (mySpriteRenderer == null) Debug.LogWarning("PlayerController: No se encontró SpriteRenderer para el Afterimage.");

        // 2. Inicializar el Pool de Fantasmas
        InitializeGhostPool();
    }

    private void InitializeGhostPool()
    {
        if (ghostPrefab == null) return;

        // Crear un contenedor en la jerarquía para que no estorben
        GameObject poolContainer = new GameObject("GhostPool_Container");

        for (int i = 0; i < ghostPoolSize; i++)
        {
            GameObject obj = Instantiate(ghostPrefab, poolContainer.transform);
            GhostFader fader = obj.GetComponent<GhostFader>();
            if (fader != null)
            {
                obj.SetActive(false);
                ghostPool.Enqueue(fader);
            }
        }
    }

    void FixedUpdate()
    {
        if (groundCheck != null)
        {
            bool isGroundedNow = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, Ground);

            // DETECTAR ATERRIZAJE
            if (isGroundedNow && !wasGrounded)
            {
                PlaySound(stats.landSound, 0.8f);

                // >>> AÑADIR CORRECCIÓN AQUÍ <<<
                if (isFastFalling)
                {
                    isFastFalling = false; // Desactiva el estado de caída rápida
                }
            }

            isGrounded = isGroundedNow;
            wasGrounded = isGrounded;
        }
    }

    // ========== MÉTODOS PÚBLICOS ==========

    // CORRECCIÓN 1: Renombrado de 'Move' a 'HandleMovement' para corregir el error CS1061
    public void HandleMovement(Vector2 direction)
    {
        if (!canMove) return;

        float moveSpeed = stats != null ? stats.moveSpeed : 5f;
        float currentSpeed = moveSpeed;


        if (rb2d != null)
        {
            rb2d.linearVelocity = new Vector2(direction.x * currentSpeed, rb2d.linearVelocity.y);
        }

        if (isCrouching)
        {
            // Frenar gradualmente o en seco
            if (rb2d != null)
            {
                rb2d.linearVelocity = new Vector2(0, rb2d.linearVelocity.y);
            }
            return; // Salimos para no aplicar velocidad de movimiento
        }

        // SONIDO DE PASOS
        if (isGrounded && direction.x != 0)
        {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0)
            {
                PlayRandomFootstep();
                footstepTimer = stats.footstepRate;
            }
        }
        else
        {
            footstepTimer = 0.1f; // Resetear para que suene apenas camines
        }
    }

    // Mantenemos 'Move' por si algún otro script viejo lo usaba (Compatibilidad)
    public void Move(Vector2 direction) => HandleMovement(direction);

    public void Jump()
    {
        if (!canMove || !isGrounded || isDashing || isKnockedBack || rb2d == null) return;
        rb2d.linearVelocity = new Vector2(rb2d.linearVelocity.x, jumpForce);
        // SONIDO SALTO
        PlaySound(stats.jumpSound);
    }

    // CORRECCIÓN 2: Añadido 'CutJump' que redirige a 'CancelJump' porque el PlayerController lo llama así
    public void CutJump()
    {
        CancelJump();
    }

    public void CancelJump()
    {
        if (!canMove || rb2d == null || rb2d.linearVelocity.y <= 0) return; // Solo cancela si está subiendo

        rb2d.linearVelocity = new Vector2(rb2d.linearVelocity.x, jumpForce * 0.5f);
    }

    public void FastFall()
    {
        if(!isGrounded){
            StartCoroutine(FastFallRoutine());
        }
    }

    private IEnumerator FastFallRoutine()
    {
        isFastFalling = true;

        // Guardar la gravedad original antes de anularla
        originalGravityScale = rb2d.gravityScale;

        // 1. Anular gravedad y aplicar velocidad de dash
        rb2d.gravityScale = 0f;
        // Aplicamos la velocidad de Dash hacia abajo, usando dashSpeed para que se sienta como un dash
        rb2d.linearVelocity = new Vector2(rb2d.linearVelocity.x, -dashSpeed);

        // 2. Ejecutar el efecto visual (ya usa isFastFalling)
        if (ghostPrefab != null)
        {
            StartCoroutine(DashGhostRoutine());
        }

        

        // SONIDO DASH
        PlaySound(stats.dashSound);

        // 4. Duración del "dash" vertical
        // Mantenemos el estado activo hasta que pase el tiempo O aterrice.
        float startTime = Time.time;
        while (Time.time < startTime + dashDuration && !isGrounded)
        {
            // Forzamos la velocidad descendente mientras dura el dash
            rb2d.linearVelocity = new Vector2(rb2d.linearVelocity.x, -dashSpeed);
            yield return null;
        }

        // 5. Restaurar física y estado
        rb2d.gravityScale = originalGravityScale;
        isFastFalling = false;
        // 3. DETECCIÓN DE IMPACTO (Ocurre al inicio)
        PerformFastFallAttack();
    }

    private void PerformFastFallAttack()
    {
        // El punto de impacto es el centro del jugador
        Vector2 impactPosition = transform.position;
        if (fastFallImpactPrefab != null)
        {
            // Instanciamos el efecto en la posición actual del jugador
            GameObject impactFX = Instantiate(
                fastFallImpactPrefab,
                impactPosition,
                Quaternion.identity // Sin rotación
            );

            Destroy(impactFX, 1.5f); 
        }

        PlaySound(stats.fastFallSound);

        // Detección de enemigos cercanos
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(impactPosition, fastFallHitRadius, enemyLayer);

        foreach (Collider2D hit in hitEnemies)
        {
            // Asume que el enemigo tiene un componente 'EnemyController' con el método OnHitByPlayer
            EnemyController enemyController = hit.GetComponent<EnemyController>();

            if (enemyController != null)
            {
                // Creamos un ScriptableObject temporal para pasar los parámetros del ataque.
                // Esto es similar a cómo se maneja el Dash horizontal en PlayerController.
                PlayerAttackType fastFallAttack = ScriptableObject.CreateInstance<PlayerAttackType>();
                fastFallAttack.damage = fastFallDamage;
                fastFallAttack.knockbackForce = fastFallKnockbackForce;

                // Un alto valor para 'upwardForce' empuja a los enemigos hacia ARRIBA y hacia AFUERA.
                // Esto es clave para separarlos al caer sobre ellos.
                fastFallAttack.knockbackUpwardForce = 0.9f;
                fastFallAttack.hitStunDuration = 0.2f;

                // Llamar al método de impacto del enemigo
                // Esto asumirá que el enemigo aplica el knockback desde el punto de impacto.
                enemyController.OnHitByPlayer(impactPosition, fastFallAttack);

                // IMPORTANTE: Eliminar la instancia temporal para evitar fugas de memoria.
                Destroy(fastFallAttack);
            }
        }
    }

    public void StartCrouch()
    {
        if (!canMove || !isGrounded || isCrouching || isDashing || boxCollider2D == null) return;
        isCrouching = true;
        float heightDifference = normalSize.y - crouchHeight;
        boxCollider2D.size = new Vector2(normalSize.x, crouchHeight);
        boxCollider2D.offset = new Vector2(normalOffset.x, normalOffset.y - heightDifference / 2f);
    }

    public void StopCrouch()
    {
        if (!isCrouching || boxCollider2D == null) return;

        isCrouching = false;
        boxCollider2D.size = normalSize;
        boxCollider2D.offset = normalOffset;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
        if (!value && rb2d != null) rb2d.linearVelocity = new Vector2(0, rb2d.linearVelocity.y);
    }

    // ========== NUEVA LÓGICA DE DASH ==========

    private IEnumerator DashGhostRoutine()
    {
        // Mientras estemos en estado de Dash...
        while ((isDashing || isFastFalling) && mySpriteRenderer != null)
        {
            SpawnGhost();
            // Esperar un poquito antes de poner el siguiente
            yield return new WaitForSeconds(ghostSpawnDelay);
        }
    }

    private void SpawnGhost()
    {
        if (ghostPool.Count == 0) return; // Seguridad

        // 1. Sacar un fantasma dormido de la fila
        GhostFader ghost = ghostPool.Dequeue();

        // 2. Configurarlo y activarlo
        ghost.Setup(
            mySpriteRenderer.sprite,   // Copiar sprite actual
            mySpriteRenderer.flipX,    // Copiar dirección
            transform.position,        // Posición actual
            transform.rotation,        // Rotación actual
            ghostInitialAlpha,         // Opacidad 30%
            ghostFadeSpeed,            // Velocidad de desvanecimiento
            ghostColor                 // Tinte
        );

        // 3. Devolverlo al final de la fila para reutilizarlo luego
        ghostPool.Enqueue(ghost);
    }

    public bool CanDash()
    {
        return Time.time >= lastDashTime + dashCooldown && !isDashing;
    }

    public void PerformDash(Vector2 direction)
    {
        if (!CanDash()) return;

        StartCoroutine(DashRoutine(direction));
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        isDashing = true;
        lastDashTime = Time.time;

        // SONIDO DASH
        PlaySound(stats.dashSound);

        // --- NUEVO: Iniciar el efecto visual ---
        if (ghostPrefab != null)
        {
            StartCoroutine(DashGhostRoutine());
        }

        originalGravityScale = rb2d.gravityScale;
        rb2d.gravityScale = 0f;


        Vector2 dashDir = direction;
        if (dashDir.magnitude == 0)
        {
            dashDir = new Vector2(transform.localScale.x, 0).normalized;
        }
        else
        {
            dashDir = new Vector2(direction.x, 0).normalized; // Forzar horizontal
        }

        // 3. Aplicar velocidad constante
        rb2d.linearVelocity = dashDir * dashSpeed;

        // Opcional: Efecto visual (Trail Renderer, Partículas) aquí

        yield return new WaitForSeconds(dashDuration);

        // 4. Restaurar físicas
        rb2d.gravityScale = originalGravityScale;
        rb2d.linearVelocity = Vector2.zero; // Frenar al final (opcional, da control)
        isDashing = false;
    }

    // ========== DEBUG ==========

    // --- HELPERS DE AUDIO ---
    private void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySoundAtPosition(clip, transform.position, volume);
        }
    }

    private void PlayRandomFootstep()
    {
        if (stats.footstepSounds != null && stats.footstepSounds.Length > 0)
        {
            int index = Random.Range(0, stats.footstepSounds.Length);
            PlaySound(stats.footstepSounds[index], 0.6f); // Volumen más bajo para pasos
        }
    }

    // ========== LÓGICA DE KNOCKBACK (RECIBIR EMPUJE) ==========
    public void ApplyKnockback(Vector2 force, float duration)
    {
        if (rb2d == null) return;
        StartCoroutine(KnockbackRoutine(force, duration));
    }

    private IEnumerator KnockbackRoutine(Vector2 force, float duration)
    {
        isKnockedBack = true;

        // Resetear velocidad para que el impacto sea seco y claro
        rb2d.linearVelocity = Vector2.zero;

        // Aplicar fuerza impulsiva
        rb2d.AddForce(force, ForceMode2D.Impulse);

        yield return new WaitForSeconds(duration);

        isKnockedBack = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        // --- NUEVO: Visualización del radio de ataque Fast Fall ---
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, fastFallHitRadius);
    }
}