using UnityEngine;

// abstract = No puedes poner este script directamente en un objeto.
// Es solo una plantilla para otros scripts.
public abstract class UpgradeEffect : ScriptableObject
{
    // Todas las mejoras deben implementar este método.
    // target = Quién recibe la mejora (normalmente el Jugador).
    public abstract void Apply(GameObject target);
}