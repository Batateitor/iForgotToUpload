using System;
using UnityEngine;

namespace GeometricSurvival
{
    public sealed class MatchController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private PlayerController player;
        [SerializeField] private GameObject escapeExit;
        private IMatchPhase phase;
        private bool finished;
        private readonly GameEventQueue<EnemyKilledEvent> events = new GameEventQueue<EnemyKilledEvent>();
        public GameConfig Config => config;
        public int Score { get; private set; }
        public int Kills { get; private set; }
        public float Elapsed { get; private set; }
        public float EscapeRemaining { get; set; }
        public bool Enraged { get; private set; }
        public bool CanEscape => phase is EscapePhase && EscapeRemaining > 0;
        public float SpeedMultiplier => Enraged ? config.rageSpeedMultiplier : 1;
        public float DamageMultiplier => Enraged ? config.rageDamageMultiplier : 1;
        public string Status { get; private set; }
        public event Action<int, int> ScoreChanged;
        public event Action<string> StatusChanged;
        private void OnEnable() { player.Died += OnPlayerDied; }
        private void OnDisable() { player.Died -= OnPlayerDied; }
        private void Start() { SetExit(false); ChangePhase(new SurvivalPhase()); }
        private void Update()
        {
            if (finished) return;
            Elapsed += Time.deltaTime; phase.Tick(this, Time.deltaTime);
        }
        public void ChangePhase(IMatchPhase next) { phase = next; phase.Enter(this); }
        public void SetExit(bool open) { escapeExit.SetActive(open); }
        public void SetEnraged() { Enraged = true; }
        public void SetStatus(string message) { Status = message; StatusChanged?.Invoke(message); }
        public void EnemyKilled(int points)
        {
            if (finished) return;
            events.Enqueue(new EnemyKilledEvent(points));
        }
        private void LateUpdate() { if (!finished) events.Drain(ApplyEnemyKilled); }
        private void ApplyEnemyKilled(EnemyKilledEvent gameEvent)
        { Score += gameEvent.Points; Kills++; ScoreChanged?.Invoke(Score, Kills); }
        private void OnPlayerDied() { Finish(false); }
        public void Escape() { if (CanEscape) Finish(true); }
        private void Finish(bool escaped)
        {
            if (finished) return;
            events.Drain(ApplyEnemyKilled);
            finished = true;
            GameFlowFacade.FinishRun(Score, Kills, escaped);
        }
    }
}
