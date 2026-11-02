using System;
using UnityEngine;

namespace GeometricSurvival
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class EnemyActor : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition definition;
        private Rigidbody2D physicsBody;
        private PlayerController target;
        private MatchController match;
        private Action<EnemyActor> release;
        private float health;
        public EnemyDefinition Definition => definition;
        public int SpawnId { get; private set; }
        private static int nextSpawnId;
        public bool Alive => health > 0 && gameObject.activeSelf;
        private void Awake() { physicsBody = GetComponent<Rigidbody2D>(); }
        public void Spawn(Vector2 point, PlayerController player, MatchController controller, Action<EnemyActor> returnToPool)
        {
            target = player; match = controller; release = returnToPool;
            health = definition.health; SpawnId = ++nextSpawnId;
            transform.position = point;
            gameObject.SetActive(true);
            physicsBody.position = point;
        }
        private void FixedUpdate()
        {
            if (!target || !Alive) return;
            physicsBody.MovePosition(Vector2.MoveTowards(physicsBody.position, target.transform.position,
                definition.speed * match.SpeedMultiplier * Time.fixedDeltaTime));
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            if (Alive && other.GetComponent<PlayerController>() == target)
                target.TakeDamage(definition.damage * match.DamageMultiplier);
        }
        public void TakeDamage(float amount)
        {
            if (!Alive) return;
            health -= amount;
            if (health > 0) return;
            match.EnemyKilled(definition.points);
            release(this);
        }
    }
}
