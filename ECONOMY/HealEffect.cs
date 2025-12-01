using UnityEngine;

[CreateAssetMenu(fileName = "NewHealEffect", menuName = "Upgrades/Effects/Heal")]
public class HealEffect : UpgradeEffect
{
    [Tooltip("Cantidad de vida a restaurar (Negativo para hacer daño)")]
    public int amount = 20;

    public override void Apply(GameObject target)
    {
        CharacterHealth health = target.GetComponent<CharacterHealth>();
        if (health != null)
        {
            if (amount > 0)
            {
                health.Heal(amount);
                Debug.Log($"Efecto: Curado {amount} HP");
            }
            else if (amount < 0)
            {
                // Para efectos de trade-off que quitan vida al usarse
                health.TakeDamage(Mathf.Abs(amount));
                Debug.Log($"Efecto: Sacrificado {Mathf.Abs(amount)} HP");
            }
        }
    }
}