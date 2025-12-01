using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    public CharacterStats stats; //Aquí se asigna en el inspector el scriptable object de stats del enemigo

    [Header("Behavior Type")]
    public EnemyBehaviorType behaviorType = EnemyBehaviorType.Aggressive; //Estado en el comportamiento del enemigo, agresivo por defecto

    [Header("Patrol Settings (Optional)")]
    public Transform[] patrolPoints; // Puntos de patrulla, deben ser empty game objects en la escena
    public float patrolWaitTime = 2f; // Tiempo de espera en cada punto
    private int currentPatrolIndex = 0; //Punto de patrulla actual, inicia en el 0 (primer punto en la lista)
    private float patrolWaitTimer = 0f; //Tiempo de espera en el punto que se encuentre

    // Componentes
    private EnemyMovement movement; // Referencia al script de movimiento
    private EnemyDetection detection; // Referencia al script de detección
    private EnemyAttack attack; // Referencia al script de ataque
    private CharacterHealth health; // Referencia al script de salud
    private EnemyAnimation animation; // Referencia al script de animaciones, poner cuando esté listo

    // Estado actual
    private EnemyState currentState = EnemyState.Idle; //Estado en el que se encuentra el enemigo, inicia en idle

    // Propiedades públicas
    public EnemyState CurrentState => currentState; // Permite leer el estado actual desde otros scripts

    void Start()
    {
        // Obtener componentes
        movement = GetComponent<EnemyMovement>(); // Obtener componente de movimiento desde el script EnemyMovement
        detection = GetComponent<EnemyDetection>(); // Obtener componente de deteccion desde el script EnemyDetection
        attack = GetComponent<EnemyAttack>(); // Obtener componente de ataque desde el script EnemyAttack
        health = GetComponent<CharacterHealth>(); // Obtener componente de salud desde el script EnemyHealth
        animation = GetComponent<EnemyAnimation>();

        // Validar componentes requeridos
        if (movement == null) Debug.LogError($"{name}: Falta componente EnemyMovement"); //Verifica que los componentes existan y estén adjuntos al script
        if (detection == null) Debug.LogError($"{name}: Falta componente EnemyDetection");
        if (attack == null) Debug.LogError($"{name}: Falta componente EnemyAttack");

        // Suscribirse a eventos de detección
        if (detection != null)
        {
            detection.OnPlayerDetected += HandlePlayerDetected; //Esto es un evento y esta detectando cuando el jugador es detectado
            detection.OnPlayerLost += HandlePlayerLost; //Eto es un evento y esta detectando cuando el jugador se pierde de vista
        }

        // Suscribirse a evento de muerte
        if (health != null)
        {
            health.OnDeath += HandleDeath; //Evento que detecta cuando el enemigo muere
        }

        // Suscribirse a eventos de ataque para animaciones
        if (attack != null)
        {
            attack.OnAttackExecuted += HandleAttackExecuted; //Evento que detecta cuando el enemigo ejecuta un ataque
            attack.OnAttackHit += HandleAttackHit; //Evento que detecta cuando el ataque del enemigo impacta
        }

        // Inicializar según tipo de comportamiento
        InitializeBehavior();
    }

    void Update()
    {
        // Ejecutar lógica según estado actual
        switch (currentState)
        {
            case EnemyState.Idle: //Caso para el idle
                UpdateIdle(); //Llama al metodo UpdateIdle
                break;

            case EnemyState.Patrol: //Caso para el patrullaje
                UpdatePatrol(); //Llama al metodo UpdatePatrol
                break;

            case EnemyState.Chase: //Caso para la persecución
                UpdateChase(); //Llama al metodo UpdateChase
                break;

            case EnemyState.Attack: //Caso para el ataque
                UpdateAttack(); //Llama al metodo UpdateAttack
                break;

            case EnemyState.Dead:
                // No hace nada, está muerto, deberian incluirse efectos de muerte aquí o drops aqui
                break;
        }
    }

    // ========== INICIALIZACIÓN ==========

    private void InitializeBehavior() //Inicializa el comportamiento del enemigo segun el tipo seleccionado
    {
        switch (behaviorType) //Contienen todos los comportamientos, se pueden agregar mas si es necesario (es una IA chiquita)
        {
            case EnemyBehaviorType.Aggressive:
                // Busca al jugador desde el inicio (sin necesidad de estar en rango de detección)
                detection.SetCanDetect(true);

                // Buscar al jugador inmediatamente por tag
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    attack.SetTarget(player.transform); // Asignar target al ataque
                    movement.SetTarget(player.transform); // Asignar target al movimiento
                    ChangeState(EnemyState.Chase); // Ir directo a perseguir
                }
                else
                {
                    Debug.LogWarning($"{name}: Enemigo agresivo pero no se encontró jugador con tag 'Player'");
                    ChangeState(EnemyState.Idle); // Quedarse quieto si no hay jugador
                }
                break;

            case EnemyBehaviorType.Patrol:
                // Empieza a patrullar
                if (patrolPoints != null && patrolPoints.Length > 0) //Si existen puntos de patrulla
                {
                    ChangeState(EnemyState.Patrol); //Patrulla entre los puntos asignados
                }
                else
                {
                    Debug.LogWarning($"{name}: Patrol seleccionado pero no hay puntos de patrulla. Cambiando a Idle."); //Si no hay puntos de patrulla, advierte y cambia a idle
                    ChangeState(EnemyState.Idle); // Cambia a estado Idle
                }
                break;

            case EnemyBehaviorType.Stationary:
                // Se queda quieto hasta detectar
                movement.SetCanMove(false); // No se mueve
                ChangeState(EnemyState.Idle); // Cambia a estado Idle
                break;

            case EnemyBehaviorType.Sleeping:
                // Dormido, no detecta hasta ser atacado o que el jugador se acerque mucho
                detection.SetCanDetect(false); // No detecta al jugador
                ChangeState(EnemyState.Idle); // Cambia a estado Idle
                break;
        }
    }

    // ========== ESTADOS ==========

    private void UpdateIdle()
    {
        if (animation != null) animation.PlayIdle();
    }

    private void UpdatePatrol() //Lógica para patrullar entre puntos
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return; //Si no hay puntos de patrulla, salir

        Transform targetPoint = patrolPoints[currentPatrolIndex]; //Alterna el punto al que se dirige

        // Moverse hacia el punto de patrulla
        movement.SetTarget(targetPoint);

        if (animation != null) animation.PlayWalk();

        // Verificar si llegó al punto
        float distanceToPoint = Vector2.Distance(transform.position, targetPoint.position);

        if (distanceToPoint <= 0.5f) // Llegó al punto
        {
            patrolWaitTimer += Time.deltaTime; // Incrementar temporizador de espera

            movement.SetTarget(null); // Detenerse (con inercia)
            if (animation != null) animation.PlayIdle();

            if (patrolWaitTimer >= patrolWaitTime) //Al exceder el tiempo de espera preestablecido entre puntos
            {
                // Ir al siguiente punto
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length; //Recorre la lista de puntos de patrulla pasando al siguiente en esta
                patrolWaitTimer = 0f; //Reiniciar temporizador
            }
        }
        else
        {
            if (animation != null) animation.PlayWalk();
        }
    }

    private void UpdateChase() //Lógica para perseguir al jugador
    {
        // El target ya está asignado por detection, solo verificar distancia
        if (detection.HasTarget)
        {
            // IMPORTANTE: Actualizar el target continuamente mientras persigue
            movement.SetTarget(detection.CurrentTarget);
            if (animation != null) animation.PlayChase();

            float distanceToTarget = Vector2.Distance(transform.position, detection.CurrentTarget.position); //Calcula la distancia al objetivo

            // ANIMACIÓN: animation.PlayChase();

            // Si está en rango de ataque, cambiar a estado Attack
            if (distanceToTarget <= attack.GetAttackRange())
            {
                ChangeState(EnemyState.Attack); //Cambia al estado de ataque
            }
        }
        else
        {
            // Si no tiene target (enemigo agresivo que perdió el jugador), buscarlo de nuevo
            if (behaviorType == EnemyBehaviorType.Aggressive)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    attack.SetTarget(player.transform);
                    movement.SetTarget(player.transform);
                }
            }
        }
    }

    private void UpdateAttack() //Lógica para atacar al jugador
    {
        // Verificar si sigue en rango
        if (detection.HasTarget)
        {
            float distanceToTarget = Vector2.Distance(transform.position, detection.CurrentTarget.position); //Calcula la distancia al objetivo

            // Si se alejó, volver a perseguir o no lo ha alcanzado
            if (distanceToTarget > attack.GetAttackRange())
            {
                ChangeState(EnemyState.Chase); //Cambia al estado de persecución
                return;
            }

            // Intentar atacar (el script de attack maneja el cooldown)
            attack.TryAttack();

            // ANIMACIÓN: La animación de ataque se maneja en HandleAttackExecuted()
            // porque necesita sincronizarse con el evento OnAttackExecuted
        }
        else
        {
            // Perdió el target, volver al comportamiento inicial
            ReturnToInitialBehavior();
        }
    }

    // ========== CAMBIO DE ESTADOS ==========

    private void ChangeState(EnemyState newState) //Método para cambiar de estado
    {
        if (currentState == newState) return; //Si no existen cambios, salir

        // Salir del estado anterior
        ExitState(currentState);

        // Cambiar estado
        currentState = newState;

        // Entrar al nuevo estado
        EnterState(newState);

        Debug.Log($"{name}: Estado cambiado a {newState}"); //Mensaje de depuración para verificar el cambio de estado, borrar al terminar
    }

    private void EnterState(EnemyState state) //Lógica al entrar en estados específicos
    {
        switch (state) //Contiene la lógica para cada estado
        {
            case EnemyState.Idle: //Caso para el estado idle
                movement.SetTarget(null); // Quitar target (se detiene CON INERCIA en EnemyMovement)
                // NO llamar a SetCanMove(false) aquí - dejamos que frene naturalmente
                // ANIMACIÓN: animation.SetState(EnemyState.Idle);
                break;

            case EnemyState.Patrol: //Caso para el estado patrulla
                movement.SetCanMove(true); // Asegurar que puede moverse para patrullar
                // Ya tiene lógica en UpdatePatrol
                // ANIMACIÓN: animation.SetState(EnemyState.Patrol);
                break;

            case EnemyState.Chase: //Caso para el estado persecución
                if (behaviorType == EnemyBehaviorType.Stationary) // Si es torreta
                {
                    movement.SetCanMove(false); // Las torretas no se mueven
                }
                else
                {
                    movement.SetCanMove(true); // Asegurar que puede moverse
                }
                // ANIMACIÓN: animation.SetState(EnemyState.Chase);
                break;

            case EnemyState.Attack: //Caso para el estado ataque
                movement.SetTarget(null); // Quitar target
                movement.SetCanMove(false); // FRENO INSTANTÁNEO al atacar
                // ANIMACIÓN: animation.SetState(EnemyState.Attack);
                break;

            case EnemyState.Dead: //Caso para el estado muerte
                movement.SetCanMove(false); // FRENO INSTANTÁNEO al morir
                attack.SetCanAttack(false); // Los muertos no atacan
                movement.SetTarget(null); // Quitar target
                // ANIMACIÓN: animation.PlayDeath();
                break;
        }
    }

    private void ExitState(EnemyState state) //Lógica al salir de estados específicos
    {
        // SOLUCIÓN GLOBAL: Limpiar estado anterior antes de cambiar
        // Esto previene bugs de "arrastre" de comportamientos entre estados

        switch (state)
        {
            case EnemyState.Idle:
                // No requiere limpieza especial
                break;

            case EnemyState.Patrol:
                movement.SetTarget(null); // Dejar de ir al punto de patrulla
                patrolWaitTimer = 0f; // Resetear temporizador
                break;

            case EnemyState.Chase:
                movement.SetTarget(null); // Dejar de perseguir
                break;

            case EnemyState.Attack:
                movement.SetCanMove(true); // Reactivar movimiento al salir del ataque
                break;

            case EnemyState.Dead:
                // Los muertos no salen de su estado
                break;
        }
    }

    // ========== EVENTOS ==========

    private void HandlePlayerDetected(Transform player) //Lógica cuando el jugador es detectado
    {
        Debug.Log($"{name}: Jugador detectado!");

        // Asignar target solo al attack (movement lo recibirá en UpdateChase)
        attack.SetTarget(player); //Asignar el objetivo al script de ataque

        // Cambiar a estado de persecución
        ChangeState(EnemyState.Chase);
    }

    private void HandlePlayerLost() //Lógica cuando el jugador se pierde de vista
    {
        Debug.Log($"{name}: Jugador perdido");

        // Quitar targets
        movement.SetTarget(null); // Dejar de perseguir (frena con inercia)
        attack.SetTarget(null); // Dejar de atacar

        // Volver al comportamiento inicial
        ReturnToInitialBehavior();
    }

    private void HandleAttackExecuted() //Lógica cuando el enemigo ejecuta un ataque
    {
        if (animation != null) animation.PlayAttack();
        Debug.Log($"{name}: Ejecutó ataque");
    }

    private void HandleAttackHit() //Lógica cuando el ataque del enemigo impacta
    {
        // ANIMACIÓN: animation.PlayHitEffect(); // Efecto visual cuando golpea
        Debug.Log($"{name}: ¡Impacto!");
    }

    private void HandleDeath() //Lógica cuando el enemigo muere
    {
        Debug.Log($"{name}: Murió");
        ChangeState(EnemyState.Dead); // Cambiar a estado muerto

        // Cuando este enemigo muere, reportamos al GameManager
        if (GameManager.Instance != null && stats != null)
        {
            GameManager.Instance.RegisterEnemyKill(stats.scoreValue, stats.goldDrop);
            Debug.Log($"Enemigo derrotado. Recompensa: {stats.goldDrop} oro");
            // --- NUEVO: TEXTO FLOTANTE ---
            if (FloatingTextManager.Instance != null)
            {
                // Mostrar Oro
                if (stats.goldDrop > 0)
                    FloatingTextManager.Instance.ShowGold(stats.goldDrop, transform.position);

                
                if (stats.scoreValue > 0) { 
                    FloatingTextManager.Instance.ShowScore(stats.scoreValue, transform.position);
                }
            }
        }

        // Animación de muerte
        if (animation != null) animation.PlayDeath();
    }

    // ========== MÉTODOS AUXILIARES ==========

    private void ReturnToInitialBehavior() //Método para volver al comportamiento inicial según el tipo de enemigo
    {
        switch (behaviorType) //Contiene la lógica para cada tipo de comportamiento
        {
            case EnemyBehaviorType.Aggressive: //Ya lo describi varias veces, simplemente itera sobre el comportamiento deseado.
                // Los agresivos siguen buscando al jugador
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    attack.SetTarget(player.transform);
                    movement.SetTarget(player.transform);
                    ChangeState(EnemyState.Chase);
                }
                else
                {
                    ChangeState(EnemyState.Idle);
                }
                break;

            case EnemyBehaviorType.Patrol:
                ChangeState(EnemyState.Patrol);
                break;

            case EnemyBehaviorType.Stationary:
                ChangeState(EnemyState.Idle);
                break;

            case EnemyBehaviorType.Sleeping:
                ChangeState(EnemyState.Idle);
                detection.SetCanDetect(false); // Vuelve a dormir
                break;
        }
    }

    // ========== MÉTODOS PÚBLICOS ==========

    // Para despertar enemigos dormidos (cuando el jugador los ataca)
    public void WakeUp()
    {
        if (behaviorType == EnemyBehaviorType.Sleeping) //Solo si está dormido
        {
            detection.SetCanDetect(true); // Permite detectar al jugador
            detection.ForceDetection(); // Fuerza la detección inmediata del jugador
        }
    }

    // Para stunear/congelar enemigo
    public void Stun(float duration)
    {
        movement.SetCanMove(false); // Detener movimiento (FRENO INSTANTÁNEO)
        attack.SetCanAttack(false); // Detener ataque

        Invoke(nameof(EndStun), duration); // Llama a EndStun después de 'duration' segundos
    }

    private void EndStun() // Termina el estado de stun
    {
        if (currentState != EnemyState.Dead) // No reactivar si está muerto
        {
            movement.SetCanMove(true); // Reactivar movimiento
            attack.SetCanAttack(true); // Reactivar ataque
        }
    }

    // ========== NUEVOS MÉTODOS DE REACCIÓN (HIT Y DAÑO) ==========

    /// <summary>
    /// El enemigo maneja su propia reacción cuando es golpeado por el jugador
    /// </summary>
    public void OnHitByPlayer(Vector2 attackerPosition, PlayerAttackType attackType)
    {
        if (currentState == EnemyState.Dead) return;

        // 1. Despertar si está dormido
        if (behaviorType == EnemyBehaviorType.Sleeping)
        {
            WakeUp();
        }

        // 2. Aplicar stun
        if (attackType.hitStunDuration > 0)
        {
            Stun(attackType.hitStunDuration);
        }

        // 3. Aplicar knockback físico
        if (movement != null)
        {
            movement.ApplyPhysicalKnockback(
                attackerPosition,
                attackType.knockbackForce,
                attackType.knockbackUpwardForce,
                attackType.hitStunDuration
            );
        }
    }

    public void OnTakeDamage()
    {
        // Efecto visual de golpe
        if (animation != null)
        {
            animation.PlayHitBlink(); // O usa PlayHitBlink() para parpadeo
        }

        // Despertar si está dormido
        if (behaviorType == EnemyBehaviorType.Sleeping)
        {
            WakeUp();
        }
    }
}

// ========== ENUMS ==========

public enum EnemyState //Estados posibles del enemigo
{
    Idle,    // Quieto, esperando
    Patrol,  // Patrullando entre puntos
    Chase,   // Persiguiendo al jugador
    Attack,  // Atacando al jugador
    Dead     // Muerto
    //Agregar más estados si es necesario
}

public enum EnemyBehaviorType //Tipos de comportamiento del enemigo
{
    Aggressive,  // Busca y persigue desde el inicio
    Patrol,      // Patrulla hasta detectar al jugador
    Stationary,  // Torreta/planta, no se mueve pero ataca
    Sleeping     // Dormido hasta ser atacado o que el jugador se acerque mucho
                 //Agregar más tipos si es necesario
}