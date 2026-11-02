using UnityEngine;

namespace GeometricSurvival
{
    [CreateAssetMenu(menuName = "Geometric Survival/Partida")]
    public sealed class GameConfig : ScriptableObject
    {
        [Min(1)] public int targetScore = 500;
        [Min(1)] public float escapeSeconds = 15;
        [Min(1)] public float playerHealth = 100;
        [Min(0.1f)] public float playerSpeed = 5.7f;
        public Vector2 arenaHalfSize = new Vector2(17, 10);
        [Min(0.05f)] public float spawnInterval = .85f;
        [Min(0.05f)] public float enragedSpawnInterval = .18f;
        [Min(1)] public int maxEnemies = 180;
        public float rageSpeedMultiplier = 1.85f;
        public float rageDamageMultiplier = 1.5f;
    }
}
