using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public CharacterStats stats; // Stats del jugador (opcional, por si lo necesitas aquí)

    // Componentes
    private PlayerInput input; // Referencia al script de input
    private PlayerMovement movement; // Referencia al script de movimiento
    private PlayerAttack attack; // Referencia al script de ataque
    private CharacterHealth health; // Referencia al script de salud

    // Estado actual
    private PlayerState currentState = PlayerState.Normal; // Estado en el que se encuentra el jugador
    private bool canControl = true; // Controla si el jugador puede controlar al personaje

    // --- SISTEMA DE STAMINA ---
    private float currentStamina;
    public float CurrentStamina => currentStamina; // Para el HUD

    // Propiedades públicas
    public PlayerState CurrentState => currentState; // Permite leer el estado actual desde otros scripts
    public bool CanControl => canControl; // Permite leer si puede controlar desde otros scripts

    [Header("Dash Combat")]
    [Tooltip("Daño que hace al atravesar enemigos con dash")]
    public int dashDamage = 5;
    [Tooltip("Fuerza con la que empuja a los enemigos")]
    public float dashKnockbackForce = 15f;

    void Start()
    {
        input = GetComponent<PlayerInput>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<CharacterHealth>();

        if (movement != null && stats != null) movement.stats = stats;

        // Inicializar Stamina
        if (stats != null) currentStamina = stats.maxStamina;
    }

    void Update()
    {
        if (health != null && health.currentHealth <= 0)
        {
            ChangeState(PlayerState.Dead);
            return;
        }

        // --- REGENERACIÓN DE STAMINA ---
        if (stats != null && currentState != PlayerState.Dead)
        {
            // Solo regenerar si NO estamos agachados (bloqueando)
            bool isBlocking = movement != null && movement.IsCrouching;

            if (!isBlocking && currentStamina < stats.maxStamina)
            {
                currentStamina += stats.staminaRegenRate * Time.deltaTime;
                if (currentStamina > stats.maxStamina) currentStamina = stats.maxStamina;
            }
        }

        switch (currentState)
        {
            case PlayerState.Normal:
                HandleInput();
                break;

            case PlayerState.Attacking:
                if (!attack.IsInCooldown) ChangeState(PlayerState.Normal);
                break;

            case PlayerState.Stunned:
                // No hacer nada, esperar a que termine el stun
                break;

            case PlayerState.Dashing:
                if (!movement.IsDashing)
                {
                    ChangeState(PlayerState.Normal);
                }
                break;

            case PlayerState.Dead:
                canControl = false;
                movement.SetCanMove(false);
                break;
        }
    }

    // --- MÉTODO PARA CONSUMIR STAMINA ---
    public bool TryConsumeStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            return true;
        }
        return false; // No hay suficiente energía
    }

    private void HandleInput()
    {
        if (!canControl) return;

        // 1. Movimiento
        if (movement != null)
        {
            movement.HandleMovement(input.MoveInput);

            if (input.JumpPressed) movement.Jump();
            if (input.JumpReleased) movement.CancelJump();

            if (input.CrouchPressed) movement.StartCrouch();
            if (input.CrouchReleased) movement.StopCrouch();

            if (input.DownPressed && movement.CanDash())
            {
                // Verificar si tenemos energía suficiente
                float cost = 10;

                if (TryConsumeStamina(cost))
                {
                    movement.FastFall();
                }
                else
                {
                    Debug.Log("¡Sin energía para Dash!");
                    // Aquí podrías poner un sonido de error o flash en la barra
                }
            }


            // --- DASH CON COSTE DE STAMINA ---
            if (input.DashPressed && movement.CanDash())
            {
                // Verificar si tenemos energía suficiente
                float cost = (stats != null) ? stats.dashStaminaCost : 0;

                if (TryConsumeStamina(cost))
                {
                    movement.PerformDash(input.MoveInput);
                    ChangeState(PlayerState.Dashing);
                }
                else
                {
                    Debug.Log("¡Sin energía para Dash!");
                    // Aquí podrías poner un sonido de error o flash en la barra
                }
            }
        }

        // 2. Combate (Lógica Actualizada)
        if (attack != null)
        {
            // Cambio de Arma (Rueda del Ratón)
            if (input.WeaponScroll > 0f) attack.CycleWeapon(-1); // Arriba -> Arma anterior
            if (input.WeaponScroll < 0f) attack.CycleWeapon(1);  // Abajo -> Arma siguiente

            // Ataque Principal (Click Izquierdo) -> Usa el arma equipada
            if (input.PrimaryFirePressed)
            {
                attack.TryCurrentWeapon();
            }

            // Ataque Melee Rápido (Tecla E) -> Siempre disponible
            if (input.QuickMeleePressed)
            {
                attack.TryQuickMelee();
            }
        }
    }

    // --- NUEVO: RECIBIR KNOCKBACK ---
    public void OnKnockback(Vector2 force, float duration)
    {
        if (currentState == PlayerState.Dead) return;

        // Aplicar la física
        if (movement != null)
        {
            movement.ApplyKnockback(force, duration);
        }

        OnStun(duration); // Stunear al jugador durante el knockback
    }

    // ========== CAMBIO DE ESTADOS ==========

    public void ChangeState(PlayerState newState)
    {
        if (currentState == newState) return;
        currentState = newState;

        switch (newState)
        {
            case PlayerState.Normal:
                canControl = true;
                if (movement != null) movement.SetCanMove(true);
                break;

            case PlayerState.Stunned:
                canControl = false;
                if (movement != null) movement.SetCanMove(false);
                break;

            case PlayerState.Dashing:
                // Durante el dash, quizás quieras ser invulnerable (opcional)
                // canControl = false; // Ya lo manejamos en Update ignorando inputs
                break;
        }
    }


    public void OnStun(float duration)
    {
        if (currentState == PlayerState.Dead) return;

        ChangeState(PlayerState.Stunned);
        Invoke(nameof(RecoverFromStun), duration);
    }

    private void RecoverFromStun()
    {
        if (currentState == PlayerState.Stunned)
        {
            ChangeState(PlayerState.Normal);
        }
    }

    public void Heal(int amount)
    {
        if (health != null) health.Heal(amount);
    }

    // ========== EVENTOS ==========

    private void HandleDeath()
    {
        Debug.Log($"{name}: Murió");
        ChangeState(PlayerState.Dead); // Cambiar a estado muerto
    }

    private void HandleAttackExecuted()
    {
        // ANIMACIÓN: animation.PlayAttack();
        Debug.Log($"{name}: Ejecutó ataque");
    }

    private void HandleAttackHit()
    {
        // ANIMACIÓN/SONIDO: Efecto de impacto
        Debug.Log($"{name}: ¡Impactó enemigo!");
    }

    // ========== MÉTODOS PÚBLICOS ==========

    public void SetCanControl(bool value)
    {
        canControl = value;

        if (!value)
        {
            input.ClearInputs(); // Limpiar inputs si se desactiva el control
        }
    }

    // Para stunear/congelar jugador
    public void Stun(float duration)
    {
        ChangeState(PlayerState.Stunned); // Cambiar a estado stuneado
        Invoke(nameof(EndStun), duration); // Llamar a EndStun después de 'duration' segundos
    }

    private void EndStun()
    {
        if (currentState == PlayerState.Stunned)
        {
            ChangeState(PlayerState.Normal); // Volver a estado normal
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState == PlayerState.Dashing)
        {
            // Verificar si chocamos con un enemigo
            CharacterHealth enemyHealth = collision.gameObject.GetComponent<CharacterHealth>();

            if (enemyHealth != null)
            {
                // 1. Aplicar Daño (Opcional ahora, pero listo para el futuro)
                // enemyHealth.TakeDamage(dashDamage);

                // 2. Aplicar Knockback
                EnemyController enemyController = collision.gameObject.GetComponent<EnemyController>();
                if (enemyController != null)
                {
                    // Crear un "ataque virtual" para pasarle los datos de empuje
                    // O llamar directamente a una función de empuje si la tienes expuesta

                    // Opción rápida reutilizando tu sistema actual:
                    // Crear un PlayerAttackType temporal en código o usar valores fijos
                    PlayerAttackType dashImpact = ScriptableObject.CreateInstance<PlayerAttackType>();
                    dashImpact.knockbackForce = dashKnockbackForce;
                    dashImpact.knockbackUpwardForce = 0.2f; // Un poquito hacia arriba
                    dashImpact.hitStunDuration = 0.2f;

                    enemyController.OnHitByPlayer(transform.position, dashImpact);

                    Debug.Log("¡Dash Impact!");
                }
            }
        }
    }


    // Para aplicar boost de velocidad temporal (power-ups)
    /*public void ApplySpeedBoost(float multiplier, float duration)
    {
        if (stats != null)
        {
            float originalSpeed = stats.moveSpeed;
            stats.moveSpeed *= multiplier;

            // Restaurar velocidad después de la duración
            Invoke(() => {
                stats.moveSpeed = originalSpeed;
                Debug.Log($"{name}: Speed boost terminado");
            }, duration);

            Debug.Log($"{name}: Speed boost aplicado x{multiplier} por {duration}s");
        }
    }*/
}

// ========== ENUMS ==========

public enum PlayerState
{
    Normal,    // Estado normal, puede moverse y atacar libremente
    Attacking, // Ejecutando ataque (opcional, si quieres bloquear movimiento)
    Stunned,   // Stuneado, no puede moverse ni atacar
    Dead,       // Muerto, no puede hacer nada
    Dashing // <--- NUEVO ESTADO
}