using System;
using UnityEngine;

namespace GeometricSurvival
{
    // Factory: callers request a ready enemy without handling prefab selection,
    // pooling or initialization. Creation and reuse remain separate responsibilities.
    public sealed class EnemyFactory
    {
        private readonly ComponentPool<EnemyActor>[] pools;
        public EnemyFactory(EnemyActor[] prefabs)
        {
            pools = new ComponentPool<EnemyActor>[prefabs.Length];
            for (int i = 0; i < prefabs.Length; i++) pools[i] = new ComponentPool<EnemyActor>(prefabs[i]);
        }
        public EnemyActor CreateRandom(Vector2 point, PlayerController player, MatchController match, Action<EnemyActor> onReleased)
        {
            int type = UnityEngine.Random.Range(0, pools.Length);
            var enemy = pools[type].Rent();
            enemy.Spawn(point, player, match, actor => {
                onReleased(actor);
                pools[type].Return(actor);
            });
            return enemy;
        }
    }
}
