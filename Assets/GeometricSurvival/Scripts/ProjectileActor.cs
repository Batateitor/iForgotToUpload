using System;
using System.Collections.Generic;
using UnityEngine;

namespace GeometricSurvival
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class ProjectileActor : MonoBehaviour
    {
        [SerializeField] private WeaponDefinition definition;
        [SerializeField] private ExplosionEffect explosionPrefab;
        private static readonly IImpactStrategy Direct = new DirectImpact();
        private static readonly IImpactStrategy Explosive = new ExplosiveImpact();
        private readonly HashSet<int> hitEnemies = new HashSet<int>();
        private readonly RaycastHit2D[] hits = new RaycastHit2D[256];
        private IImpactStrategy strategy;
        private Action<ProjectileActor> release;
        private Vector2 direction;
        private float expires;
        private int remaining;
        private EnemySpawner spawner;
        private Rigidbody2D physicsBody;
        public WeaponDefinition Definition => definition;
        private void Awake() { physicsBody = GetComponent<Rigidbody2D>(); }
        public void Fire(Vector2 point, Vector2 aim, EnemySpawner enemySpawner, Action<ProjectileActor> returnToPool)
        {
            transform.position = point; direction = aim; spawner = enemySpawner; release = returnToPool;
            remaining = definition.penetration; expires = Time.time + definition.lifetime;
            hitEnemies.Clear(); strategy = definition.impact == ImpactKind.Explosive ? Explosive : Direct;
            gameObject.SetActive(true); physicsBody.position = point;
        }
        private void FixedUpdate()
        {
            if (Time.time >= expires) { release(this); return; }
            float distance = definition.speed * Time.fixedDeltaTime;
            var filter = new ContactFilter2D { useTriggers = true };
            filter.SetLayerMask(1 << 8);
            int count = Physics2D.CircleCast(physicsBody.position, .12f, direction, filter, hits, distance);
            for (int i = 0; i < count; i++)
            {
                var enemy = hits[i].collider.GetComponent<EnemyActor>();
                if (!enemy || !enemy.Alive || !hitEnemies.Add(enemy.SpawnId)) continue;
                strategy.Apply(this, enemy, hits[i].centroid);
                if (--remaining <= 0) { release(this); return; }
            }
            physicsBody.MovePosition(physicsBody.position + direction * distance);
        }
        public void Explode(Vector2 point)
        {
            spawner.DamageArea(point, definition.explosionRadius, definition.damage);
            if (explosionPrefab)
            {
                var effect = Instantiate(explosionPrefab, point, Quaternion.identity);
                effect.transform.localScale = Vector3.one * definition.explosionRadius * 2;
            }
        }
    }
}
