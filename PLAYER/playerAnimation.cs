using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public SpriteRenderer spriteRenderer;

    [Header("Animator Parameters")]
    // Triggers (Acciones de un solo uso)
    public string jumpTrigger = "Jump";
    public string dashTrigger = "Dash";
    public string attackTrigger = "Attack";
    public string hurtTrigger = "Hurt";
    public string dieTrigger = "Die"; // <--- CAMBIO: Trigger para muerte

    // Booleans/Floats (Estados continuos)
    public string speedParam = "Speed";
    public string verticalSpeedParam = "VerticalSpeed";
    public string isGroundedParam = "IsGrounded";
    public string isCrouchingParam = "IsCrouching"; // Asegúrate que este nombre coincide con tu Animator
    public string isFastFallingParam = "IsFastFalling";

    [Header("Visual Effects")]
    public bool enableHitFlash = true;
    public Color hitFlashColor = Color.red;
    public float hitFlashDuration = 0.1f;

    // Referencias a sistemas
    private PlayerController controller;
    private PlayerMovement movement;
    private PlayerAttack attack;
    private CharacterHealth health;
    private Rigidbody2D rb;
    private Camera mainCam;

    // Memoria de estado
    private bool wasGrounded;
    private bool wasDashing;
    private bool isDead; // Para evitar disparar el trigger multiples veces

    // Hashes
    private int _jumpHash;
    private int _dashHash;
    private int _attackHash;
    private int _hurtHash;
    private int _speedHash;
    private int _vSpeedHash;
    private int _groundedHash;
    private int _crouchHash; // Hash para crouch
    private int _dieHash; // <--- Hash para trigger muerte
    private int _fastFallHash;
    // Hashes nuevos
    private int _weaponIDHash;
    private int _attackIndexHash;

    private bool isFlashing = false;
    private Color originalColor;

    void Awake()
    {
        controller = GetComponent<PlayerController>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        health = GetComponent<CharacterHealth>();
        rb = GetComponent<Rigidbody2D>();

        if (animator == null) animator = GetComponent<Animator>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        mainCam = Camera.main;
        if (spriteRenderer != null) originalColor = spriteRenderer.color;

        // Inicializar Hashes
        _jumpHash = Animator.StringToHash(jumpTrigger);
        _dashHash = Animator.StringToHash(dashTrigger);
        _attackHash = Animator.StringToHash(attackTrigger);
        _hurtHash = Animator.StringToHash(hurtTrigger);
        _speedHash = Animator.StringToHash(speedParam);
        _vSpeedHash = Animator.StringToHash(verticalSpeedParam);
        _groundedHash = Animator.StringToHash(isGroundedParam);
        _crouchHash = Animator.StringToHash(isCrouchingParam); // Inicializar hash
        _dieHash = Animator.StringToHash(dieTrigger); // <--- Hash nuevo
        _fastFallHash = Animator.StringToHash(isFastFallingParam);
        // Inicializar nuevos hashes
        _weaponIDHash = Animator.StringToHash("WeaponID");
        _attackIndexHash = Animator.StringToHash("AttackIndex");
    }

    void OnEnable()
    {
        if (attack != null) attack.OnAttackExecuted += PlayAttackAnim;
        if (health != null) health.OnHit += PlayHurtAnim;
    }

    void OnDisable()
    {
        if (attack != null) attack.OnAttackExecuted -= PlayAttackAnim;
        if (health != null) health.OnHit -= PlayHurtAnim;
    }

    void Update()
    {
        if (animator == null || movement == null) return;

        // SI ESTÁ MUERTO, SOLO EJECUTAR MUERTE Y SALIR
        if (controller != null && controller.CurrentState == PlayerState.Dead)
        {
            if (!isDead)
            {
                isDead = true;
                animator.SetTrigger(_dieHash); // Disparar animación una única vez
            }
            return; // Dejar de actualizar movimiento/flip si está muerto
        }

        // 1. MOVIMIENTO HORIZONTAL
        float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat(_speedHash, currentSpeed);

        // 2. MOVIMIENTO VERTICAL
        float vSpeed = rb.linearVelocity.y;
        animator.SetFloat(_vSpeedHash, vSpeed);
        animator.SetBool(_groundedHash, movement.IsGrounded);

        // 3. CROUCH (ESTADO CONTINUO) - CORREGIDO
        // Se actualiza en cada frame basado en el estado real del movimiento
        animator.SetBool(_crouchHash, movement.IsCrouching);

        // 4. FAST FALL (ESTADO CONTINUO) - CORREGIDO
        bool isFastFalling = !movement.IsGrounded && vSpeed < -12f;
        animator.SetBool(_fastFallHash, isFastFalling);

        // 5. TRIGGERS (Acciones puntuales)

        // Salto
        if (wasGrounded && !movement.IsGrounded && vSpeed > 0.1f)
        {
            animator.SetTrigger(_jumpHash);
        }
        wasGrounded = movement.IsGrounded;

        // Dash
        if (!wasDashing && movement.IsDashing)
        {
            animator.SetTrigger(_dashHash);
        }
        wasDashing = movement.IsDashing;

        // 7. FLIP
        HandleSpriteFlip();
    }

    private void HandleSpriteFlip()
    {
        if (spriteRenderer == null || mainCam == null || (controller != null && controller.CurrentState == PlayerState.Dead))
            return;

        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        bool shouldFlip = mousePos.x < transform.position.x;
        if (spriteRenderer.flipX != shouldFlip)
        {
            spriteRenderer.flipX = shouldFlip;
        }
    }

    private void PlayAttackAnim()
    {
        if (attack != null && attack.CurrentEquippedWeapon != null)
        {
            // 1. Decirle al Animator qué arma tenemos
            int weaponID = attack.CurrentEquippedWeapon.weaponAnimationID;
            animator.SetInteger(_weaponIDHash, weaponID);

            // 2. Variación Random (Solo para Melee / ID 0)
            // Si quisieras variaciones para otras armas, quitas el 'if'
            if (weaponID == 0)
            {
                // Elige entre 0 y 1 (asumiendo que tienes 2 animaciones)
                // Si tienes 3, pon Random.Range(0, 3)
                int randomVariant = Random.Range(0, 2);
                animator.SetInteger(_attackIndexHash, randomVariant);
            }
        }

        // 3. Disparar
        animator.SetTrigger(_attackHash);
    }

    // Este método se debe llamar en el último frame de la animación de ataque o habilidad
    public void FinishAnimationTrigger()
    {
        // Reseteamos triggers manualmente por seguridad
        animator.ResetTrigger(_attackHash);

        // Si usas un bool para bloquear movimiento (como isAttacking), aquí lo apagarías
        if (controller != null && controller.CurrentState == PlayerState.Attacking)
        {
            controller.ChangeState(PlayerState.Normal);
        }
    }

    private void PlayHurtAnim()
    {
        animator.SetTrigger(_hurtHash);
        if (enableHitFlash && spriteRenderer != null) StartCoroutine(HitFlashRoutine());
    }

    private System.Collections.IEnumerator HitFlashRoutine()
    {
        if (isFlashing) yield break;
        isFlashing = true;
        spriteRenderer.color = hitFlashColor;
        yield return new WaitForSeconds(hitFlashDuration);
        spriteRenderer.color = originalColor;
        isFlashing = false;
    }
}