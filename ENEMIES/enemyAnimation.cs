using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("Animation Parameters")]
    [Tooltip("Nombres de los parámetros del Animator")]
    public string idleParam = "Idle";
    public string walkParam = "Walk";
    public string attackParam = "Attack";
    public string deadParam = "Dead";
    public string speedParam = "Speed"; // Float para velocidad de movimiento

    [Header("Visual Effects")]
    public bool enableHitFlash = true;
    public Color hitFlashColor = Color.white;
    public float hitFlashDuration = 0.1f;

    [Header("Flip Settings")]
    public bool autoFlip = false; // Flipear sprite según dirección de movimiento
    public bool flipX = true; // Dirección del flip (false = derecha es positivo)

    // Componentes
    private EnemyController controller;
    private EnemyMovement movement;
    private CharacterHealth health;

    // Estado interno
    private EnemyState currentAnimState = EnemyState.Idle;
    private bool isFlashing = false;
    private Color originalColor;

    void Start()
    {
        // Obtener componentes
        controller = GetComponent<EnemyController>();
        movement = GetComponent<EnemyMovement>();
        health = GetComponent<CharacterHealth>();

        // Auto-asignar Animator si no está asignado
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Auto-asignar SpriteRenderer si no está asignado
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Guardar color original
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // Suscribirse a eventos si existen los componentes
        if (controller != null)
        {
            // No hay evento público de cambio de estado en EnemyController
            // Lo manejaremos en Update()
        }

        if (health != null)
        {
            health.OnDeath += PlayDeath;
        }

        // Validaciones
        if (animator == null)
        {
            Debug.LogWarning($"{name}: No se encontró Animator. Las animaciones no funcionarán.");
        }

        if (spriteRenderer == null)
        {
            Debug.LogWarning($"{name}: No se encontró SpriteRenderer. Los efectos visuales no funcionarán.");
        }
    }

    void Update()
    {
        if (animator == null || controller == null) return;

        // Actualizar animación según el estado del controller
        UpdateAnimation();

        // Actualizar flip del sprite
        if (autoFlip && movement != null)
        {
            UpdateSpriteFlip();
        }

        // Actualizar parámetro de velocidad
        UpdateSpeedParameter();
    }

    // ========== ACTUALIZACIÓN DE ANIMACIONES ==========

    private void UpdateAnimation()
    {
        EnemyState newState = controller.CurrentState;

        // Solo cambiar animación si el estado cambió
        if (newState != currentAnimState)
        {
            currentAnimState = newState;

            // Cambiar animación según estado
            switch (newState)
            {
                case EnemyState.Idle:
                    PlayIdle();
                    break;

                case EnemyState.Patrol:
                    PlayWalk();
                    break;

                case EnemyState.Chase:
                    PlayChase();
                    break;

                case EnemyState.Attack:
                    PlayAttack();
                    break;

                case EnemyState.Dead:
                    PlayDeath();
                    break;
            }
        }
    }

    private void UpdateSpeedParameter()
    {
        if (movement == null) return;

        // Actualizar parámetro de velocidad (útil para blend trees)
        float speed = movement.IsMoving ? movement.MoveDirection.magnitude : 0f;
        animator.SetFloat(speedParam, speed);
    }

    private void UpdateSpriteFlip()
    {
        if (spriteRenderer == null || movement == null) return;

        // Flipear sprite según dirección de movimiento
        Vector2 moveDir = movement.MoveDirection;

        if (Mathf.Abs(moveDir.x) > 0.01f) // Solo flipear si hay movimiento horizontal significativo
        {
            if (flipX)
            {
                spriteRenderer.flipX = moveDir.x < 0; // Voltear cuando va a la izquierda
            }
            else
            {
                spriteRenderer.flipX = moveDir.x > 0; // Voltear cuando va a la derecha
            }
        }
    }

    // ========== MÉTODOS DE ANIMACIÓN ==========

    public void PlayIdle()
    {
        if (animator == null) return;

        animator.SetBool(walkParam, false);
        animator.SetBool(attackParam, false);
        // Nota: Idle es el estado por defecto, no necesita trigger

        Debug.Log($"{name}: Animación Idle");
    }

    public void PlayWalk()
    {
        if (animator == null) return;

        animator.SetBool(walkParam, true);
        animator.SetBool(attackParam, false);

        Debug.Log($"{name}: Animación Walk");
    }

    public void PlayChase()
    {
        // Chase usa la misma animación que Walk, pero puede ser más rápida
        PlayWalk();
    }

    public void PlayAttack()
    {
        if (animator == null) return;

        animator.SetBool(walkParam, false);
        animator.SetTrigger(attackParam); // Usar Trigger para ataque (one-shot)

        Debug.Log($"{name}: Animación Attack");
    }

    public void PlayDeath()
    {
        if (animator == null) return;

        // Desactivar todas las otras animaciones
        animator.SetBool(walkParam, false);
        animator.SetBool(attackParam, false);

        // Activar animación de muerte
        animator.SetTrigger(deadParam);

        Debug.Log($"{name}: Animación Death");
    }

    // ========== EFECTOS VISUALES ==========

    /// <summary>
    /// Flash blanco cuando recibe daño
    /// </summary>
    public void PlayHitFlash()
    {
        if (!enableHitFlash || isFlashing || spriteRenderer == null) return;

        StartCoroutine(HitFlashCoroutine());
    }

    private System.Collections.IEnumerator HitFlashCoroutine()
    {
        isFlashing = true;

        // Cambiar a color de flash
        spriteRenderer.color = hitFlashColor;

        yield return new WaitForSeconds(hitFlashDuration);

        // Volver al color original
        spriteRenderer.color = originalColor;

        isFlashing = false;
    }

    /// <summary>
    /// Parpadeo al recibir daño (alternativa al flash)
    /// </summary>
    public void PlayHitBlink()
    {
        if (spriteRenderer == null) return;

        StartCoroutine(HitBlinkCoroutine());
    }

    private System.Collections.IEnumerator HitBlinkCoroutine()
    {
        int blinkCount = 3;
        float blinkSpeed = 0.05f;

        for (int i = 0; i < blinkCount; i++)
        {
            spriteRenderer.enabled = false;
            yield return new WaitForSeconds(blinkSpeed);

            spriteRenderer.enabled = true;
            yield return new WaitForSeconds(blinkSpeed);
        }
    }

    // ========== MÉTODOS PÚBLICOS ==========

    /// <summary>
    /// Forzar reproducción de animación específica
    /// </summary>
    public void ForcePlayAnimation(string animationName)
    {
        if (animator == null) return;

        animator.Play(animationName);
    }

    /// <summary>
    /// Cambiar velocidad de animación (útil para slow/fast effects)
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        if (animator == null) return;

        animator.speed = speed;
    }

    /// <summary>
    /// Resetear velocidad de animación a normal
    /// </summary>
    public void ResetAnimationSpeed()
    {
        SetAnimationSpeed(1f);
    }
}