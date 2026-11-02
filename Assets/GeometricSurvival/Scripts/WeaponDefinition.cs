using UnityEngine;

namespace GeometricSurvival
{
    public enum ImpactKind { Explosive, Piercing, Heavy }

    [CreateAssetMenu(menuName = "Geometric Survival/Arma")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        public string displayName;
        public ImpactKind impact;
        public float damage = 55;
        [Min(.05f)] public float cooldown = .24f;
        [Min(1)] public int penetration = 1;
        public float explosionRadius = 2.2f;
        public float speed = 20;
        public float lifetime = 2;
    }
}
