using UnityEngine;

[CreateAssetMenu(fileName = "NewAmmoEffect", menuName = "Upgrades/Effects/Refill Ammo")]
public class RefillAmmoEffect : UpgradeEffect
{
    [Tooltip("Porcentaje a recargar (0 a 1). 0.5 = 50%, 1 = 100%")]
    [Range(0f, 1f)]
    public float percentage = 1f;

    public override void Apply(GameObject target)
    {
        PlayerAttack attack = target.GetComponent<PlayerAttack>();
        if (attack != null)
        {
            attack.RefillAllAmmo(percentage);
            Debug.Log($"Efecto: Munición recargada al {percentage * 100}%");
        }
    }
}