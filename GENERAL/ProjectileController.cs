using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ProjectileController : MonoBehaviour
{
    private PlayerAttackType attackData; // Datos del ataque
    private Vector2 direction;
    private LayerMask targetLayer;
    private int hitCount = 0;
    // NUEVO: Daño actual (local) para poder modificarlo al desviar
    private int currentDamage;
    // Para evitar golpear al mismo enemigo múltiples veces en un frame con el área creciente
    private List<GameObject> hitTargets = new List<GameObject>();
    private float damageTimer = 0f;

    private Rigidbody2D rb2d;
    private SpriteRenderer spriteRenderer; // Referencia visual

    // --- GHOST TRAIL SETTINGS ---
    [Header("Visuals (Ghost Trail)")]
    [Tooltip("Asigna el prefab 'PlayerGhost' aquí si quieres estela")]
    public GameObject ghostPrefab;
    public float ghostSpawnDelay = 0.05f;
    [Range(0f, 1f)] public float ghostAlpha = 0.3f;
    public float ghostFadeSpeed = 3f;
    public Color ghostColor = Color.white;

    // Pool local para este proyectil
    private Queue<GhostFader> ghostPool = new Queue<GhostFader>();
    private GameObject poolContainer; // Contenedor para no ensuciar la jerarquía

    private void Awake()
    {
        // Inicialización de componentes en Awake/Start
        rb2d = GetComponent<Rigidbody2D>();
        // >>> CORRECCIÓN CRUCIAL: Inicializar el SpriteRenderer. <<<
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer == null)
        {
            Debug.LogWarning("ProjectileController: No se encontró SpriteRenderer en el proyectil. La estela de fantasma no funcionará.");
        }
    }

    public void Initialize(PlayerAttackType data, Vector2 dir, LayerMask layer)
    {
        attackData = data;
        direction = dir.normalized;
        targetLayer = layer;
        currentDamage = data.damage;

        // Inicializar Pool de Fantasmas si hay prefab asignado
        if (ghostPrefab != null && spriteRenderer != null)
        {
            InitializeGhostPool();
            StartCoroutine(GhostTrailRoutine());
        }

        // Configuración inicial según comportamiento
        float lifetime = (data.projectileLifetime > 0) ? data.projectileLifetime : 3f;
        Destroy(gameObject, lifetime);

        switch (attackData.attackBehavior)
        {
            case PlayerAttackBehavior.Projectile:
                // Movimiento cinemático (sin gravedad) manejado en Update
                UpdateRotation();
                if (rb2d != null) rb2d.gravityScale = 0;
                break;

            case PlayerAttackBehavior.Lobbed:
                // Movimiento Físico (Con gravedad)
                if (rb2d != null)
                {
                    rb2d.gravityScale = 1f; // Activar gravedad
                    // Aplicar fuerza: Hacia adelante + Hacia arriba (Arco)
                    Vector2 force = (direction * attackData.projectileSpeed) + (Vector2.up * attackData.throwArc);
                    rb2d.AddForce(force, ForceMode2D.Impulse);
                    // Rotación inicial aleatoria para que se vea natural al girar
                    rb2d.AddTorque(Random.Range(-100f, 100f));
                }
                else
                {
                    Debug.LogError("ProjectileController: Ataque Lobbed requiere Rigidbody2D en el prefab.");
                }
                break;

            case PlayerAttackBehavior.Zone:
            case PlayerAttackBehavior.Area:
                // Estático
                if (rb2d != null) rb2d.bodyType = RigidbodyType2D.Static; // No se mueve
                // Activar el timer inmediatamente para el primer tick
                damageTimer = attackData.tickRate;
                break;
        }
    }

    private void InitializeGhostPool()
    {
        // Crear un contenedor temporal en la escena para soltar los fantasmas
        poolContainer = new GameObject($"Ghosts_{gameObject.name}_{GetInstanceID()}");

        // Crear unos 5-10 fantasmas (suficientes para la vida corta de una bala)
        int poolSize = 8;

        for (int i = 0; i < poolSize; i++)
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

    private IEnumerator GhostTrailRoutine()
    {
        while (true)
        {
            SpawnGhost();
            yield return new WaitForSeconds(ghostSpawnDelay);
        }
    }

    private void SpawnGhost()
    {
        if (ghostPool.Count == 0 || spriteRenderer == null) return;

        // Sacar del pool
        GhostFader ghost = ghostPool.Dequeue();

        // IMPORTANTE: Sacar al fantasma del contenedor si el contenedor se mueve con la bala?
        // No, el contenedor es estático en el mundo, así que está bien.

        ghost.Setup(
            spriteRenderer.sprite,
            spriteRenderer.flipX,
            transform.position,
            transform.rotation,
            ghostAlpha,
            ghostFadeSpeed,
            ghostColor
        );

        // Devolver al pool
        ghostPool.Enqueue(ghost);
    }

    // Método para rotar el sprite hacia donde se mueve
    private void UpdateRotation()
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }

    // --- NUEVO: LÓGICA DE DESVÍO (PARRY) ---
    public void Deflect(Vector2 newDirection, LayerMask newTargetLayer, float damageMultiplier)
    {
        // 1. Cambiar dirección (hacia donde apunta el jugador)
        direction = newDirection.normalized;

        // 2. Cambiar bando (ahora lastima a los enemigos, no al jugador)
        targetLayer = newTargetLayer;

        // 3. Modificar daño (ej: 20% extra o reducido)
        currentDamage = Mathf.RoundToInt(currentDamage * damageMultiplier);

        // 4. Resetear tiempo de vida (para que no desaparezca a mitad de camino)
        CancelInvoke(); // Si usaras Invoke para destruir
        Destroy(gameObject, 3f); // Darle 3 segundos extra de vida

        // 5. Actualizar rotación visual
        UpdateRotation();

        // Opcional: Aumentar velocidad o cambiar color
        // GetComponent<SpriteRenderer>().color = Color.yellow;
        Debug.Log("¡Proyectil Desviado!");
    }

    private void OnDestroy()
    {
        // Limpieza: Cuando la bala muere, destruimos su contenedor de fantasmas
        // Le damos un tiempo extra para que los últimos fantasmas terminen de desvanecerse
        if (poolContainer != null)
        {
            Destroy(poolContainer, 1f);
        }
    }

    void Update()
    {
        if (attackData == null) return;

        switch (attackData.attackBehavior)
        {
            case PlayerAttackBehavior.Projectile:
            case PlayerAttackBehavior.Lobbed: // Lobbed también se mueve si no usas física pura, pero con RB se mueve solo
                if (attackData.attackBehavior == PlayerAttackBehavior.Projectile)
                    transform.Translate(Vector3.right * attackData.projectileSpeed * Time.deltaTime);
                break;

            case PlayerAttackBehavior.Area:
            case PlayerAttackBehavior.Zone: // AHORA AMBOS CRECEN

                // Lógica de Crecimiento (Spreading)
                if (transform.localScale.x < attackData.areaFinalSize)
                {
                    float growth = attackData.areaGrowthSpeed * Time.deltaTime;
                    transform.localScale += new Vector3(growth, growth, 0);
                }

                // Lógica de Ciclo de Vida para Ataques Instantáneos (Sismo)
                if (attackData.attackBehavior == PlayerAttackBehavior.Area && !attackData.isDamageOverTime)
                {
                    if (transform.localScale.x >= attackData.areaFinalSize)
                        Destroy(gameObject);
                }

                // Lógica de Daño por Tiempo (Charco/Zona)
                if (attackData.isDamageOverTime)
                {
                    damageTimer += Time.deltaTime;
                    if (damageTimer >= attackData.tickRate)
                    {
                        PulseDamage();
                        damageTimer = 0f;
                    }
                }
                break;
        }
    }

    // Función específica para dañar todo lo que esté dentro del área en ese instante
    private void PulseDamage()
    {
        float radius = transform.localScale.x / 2f;
        // Usamos OverlapCircleAll para dañar todo lo que esté dentro de la zona
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius, targetLayer);

        foreach (var hit in hits)
        {
            ApplyDamageAndEffects(hit);
        }
        // Opcional: Feedback visual del pulso (un pequeño flash o partículas)
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (attackData == null) return;

        // --- GRANADA (LOBBED) ---
        if (attackData.attackBehavior == PlayerAttackBehavior.Lobbed)
        {
            // Chocar con Suelo o Enemigo -> Explotar
            if (((1 << collision.gameObject.layer) & targetLayer) != 0 || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
            {
                SpawnImpactEffect();
                Destroy(gameObject);
                return;
            }
        }

        // --- IMPACTO DIRECTO (PROYECTIL / AREA) ---
        if (((1 << collision.gameObject.layer) & targetLayer) != 0)
        {
            // VFX: IMPACTO (Al chocar con entidad viva)
            if (attackData.hitEffectPrefab != null)
            {
                // Instanciar en el punto de impacto (o posición del proyectil)
                // Usamos la rotación inversa a la dirección para que las chispas salten "hacia atrás"
                Quaternion rot = Quaternion.LookRotation(direction * -1);
                // Nota: LookRotation en 2D puede ser tricky, mejor usar identity o rotación del proyectil
                GameObject vfx = Instantiate(attackData.hitEffectPrefab, transform.position, transform.rotation);
                Destroy(vfx, 2f);
            }

            // Si es zona persistente, el daño lo maneja PulseDamage(), ignoramos TriggerEnter para no duplicar daño
            if (attackData.isDamageOverTime) return;

            // Para Sismo (Area), evitar golpes múltiples en el mismo frame
            if (attackData.attackBehavior == PlayerAttackBehavior.Area)
            {
                if (hitTargets.Contains(collision.gameObject)) return;
                hitTargets.Add(collision.gameObject);
            }

            ApplyDamageAndEffects(collision);

            // Penetración
            if (attackData.attackBehavior == PlayerAttackBehavior.Projectile)
            {
                hitCount++;
                if (attackData.pierceCount >= 0 && hitCount > attackData.pierceCount)
                {
                    Destroy(gameObject);
                }
            }
        }
        else if (attackData.attackBehavior == PlayerAttackBehavior.Projectile && collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }


    private void ApplyDamageAndEffects(Collider2D collision)
    {
        // Búsqueda robusta de componentes
        CharacterHealth health = collision.GetComponent<CharacterHealth>();
        if (health == null) health = collision.GetComponentInParent<CharacterHealth>();
        if (health == null) health = collision.GetComponentInChildren<CharacterHealth>();

        EnemyController enemy = collision.GetComponent<EnemyController>();
        if (enemy == null) enemy = collision.GetComponentInParent<EnemyController>();

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player == null) player = collision.GetComponentInParent<PlayerController>();

        // DAÑO
        if (health != null)
        {
            health.TakeDamage(currentDamage);
        }

        // EFECTOS (Knockback y Stun)
        if (attackData.hasHitReaction)
        {
            Vector2 knockbackOrigin = transform.position;

            // Si es un proyectil dirigido, el empuje viene desde "atrás" del proyectil.
            if (attackData.attackBehavior == PlayerAttackBehavior.Projectile)
            {
                knockbackOrigin = (Vector2)transform.position - (direction * 0.5f);
            }

            // A. Empujar Enemigo
            if (enemy != null)
            {
                enemy.OnHitByPlayer(knockbackOrigin, attackData);
            }

            // B. Empujar Jugador
            if (player != null)
            {
                // Calcular dirección de empuje (Desde el origen del ataque hacia el jugador)
                Vector2 difference = (Vector2)player.transform.position - knockbackOrigin;

                // CORRECCIÓN: Si la diferencia es casi cero (están en el mismo punto), empujar hacia arriba o aleatorio
                if (difference.sqrMagnitude < 0.01f)
                {
                    difference = Vector2.up; // Empuje de emergencia hacia arriba
                }

                Vector2 pushDir = difference.normalized;

                // Calcular fuerza final
                Vector2 finalForce = pushDir * attackData.knockbackForce;

                // Añadir componente vertical
                // Multiplicamos por un factor mayor (ej: 5 o 10) para que se note el levantamiento
                finalForce.y += attackData.knockbackUpwardForce * 5f;

                // Debug para ver si se está calculando fuerza
                // Debug.Log($"Aplicando Knockback al Jugador. Fuerza: {finalForce}");

                // Ejecutar empuje
                player.OnKnockback(finalForce, 0.2f);

                if (attackData.hitStunDuration > 0)
                {
                    player.OnStun(attackData.hitStunDuration);
                }
            }
        }
    }

    private void SpawnImpactEffect()
    {
        if (attackData.impactPrefab != null)
        {
            GameObject impactObj = Instantiate(attackData.impactPrefab, transform.position, Quaternion.identity);

            // HACER QUE NAZCA PEQUEÑO PARA QUE CREZCA (Efecto expansión)
            impactObj.transform.localScale = Vector3.zero;

            ProjectileController zoneController = impactObj.GetComponent<ProjectileController>();
            if (zoneController != null)
            {
                PlayerAttackType zoneData = Instantiate(attackData);
                zoneData.attackBehavior = PlayerAttackBehavior.Zone; // Cambiar a comportamiento de Zona
                zoneData.projectileLifetime = attackData.areaFinalSize; // Usamos esto o una variable nueva para duración

                // La duración real del charco debería ser 'projectileLifetime' del ScriptableObject original
                // Pero aquí estamos reusando la variable. Asegúrate de que en el ataque 'Attack_AcidGrenade', 
                // el 'Projectile Lifetime' sea la duración del charco (ej: 5s).

                zoneController.Initialize(zoneData, Vector2.zero, targetLayer);
            }
        }
    }

}