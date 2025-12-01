using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponUnlock", menuName = "Upgrades/Effects/Unlock Weapon")]
public class UnlockWeaponEffect : UpgradeEffect
{
    [Tooltip("Arrastra aquí el ScriptableObject del ataque que quieres desbloquear (ej: Fireball)")]
    public PlayerAttackType weaponToUnlock;

    public override void Apply(GameObject target)
    {
        if (weaponToUnlock != null)
        {
            // Opción A: Modificar el ScriptableObject directamente (Más fácil, tu estructura actual lo soporta)
            weaponToUnlock.unlockedRuntime = true;
            Debug.Log($"UPGRADE: Arma desbloqueada: {weaponToUnlock.attackName}");
        }
        else
        {
            Debug.LogError("UnlockWeaponEffect: No se asignó ningún arma para desbloquear.");
        }
    }
}