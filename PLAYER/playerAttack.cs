using UnityEngine;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    // Struct simple para guardar los datos originales de un arma
    public struct WeaponStatsBackup
    {
        public int damage;
        public float range;
        public float cooldown;
        public float maxAmmo;
        public float ammoRegenRate;
        public float projectileSpeed;
        public int pierceCount;
    }

    [Header("Attack Types")]
    public PlayerAttackType basicAttack; // Arma inicial / Melee (Índice 0)
    public PlayerAttackType[] specialAttacks; // Armas desbloqueables (Índices 1+)


    [Header("References")]
    public Transform attackPoint;
    public LayerMask enemyLayer;
    private Camera mainCam; // Referencia a la cámara para el mouse
    // NUEVO: Layer de los proyectiles enemigos para poder detectarlos
    public LayerMask projectileLayer;

    // --- BACKUP ESTÁTICO (Sobrevive al reinicio de escena) ---
    // Guarda los valores originales para restaurarlos al reiniciar
    private static Dictionary<PlayerAttackType, WeaponStatsBackup> initialStatsBackup = new Dictionary<PlayerAttackType, WeaponStatsBackup>();

    [Header("Parry Settings")]
    [Tooltip("Multiplicador de daño al devolver (1.2 = 120% daño, 0.2 = 20% daño)")]
    public float deflectDamageMultiplier = 1.5f; // Por defecto lo hacemos más fuerte al devolverlo

    [Header("Weapon Movement")]
    [Tooltip("Distancia del cuerpo a la que flota el arma")]
    public float weaponOrbitRadius = 1.5f;

    [Header("Audio Extra")]
    public AudioClip weaponSwitchSound; // <--- NUEVO

    // Estado interno
    private float attackCooldownTimer = 0f;
    private bool canAttack = true;

    // Índice del arma seleccionada. 0 = BasicAttack, 1+ = SpecialAttacks
    private int currentWeaponIndex = 0;
    // --- SISTEMA DE MUNICIÓN (Diccionario) ---
    // Clave: El ScriptableObject del arma. Valor: Munición actual.
    private Dictionary<PlayerAttackType, float> weaponAmmo = new Dictionary<PlayerAttackType, float>();

    public bool IsInCooldown => attackCooldownTimer > 0f;
    public bool CanAttackNow => canAttack && !IsInCooldown;

    // Propiedad inteligente: Traduce el índice al objeto correcto
    public PlayerAttackType CurrentEquippedWeapon
    {
        get
        {
            if (currentWeaponIndex == 0) return basicAttack;

            int specialIndex = currentWeaponIndex - 1;
            if (specialAttacks != null && specialIndex >= 0 && specialIndex < specialAttacks.Length)
            {
                return specialAttacks[specialIndex];
                Debug.Log($"Arma actual: {specialAttacks[specialIndex].attackName}");
            }
            return basicAttack; // Fallback si algo falla
        }
    }

    // Helper para la UI (Saber munición actual)
    public float CurrentAmmo
    {
        get
        {
            var weapon = CurrentEquippedWeapon;
            if (weapon != null && weaponAmmo.ContainsKey(weapon)) return weaponAmmo[weapon];
            return 0;
        }
    }

    // Eventos
    public System.Action OnAttackExecuted;
    public System.Action OnAttackHit;
    public System.Action<PlayerAttackType> OnWeaponChanged;

    void Start()
    {
        mainCam = Camera.main; // Obtener cámara principal

        // 1. Gestionar Backup y Reseteo al nacer
        HandleWeaponBackupAndReset(basicAttack);
        if (specialAttacks != null)
        {
            foreach (var attack in specialAttacks)
            {
                HandleWeaponBackupAndReset(attack);
            }
        }

        InitializeWeapon(basicAttack);
        // Bloquear armas especiales al inicio
        if (specialAttacks != null)
        {
            foreach (var attack in specialAttacks)
            {
                if (attack != null) attack.unlockedRuntime = false;
            }
        }

        // Empezar siempre con el arma básica
        currentWeaponIndex = 0;
    }

    // --- LÓGICA DE BACKUP Y RESETEO ---
    private void HandleWeaponBackupAndReset(PlayerAttackType weapon)
    {
        if (weapon == null) return;

        // Si NO tenemos backup de este arma, significa que es la PRIMERA VEZ que jugamos.
        // Guardamos sus valores actuales como "Originales".
        if (!initialStatsBackup.ContainsKey(weapon))
        {
            WeaponStatsBackup backup = new WeaponStatsBackup
            {
                damage = weapon.damage,
                range = weapon.range,
                cooldown = weapon.cooldown,
                maxAmmo = weapon.maxAmmo,
                ammoRegenRate = weapon.ammoRegenRate,
                projectileSpeed = weapon.projectileSpeed,
                pierceCount = weapon.pierceCount
            };
            initialStatsBackup.Add(weapon, backup);

            // Aseguramos que empiece bloqueada la primera vez si es necesario
            weapon.unlockedRuntime = false;
        }
        else
        {
            // Si YA tenemos backup, significa que estamos REINICIANDO (o cambiando escena).
            // RESTAURAMOS los valores originales para borrar las mejoras de la partida anterior.
            RestoreWeaponStats(weapon);
        }
    }

    private void RestoreWeaponStats(PlayerAttackType weapon)
    {
        if (initialStatsBackup.TryGetValue(weapon, out WeaponStatsBackup backup))
        {
            weapon.damage = backup.damage;
            weapon.range = backup.range;
            weapon.cooldown = backup.cooldown;
            weapon.maxAmmo = backup.maxAmmo;
            weapon.ammoRegenRate = backup.ammoRegenRate;
            weapon.projectileSpeed = backup.projectileSpeed;
            weapon.pierceCount = backup.pierceCount;

            // Siempre re-bloquear al reiniciar
            weapon.unlockedRuntime = false;

            // Debug.Log($"Stats restaurados para: {weapon.attackName}");
        }
    }

    private void InitializeWeapon(PlayerAttackType weapon)
    {
        if (weapon != null)
        {
            weapon.unlockedRuntime = false;
            // Llenar munición al máximo al inicio
            if (!weaponAmmo.ContainsKey(weapon))
            {
                weaponAmmo.Add(weapon, weapon.maxAmmo);
            }
        }
    }

    void Update()
    {
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }
        // --- LÓGICA DE APUNTADO (MOUSE) ---
        HandleAiming();

        // --- REGENERACIÓN DE MUNICIÓN (RECALENTAMIENTO) ---
        RegenAmmoLogic();
    }

    private void RegenAmmoLogic()
    {
        // Regeneramos munición de TODAS las armas desbloqueadas pasivamente
        // O solo de la actual si prefieres. Aquí regenero todas.

        // Copiamos claves para iterar
        List<PlayerAttackType> keys = new List<PlayerAttackType>(weaponAmmo.Keys);
        foreach (var weapon in keys)
        {
            if (weapon.useAmmo)
            {
                float current = weaponAmmo[weapon];
                if (current < weapon.maxAmmo)
                {
                    // Regenerar 
                    current += weapon.ammoRegenRate * Time.deltaTime;
                    // Clampear
                    if (current > weapon.maxAmmo) current = weapon.maxAmmo;

                    weaponAmmo[weapon] = current;
                }
            }
        }
    }

    // --- MÉTODO DE RECARGA (Tu versión integrada) ---
    public void RefillAllAmmo(float percentage)
    {
        // Crear lista de claves para iterar seguro
        List<PlayerAttackType> keys = new List<PlayerAttackType>(weaponAmmo.Keys);

        foreach (var weapon in keys)
        {
            if (weapon.useAmmo)
            {
                float amountToAdd = weapon.maxAmmo * percentage;
                weaponAmmo[weapon] = Mathf.Min(weaponAmmo[weapon] + amountToAdd, weapon.maxAmmo);
            }
        }
    }

    private void HandleAiming()
    {
        if (attackPoint == null || mainCam == null) return;

        // 1. Obtener posición del mouse en el mundo
        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        // 2. Calcular dirección desde el JUGADOR (transform.position) hacia el mouse
        // IMPORTANTE: Usamos la posición del jugador como centro, no la del attackPoint anterior
        Vector3 direction = mousePos - transform.position;

        // 3. Calcular el ángulo
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 4. ROTACIÓN: Girar el arma para que apunte al mouse
        attackPoint.rotation = Quaternion.Euler(0, 0, angle);

        // 5. POSICIÓN (ORBITA): Mover el arma en un círculo alrededor del jugador
        // Convertimos el ángulo a radianes para Sin/Cos
        float angleRad = angle * Mathf.Deg2Rad;

        // Calculamos el offset circular
        Vector3 orbitOffset = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0) * weaponOrbitRadius;

        // Aplicamos posición relativa al jugador
        attackPoint.position = transform.position + orbitOffset;

        // Opcional: Voltear sprite del arma si apunta a la izquierda (para que no quede de cabeza)
        if (attackPoint.localScale.y > 0 && (angle > 90 || angle < -90))
        {
            // Invertir Y local para voltear
            Vector3 scale = attackPoint.localScale;
            scale.y = -Mathf.Abs(scale.y);
            attackPoint.localScale = scale;
        }
        else if (attackPoint.localScale.y < 0 && (angle <= 90 && angle >= -90))
        {
            // Restaurar Y local
            Vector3 scale = attackPoint.localScale;
            scale.y = Mathf.Abs(scale.y);
            attackPoint.localScale = scale;
        }
    }

    // ========== MÉTODOS PÚBLICOS ==========

    // Tecla E (Melee Rápido) - Siempre usa basicAttack directo
    public void TryQuickMelee()
    {
        if (!CanAttackNow) return;
        TryAttack(basicAttack);
    }

    // Click Izquierdo - Usa lo que tengas seleccionado
    public void TryCurrentWeapon()
    {
        if (!CanAttackNow) return;

        PlayerAttackType weaponToFire = CurrentEquippedWeapon;

        if (weaponToFire != null && weaponToFire.IsUnlocked)
        {
            TryAttack(weaponToFire);
        }
    }

    // Rueda del Ratón - Cicla inteligentemente
    public void CycleWeapon(int direction)
    {
        // Total = 1 (básico) + N (especiales)
        int totalWeapons = 1 + (specialAttacks != null ? specialAttacks.Length : 0);
        if (totalWeapons <= 1) return; // Nada que cambiar

        int originalIndex = currentWeaponIndex;
        int attempts = totalWeapons;

        // Buscar siguiente arma desbloqueada
        do
        {
            currentWeaponIndex += direction;

            // Loop infinito (Carrusel)
            if (currentWeaponIndex >= totalWeapons) currentWeaponIndex = 0;
            if (currentWeaponIndex < 0) currentWeaponIndex = totalWeapons - 1;

            attempts--;

        } while (!IsWeaponAtIndexUnlocked(currentWeaponIndex) && attempts > 0);

        // Notificar cambio si es necesario
        if (originalIndex != currentWeaponIndex)
        {
            OnWeaponChanged?.Invoke(CurrentEquippedWeapon);
            Debug.Log($"Arma equipada: {CurrentEquippedWeapon.attackName}");

            // SONIDO CAMBIO ARMA
            if (AudioManager.Instance != null && weaponSwitchSound != null)
            {
                AudioManager.Instance.PlayUISound(weaponSwitchSound);
            }
        }
    }

    private bool IsWeaponAtIndexUnlocked(int index)
    {
        // El índice 0 (básico) siempre está desbloqueado
        if (index == 0) return basicAttack != null && basicAttack.IsUnlocked;

        // Índices 1+ revisan la lista de especiales
        int specialIndex = index - 1;
        if (specialAttacks != null && specialIndex < specialAttacks.Length)
        {
            return specialAttacks[specialIndex] != null && specialAttacks[specialIndex].IsUnlocked;
        }
        return false;
    }

    public void SetCanAttack(bool value) => canAttack = value;
    public void ResetCooldown() => attackCooldownTimer = 0f;

    // ========== LÓGICA DE DAÑO ==========

    // ========== LÓGICA DE ATAQUE HÍBRIDA (MELEE / PROYECTIL) ==========

    private void TryAttack(PlayerAttackType attack)
    {
        if (attack == null || !attack.IsUnlocked) return;

        // --- CHEQUEO DE MUNICIÓN ---
        if (attack.useAmmo)
        {
            if (weaponAmmo.ContainsKey(attack))
            {
                if (weaponAmmo[attack] >= attack.ammoCostPerShot)
                {
                    // Gastar munición
                    weaponAmmo[attack] -= attack.ammoCostPerShot;
                }
                else
                {
                    // SONIDO SIN MUNICIÓN
                    if (AudioManager.Instance != null && attack.emptyAmmoSound != null)
                        AudioManager.Instance.PlayUISound(attack.emptyAmmoSound); // 2D porque es feedback al jugador
                    return;
                }
            }
        }

        // SONIDO DISPARO
        if (AudioManager.Instance != null && attack.launchSound != null)
        {
            // Variar pitch ligeramente para realismo (0.9 a 1.1)
            AudioManager.Instance.PlaySoundAtPosition(attack.launchSound, attackPoint.position, 1f, Random.Range(0.9f, 1.1f));
        }

        // VFX: MUZZLE FLASH (Al atacar)
        if (attack.muzzleFlashPrefab != null)
        {
            GameObject flash = Instantiate(attack.muzzleFlashPrefab, attackPoint.position, attackPoint.rotation);
            Destroy(flash, 1f); // Auto-destrucción segura
        }

        attackCooldownTimer = attack.cooldown;
        OnAttackExecuted?.Invoke();

        switch (attack.attackBehavior)
        {
            case PlayerAttackBehavior.Melee:
                ExecuteMeleeAttack(attack);
                break;

            case PlayerAttackBehavior.Projectile:
            case PlayerAttackBehavior.Lobbed: // <--- AÑADIDO: Mismo método, la diferencia está en el RB del prefab
                ExecuteProjectileAttack(attack);
                break;

            case PlayerAttackBehavior.Area:
                ExecuteAreaAttack(attack);
                break;
        }
    }

    private void ExecuteAreaAttack(PlayerAttackType attack)
    {
        if (attack.projectilePrefab != null)
        {
            // Instanciar en la posición del jugador (o attackPoint si quieres offset)
            // IMPORTANTE: Rotación Identity (0,0,0) porque es un círculo expansivo
            GameObject areaObj = Instantiate(attack.projectilePrefab, transform.position, Quaternion.identity);

            ProjectileController proj = areaObj.GetComponent<ProjectileController>();
            if (proj != null)
            {
                // La dirección (Vector2.zero) no importa para Area porque crece, no se mueve
                proj.Initialize(attack, Vector2.zero, enemyLayer);
            }
        }
    }

    private void ExecuteProjectileAttack(PlayerAttackType attack)
    {
        if (attack.projectilePrefab != null)
        {
            Vector2 shootDirection = attackPoint.right;

            GameObject projectileObj = Instantiate(attack.projectilePrefab, attackPoint.position, Quaternion.identity);
            ProjectileController proj = projectileObj.GetComponent<ProjectileController>();

            if (proj != null)
            {
                proj.Initialize(attack, shootDirection, enemyLayer);
            }
        }
    }

    private void ExecuteMeleeAttack(PlayerAttackType attack)
    {
        // 1. Dañar Enemigos
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attack.range, enemyLayer);
        bool hitSomething = false;

        foreach (Collider2D enemy in hitEnemies)
        {
            CharacterHealth eh = enemy.GetComponent<CharacterHealth>();
            if (eh != null)
            {
                eh.TakeDamage(attack.damage);
                hitSomething = true;

                // VFX: IMPACTO (Sangre)
                if (attack.hitEffectPrefab != null)
                {
                    // Instanciar en la posición del enemigo
                    GameObject hitVFX = Instantiate(attack.hitEffectPrefab, enemy.transform.position, Quaternion.identity);
                    Destroy(hitVFX, 2f);
                }

                if (attack.hasHitReaction)
                {
                    EnemyController ec = enemy.GetComponent<EnemyController>();
                    if (ec != null) ec.OnHitByPlayer(transform.position, attack);
                }
            }
        }

        // 2. --- NUEVO: DESVIAR PROYECTILES (PARRY) ---
        // Buscamos en la capa de proyectiles dentro del mismo rango del golpe
        Collider2D[] hitProjectiles = Physics2D.OverlapCircleAll(attackPoint.position, attack.range, projectileLayer);

        foreach (Collider2D hit in hitProjectiles)
        {
            ProjectileController proj = hit.GetComponent<ProjectileController>();
            if (proj != null)
            {
                // Calculamos dirección: El proyectil se devuelve hacia donde apunta el arma (el mouse)
                Vector2 deflectDir = attackPoint.right;

                // Desviar: Le damos la dirección del mouse, le decimos que ahora pegue a los Enemigos, y aplicamos el multiplicador
                proj.Deflect(deflectDir, enemyLayer, deflectDamageMultiplier);

                hitSomething = true; // Contar como golpe exitoso para feedback
            }
        }

        if (hitSomething) OnAttackHit?.Invoke();
    }

    // ========== DEBUG ==========

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        if (basicAttack != null && basicAttack.projectilePrefab == null)
            Gizmos.DrawWireSphere(attackPoint.position, basicAttack.range);

        if (CurrentEquippedWeapon != null && CurrentEquippedWeapon.projectilePrefab == null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, CurrentEquippedWeapon.range);
        }
    }
}