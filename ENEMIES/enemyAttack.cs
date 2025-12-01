using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("References")]
    public CharacterStats stats; // Stats del enemigo
    public PlayerAttackType enemyAttackData; // Datos del ataque (daño, prefab, etc.)
    public Transform attackPoint; // Punto desde donde ataca (hijo del enemigo)
    public LayerMask targetLayer; // Capa del jugador

    // 1. Agregamos el Enum aquí arriba como indicaste
    public enum AttackType
    {
        Melee,
        Ranged, // Proyectil recto (Murciélago)
        Area,   // Onda expansiva en el sitio (Sismo)
        Lobbed  // Proyectil con arco/gravedad (Granada)
    }

    [Header("Attack Behavior")]
    public bool canAttack = true; // Para desactivar ataques (stun, muerte, etc.)
    [Header("Attack Configuration")]
    public AttackType attackType = AttackType.Melee; // Selector de tipo de ataque
    [Header("Range Multipliers")]
    public float meleeRangeMultiplier = 1f;
    public float rangedRangeMultiplier = 1f;
    [Tooltip("Multiplicador de rango para Area y Lobbed")]
    public float areaRangeMultiplier = 1f;

    // Estado interno
    private float attackCooldownTimer = 0f; // Temporizador de cooldown
    private Transform currentTarget; // Asignado por EnemyController

    // Propiedades públicas
    public bool IsInCooldown => attackCooldownTimer > 0f; // Verifica si está en cooldown
    public bool CanAttackNow => canAttack && !IsInCooldown && currentTarget != null; // Verifica si puede atacar

    // Eventos
    public System.Action OnAttackExecuted; // Se dispara cuando ataca
    public System.Action OnAttackHit; // Se dispara cuando el ataque impacta

    void Update()
    {
        // Reducir cooldown
        if (attackCooldownTimer > 0f) // Si está en cooldown
        {
            attackCooldownTimer -= Time.deltaTime; // Reducir temporizador
        }
    }

    // ========== MÉTODOS PRINCIPALES ==========

    public void TryAttack()
    {
        if (!CanAttackNow || stats == null || attackPoint == null) return;

        if (!IsTargetInRange()) return;

        bool attackExecuted = false;

        switch (attackType)
        {
            case AttackType.Melee:
                attackExecuted = ExecuteMeleeAttack();
                break;

            case AttackType.Ranged:
            case AttackType.Lobbed: // Ambos usan lógica de proyectil (la física la decide el Prefab/Data)
                attackExecuted = ExecuteProjectileAttack();
                break;

            case AttackType.Area:
                attackExecuted = ExecuteAreaAttack();
                break;
        }

        if (attackExecuted)
        {
            if (AudioManager.Instance != null)
            {
                // Preferir sonido específico del ataque (proyectil), si no, el genérico del enemigo (si lo tuviera en stats)
                AudioClip clip = (enemyAttackData != null) ? enemyAttackData.launchSound : null;
                // Si stats tuviera attackSound, lo usaríamos como fallback: : stats.attackSound;

                if (clip != null)
                {
                    AudioManager.Instance.PlaySoundAtPosition(clip, attackPoint.position);
                }
            }

            float cooldown = (enemyAttackData != null) ? enemyAttackData.cooldown : stats.attackCooldown;
            attackCooldownTimer = cooldown;
            OnAttackExecuted?.Invoke();
        }
    }

    // ========== TIPOS DE ATAQUE ==========

    private bool ExecuteMeleeAttack()
    {
        float actualRange = stats.attackRange * meleeRangeMultiplier;
        int damage = (enemyAttackData != null) ? enemyAttackData.damage : stats.damage;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, actualRange, targetLayer);

        bool hitSomething = false;

        foreach (Collider2D hit in hits)
        {
            CharacterHealth health = hit.GetComponent<CharacterHealth>();
            if (health != null)
            {
                health.TakeDamage(damage);
                hitSomething = true;
                OnAttackHit?.Invoke();

                if (enemyAttackData != null && enemyAttackData.hasHitReaction)
                {
                    // Aplicar knockback al jugador (si implementamos PlayerController.OnKnockback)
                    // O simplemente dejar que el daño sea el feedback principal por ahora
                }
            }
        }

        return true; // Siempre se ejecuta (aunque no golpee nada)
    }

    private bool ExecuteProjectileAttack()
    {
        if (enemyAttackData == null || enemyAttackData.projectilePrefab == null)
        {
            Debug.LogError($"{name}: Intenta ataque Proyectil/Lobbed pero falta configuración.");
            return false;
        }

        // Calcular dirección hacia el objetivo
        Vector2 direction = (currentTarget.position - attackPoint.position).normalized;

        // Instanciar
        GameObject projObj = Instantiate(enemyAttackData.projectilePrefab, attackPoint.position, Quaternion.identity);

        // Configurar
        ProjectileController proj = projObj.GetComponent<ProjectileController>();
        if (proj != null)
        {
            // Initialize se encarga de ver si es Lobbed (Gravedad) o Ranged (Recto) 
            // basándose en el attackBehavior del ScriptableObject
            proj.Initialize(enemyAttackData, direction, targetLayer);
        }

        return true;
    }

    private bool ExecuteAreaAttack()
    {
        if (enemyAttackData == null || enemyAttackData.projectilePrefab == null)
        {
            Debug.LogError($"{name}: Intenta ataque Area pero falta configuración.");
            return false;
        }

        // Instanciar en el punto de ataque (generalmente los pies o centro del enemigo)
        // Sin rotación (Identity) porque es una expansión radial
        GameObject areaObj = Instantiate(enemyAttackData.projectilePrefab, attackPoint.position, Quaternion.identity);

        ProjectileController proj = areaObj.GetComponent<ProjectileController>();
        if (proj != null)
        {
            // La dirección no importa para Area, pasamos Zero
            proj.Initialize(enemyAttackData, Vector2.zero, targetLayer);
        }

        return true;
    }

    // ========== MÉTODOS AUXILIARES ==========

    private bool IsTargetInRange()
    {
        if (currentTarget == null || stats == null) return false;
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget.position);
        return distanceToTarget <= GetAttackRange();
    }

    public void SetTarget(Transform target) => currentTarget = target;
    public void SetCanAttack(bool value) => canAttack = value;
    public void ResetCooldown() => attackCooldownTimer = 0f;

    public float GetAttackRange()
    {
        if (stats == null) return 0f;

        switch (attackType)
        {
            case AttackType.Melee:
                return stats.attackRange * meleeRangeMultiplier;

            case AttackType.Ranged:
            case AttackType.Lobbed:
                // Rango para disparar o lanzar
                if (enemyAttackData != null) return enemyAttackData.range * rangedRangeMultiplier;
                return stats.attackRange * rangedRangeMultiplier;

            case AttackType.Area:
                // Rango para detonar el sismo (usualmente cuando el jugador está cerca)
                // Podría ser el tamaño final del área o un poco menos para asegurar el golpe
                if (enemyAttackData != null) return enemyAttackData.range * areaRangeMultiplier;
                return stats.attackRange * areaRangeMultiplier;

            default:
                return stats.attackRange;
        }
    }


    // ========== DEBUG ========== BORRAR AL FINALIZAR

    private void OnDrawGizmosSelected()
    {
        if (stats == null || attackPoint == null) return;

        // Cambiar color según tipo de ataque para identificarlo fácil en el editor
        switch (attackType)
        {
            case AttackType.Melee:
                Gizmos.color = Color.red;
                break;
            case AttackType.Ranged:
                Gizmos.color = Color.cyan; // Azul cian para distancia
                break;
        }

        Gizmos.DrawWireSphere(attackPoint.position, GetAttackRange());

        // Línea hacia el target si está en rango
        if (currentTarget != null && IsTargetInRange())
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, currentTarget.position);
        }
    }
}