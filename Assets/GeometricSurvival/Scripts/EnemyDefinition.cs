using UnityEngine;

namespace GeometricSurvival
{
    // Type Object: a shared asset describes a type; each actor owns its current health.
    [CreateAssetMenu(menuName = "Geometric Survival/Enemigo")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        public string displayName;
        [Min(1)] public float health = 50;
        [Min(0)] public float damage = 10;
        [Min(0.1f)] public float speed = 2;
        [Min(1)] public int points = 20;
    }
}
