using UnityEngine;

namespace GeometricSurvival
{
    public interface IImpactStrategy
    {
        void Apply(ProjectileActor projectile, EnemyActor enemy, Vector2 point);
    }
    public sealed class DirectImpact : IImpactStrategy
    {
        public void Apply(ProjectileActor projectile, EnemyActor enemy, Vector2 point)
        { enemy.TakeDamage(projectile.Definition.damage); }
    }
    public sealed class ExplosiveImpact : IImpactStrategy
    {
        public void Apply(ProjectileActor projectile, EnemyActor enemy, Vector2 point)
        { projectile.Explode(point); }
    }
}
