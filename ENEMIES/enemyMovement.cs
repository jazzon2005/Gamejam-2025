using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    [Header("References")]
    public CharacterStats stats;
    private Transform target;

    [Header("Movement Behavior")]
    public MovementType movementType = MovementType.Ground;
    public bool canMove = true;

    [Header("Ground Settings")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    [Header("Flying Settings")]
    public float hoverHeight = 2f;
    public float hoverSmoothness = 2f;

    [Header("Stopping Behavior")]
    public bool stopWhenInRange = true;
    public float decelerationSpeed = 5f;

    [Header("Physics Settings")]
    [Tooltip("Masa del enemigo (mayor = más difícil de empujar)")]
    public float enemyMass = 1f; // Reducido de 100 a 3-5 para knockback realista
    [Tooltip("Resistencia contra empujes del jugador")]
    public float pushResistance = 50f; // Reducido de 500 a 50

    // Componentes
    private Rigidbody2D rb2d;
    private bool isGrounded;
    private Vector2 moveDirection;
    private bool isBeingKnockedBack = false;

    // Estados públicos
    private bool isMoving;
    public bool IsMoving => isMoving;
    public Vector2 MoveDirection => moveDirection;
    public bool IsGrounded => isGrounded;

    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();

        // Configurar Rigidbody2D como Dynamic
        rb2d.bodyType = RigidbodyType2D.Dynamic;
        rb2d.mass = enemyMass;
        rb2d.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb2d.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb2d.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb2d.linearDamping = 0.5f; // Fricción para que el knockback frene naturalmente

        // Configurar gravedad según tipo
        if (movementType == MovementType.Flying)
        {
            rb2d.gravityScale = 0f;
        }
        else
        {
            rb2d.gravityScale = 2f; // Gravedad normal
        }
    }

    void Update()
    {
        // Verificar si está en el suelo
        if (groundCheck != null && movementType == MovementType.Ground)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
    }

    void FixedUpdate()
    {
        // Si está en knockback, no aplicar movimiento normal
        if (isBeingKnockedBack)
        {
            isMoving = false;
            return;
        }

        // Si no puede moverse, frenar INSTANTÁNEAMENTE
        if (!canMove)
        {
            isMoving = false;

            if (movementType == MovementType.Flying)
            {
                rb2d.linearVelocity = Vector2.zero;
            }
            else
            {
                rb2d.linearVelocity = new Vector2(0, rb2d.linearVelocity.y);
            }

            return;
        }

        // Si no tiene target o stats, frenar CON INERCIA
        if (target == null || stats == null)
        {
            isMoving = false;
            FrenarConInercia();
            return;
        }

        // Calcular dirección hacia el objetivo
        moveDirection = (target.position - transform.position).normalized;
        float distanceToTarget = Vector2.Distance(transform.position, target.position);

        // Verificar si debe detenerse
        float attackRange = stats.attackRange;
        if (stopWhenInRange && distanceToTarget <= attackRange)
        {
            isMoving = false;
            FrenarConInercia();
            return;
        }

        isMoving = true;

        // Ejecutar movimiento según tipo
        switch (movementType)
        {
            case MovementType.Ground:
                MoveGround();
                break;

            case MovementType.Flying:
                MoveFlying();
                break;
        }

        // Resistir empujes no intencionales (solo cuando NO está en knockback)
        ResistUnintendedPush();
    }

    // ========== TIPOS DE MOVIMIENTO ==========

    private void MoveGround()
    {
        float horizontalMove = moveDirection.x * stats.moveSpeed;
        rb2d.linearVelocity = new Vector2(horizontalMove, rb2d.linearVelocity.y);
    }

    private void MoveFlying()
    {
        Vector2 targetPosition = target.position;
        targetPosition.y = Mathf.Max(targetPosition.y, target.position.y + hoverHeight);

        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        Vector2 newVelocity = direction * stats.moveSpeed;

        rb2d.linearVelocity = Vector2.Lerp(rb2d.linearVelocity, newVelocity, Time.fixedDeltaTime * hoverSmoothness);
    }

    // ========== RESISTENCIA A EMPUJES ==========

    private void ResistUnintendedPush()
    {
        // Solo aplicar si el enemigo está moviéndose intencionalmente
        if (!isMoving || isBeingKnockedBack) return;

        // Solo para enemigos terrestres en el suelo
        if (movementType == MovementType.Ground && !isGrounded) return;

        // Calcular la velocidad intencional
        float intendedSpeedX = moveDirection.x * stats.moveSpeed;
        float currentSpeedX = rb2d.linearVelocity.x;

        // Si hay una diferencia significativa (está siendo empujado)
        float speedDifference = Mathf.Abs(currentSpeedX - intendedSpeedX);

        if (speedDifference > 0.5f)
        {
            // Aplicar fuerza correctiva para mantener la velocidad intencional
            float correctionForce = (intendedSpeedX - currentSpeedX) * pushResistance * Time.fixedDeltaTime;
            rb2d.AddForce(new Vector2(correctionForce, 0), ForceMode2D.Force);
        }
    }

    // ========== FRENADO ==========

    private void FrenarConInercia()
    {
        if (movementType == MovementType.Flying)
        {
            rb2d.linearVelocity = Vector2.Lerp(rb2d.linearVelocity, Vector2.zero, Time.fixedDeltaTime * decelerationSpeed);
        }
        else
        {
            float currentSpeedX = rb2d.linearVelocity.x;
            float newSpeedX = Mathf.Lerp(currentSpeedX, 0f, Time.fixedDeltaTime * decelerationSpeed);
            rb2d.linearVelocity = new Vector2(newSpeedX, rb2d.linearVelocity.y);
        }

        if (rb2d.linearVelocity.magnitude < 0.1f)
        {
            if (movementType == MovementType.Flying)
            {
                rb2d.linearVelocity = Vector2.zero;
            }
            else
            {
                rb2d.linearVelocity = new Vector2(0, rb2d.linearVelocity.y);
            }
        }
    }

    // ========== MÉTODOS PÚBLICOS ==========

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
    }

    public void ForceLookAtTarget()
    {
        if (target != null && moveDirection.x != 0)
        {
            Vector3 scale = transform.localScale;
            scale.x = moveDirection.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }

    // ========== REEMPLAZA ESTE MÉTODO EN TU EnemyMovement.cs ==========

    /// <summary>
    /// Aplica knockback físico real usando fuerzas del Rigidbody2D
    /// </summary>
    public void ApplyPhysicalKnockback(Vector2 attackerPosition, float knockbackForce, float upwardForce, float stunDuration)
    {
        if (rb2d == null)
        {
            Debug.LogError($"{name}: rb2d es NULL!");
            return;
        }

        // Verificar que el Rigidbody sea Dynamic
        if (rb2d.bodyType != RigidbodyType2D.Dynamic)
        {
            Debug.LogError($"{name}: Rigidbody2D NO es Dynamic! Es: {rb2d.bodyType}");
            return;
        }

        // Calcular dirección del knockback (alejar del atacante) - SOLO HORIZONTAL
        Vector2 horizontalDirection = ((Vector2)transform.position - attackerPosition);
        horizontalDirection.y = 0; // Ignorar diferencia vertical
        horizontalDirection.Normalize();

        // Si la dirección es cero (atacante justo encima/debajo), usar dirección aleatoria
        if (horizontalDirection.magnitude < 0.1f)
        {
            horizontalDirection = transform.position.x > attackerPosition.x ? Vector2.right : Vector2.left;
        }

        // Construir fuerza final
        Vector2 knockbackDirection;

        if (movementType == MovementType.Ground)
        {
            // Para enemigos terrestres: fuerza horizontal + componente vertical
            float verticalComponent = knockbackForce * upwardForce;
            knockbackDirection = new Vector2(
                horizontalDirection.x * knockbackForce * 100,
                verticalComponent * 100 //ESto esta mal, arreglelo paivaaaaaaaaaa, pero no tengo tiempo ahora, no funcioan con los voladores...
            );
        }
        else
        {
            // Para enemigos voladores: solo empuje horizontal
            knockbackDirection = horizontalDirection * knockbackForce;
        }

        // Cancelar velocidad actual antes de aplicar knockback
        rb2d.linearVelocity = Vector2.zero;

        // Aplicar knockback como impulso
        rb2d.AddForce(knockbackDirection, ForceMode2D.Impulse);

        Debug.Log($"{name}: Knockback aplicado - Fuerza: {knockbackForce}, Dirección: {knockbackDirection}, Velocidad resultante: {rb2d.linearVelocity}");

        // Marcar que está en knockback
        if (stunDuration > 0)
        {
            StartCoroutine(KnockbackDuration(stunDuration));
        }
    }

    private System.Collections.IEnumerator KnockbackDuration(float duration)
    {
        isBeingKnockedBack = true;

        yield return new WaitForSeconds(duration);

        isBeingKnockedBack = false;
    }

    // ========== DEBUG ==========

    private void OnDrawGizmosSelected()
    {
        if (stats == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, stats.attackRange);

        if (groundCheck != null && movementType == MovementType.Ground)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}

public enum MovementType
{
    Ground,
    Flying
}