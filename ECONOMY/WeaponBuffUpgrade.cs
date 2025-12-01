using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewWeaponUpgrade", menuName = "Upgrades/Effects/Weapon Buff")]
public class WeaponBuffUpgrade : UpgradeEffect
{
    public enum WeaponStatType
    {
        Damage,
        Range,
        Cooldown,
        MaxAmmo,
        AmmoRegen,
        ProjectileSpeed, // Velocidad del proyectil
        PierceCount // Penetración extra
    }

    [Header("Configuración")]
    public WeaponStatType statToUpgrade;

    [Tooltip("Valor a aumentar. Si usas porcentaje, 0.1 es 10%.")]
    public float amount;

    [Tooltip("Si es TRUE, aplica porcentaje. Si es FALSE, suma plana.")]
    public bool usePercentage = false;

    [Header("Objetivo")]
    [Tooltip("Si es TRUE, aplica a TODAS las armas desbloqueadas. Si es FALSE, solo al arma actual equipada.")]
    public bool applyToAllWeapons = false;

    public override void Apply(GameObject target)
    {
        PlayerAttack playerAttack = target.GetComponent<PlayerAttack>();
        if (playerAttack == null) return;

        List<PlayerAttackType> weaponsToUpgrade = new List<PlayerAttackType>();

        if (applyToAllWeapons)
        {
            // Añadir básico
            if (playerAttack.basicAttack != null) weaponsToUpgrade.Add(playerAttack.basicAttack);

            // Añadir especiales
            if (playerAttack.specialAttacks != null)
            {
                foreach (var w in playerAttack.specialAttacks)
                {
                    if (w != null && w.IsUnlocked) weaponsToUpgrade.Add(w);
                }
            }
        }
        else
        {
            // Solo arma actual
            PlayerAttackType current = playerAttack.CurrentEquippedWeapon;
            if (current != null) weaponsToUpgrade.Add(current);
        }

        // Aplicar mejoras
        foreach (var weapon in weaponsToUpgrade)
        {
            ApplyUpgradeToWeapon(weapon);
        }
    }

    private void ApplyUpgradeToWeapon(PlayerAttackType weapon)
    {
        switch (statToUpgrade)
        {
            case WeaponStatType.Damage:
                // Damage es INT. Casteamos el resultado a int.
                int dmgInc = usePercentage ? Mathf.RoundToInt(weapon.damage * amount) : Mathf.RoundToInt(amount);
                weapon.damage += dmgInc;
                Debug.Log($"WEAPON UPGRADE ({weapon.attackName}): Daño +{dmgInc}");
                break;

            case WeaponStatType.Range:
                // Range es FLOAT. Sumamos directo.
                float rangeInc = usePercentage ? weapon.range * amount : amount;
                weapon.range += rangeInc;
                Debug.Log($"WEAPON UPGRADE ({weapon.attackName}): Rango +{rangeInc}");
                break;

            case WeaponStatType.Cooldown:
                // Cooldown es FLOAT. Restamos (mejorar es reducir tiempo).
                float cdDec = usePercentage ? weapon.cooldown * amount : amount;
                weapon.cooldown = Mathf.Max(0.05f, weapon.cooldown - cdDec); // Mínimo 0.05s
                Debug.Log($"WEAPON UPGRADE ({weapon.attackName}): Cooldown -{cdDec}");
                break;

            case WeaponStatType.MaxAmmo:
                // MaxAmmo es FLOAT en tu script nuevo (para soportar 100.0 de recalentamiento).
                if (weapon.useAmmo)
                {
                    float ammoInc = usePercentage ? weapon.maxAmmo * amount : amount;
                    weapon.maxAmmo += ammoInc;
                    Debug.Log($"WEAPON UPGRADE ({weapon.attackName}): Munición Max +{ammoInc}");
                }
                break;

            case WeaponStatType.AmmoRegen:
                // AmmoRegenRate es FLOAT.
                if (weapon.useAmmo)
                {
                    float regenInc = usePercentage ? weapon.ammoRegenRate * amount : amount;
                    weapon.ammoRegenRate += regenInc;
                    Debug.Log($"WEAPON UPGRADE ({weapon.attackName}): Regen Munición +{regenInc}");
                }
                break;

            case WeaponStatType.ProjectileSpeed:
                // ProjectileSpeed es FLOAT.
                if (weapon.attackBehavior == PlayerAttackBehavior.Projectile || weapon.attackBehavior == PlayerAttackBehavior.Lobbed)
                {
                    float speedInc = usePercentage ? weapon.projectileSpeed * amount : amount;
                    weapon.projectileSpeed += speedInc;
                    Debug.Log($"WEAPON UPGRADE ({weapon.attackName}): Velocidad Proyectil +{speedInc}");
                }
                break;

            case WeaponStatType.PierceCount:
                // PierceCount es INT. Aquí estaba el error.
                if (weapon.attackBehavior == PlayerAttackBehavior.Projectile)
                {
                    // Casteo explícito de float (amount) a int.
                    int pierceInc = Mathf.RoundToInt(amount);

                    // Sumamos int con int.
                    weapon.pierceCount += pierceInc;

                    Debug.Log($"WEAPON UPGRADE ({weapon.attackName}): Penetración +{pierceInc}");
                }
                break;
        }
    }
}