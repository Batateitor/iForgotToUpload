using System.Collections.Generic;
using UnityEngine;

namespace GeometricSurvival
{
    public sealed class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private EnemyActor[] enemyPrefabs;
        [SerializeField] private PlayerController player;
        [SerializeField] private MatchController match;
        private EnemyFactory factory;
        private readonly List<EnemyActor> alive = new List<EnemyActor>();
        private float nextSpawn;
        private void Awake()
        {
            factory = new EnemyFactory(enemyPrefabs);
        }
        private void Update()
        {
            if (Time.time < nextSpawn || alive.Count >= config.maxEnemies) return;
            nextSpawn = Time.time + (match.Enraged ? config.enragedSpawnInterval : Mathf.Max(.3f, config.spawnInterval - match.Elapsed / 180));
            Vector2 point;
            int attempts = 0;
            do
            {
                float sign = Random.value > .5f ? 1 : -1;
                point = Random.value > .5f
                    ? new Vector2(sign * config.arenaHalfSize.x, Random.Range(-config.arenaHalfSize.y, config.arenaHalfSize.y))
                    : new Vector2(Random.Range(-config.arenaHalfSize.x, config.arenaHalfSize.x), sign * config.arenaHalfSize.y);
            } while (Vector2.Distance(point, player.transform.position) < 4 && ++attempts < 12);
            var enemy = factory.CreateRandom(point, player, match, e => alive.Remove(e));
            alive.Add(enemy);
        }
        public void DamageArea(Vector2 point, float radius, float damage)
        {
            for (int i = alive.Count - 1; i >= 0; i--)
                if (Vector2.Distance(alive[i].transform.position, point) <= radius) alive[i].TakeDamage(damage);
        }
    }
}
