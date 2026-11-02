namespace GeometricSurvival
{
    // State: phase objects own transitions and entry behaviour.
    public interface IMatchPhase
    {
        void Enter(MatchController match);
        void Tick(MatchController match, float deltaTime);
    }
    public sealed class SurvivalPhase : IMatchPhase
    {
        public void Enter(MatchController match) { match.SetStatus("ALCANZÁ LA META PARA ABRIR LA SALIDA"); }
        public void Tick(MatchController match, float dt)
        { if (match.Score >= match.Config.targetScore) match.ChangePhase(new EscapePhase()); }
    }
    public sealed class EscapePhase : IMatchPhase
    {
        public void Enter(MatchController match)
        { match.EscapeRemaining = match.Config.escapeSeconds; match.SetExit(true); }
        public void Tick(MatchController match, float dt)
        {
            match.EscapeRemaining = UnityEngine.Mathf.Max(0, match.EscapeRemaining - dt);
            match.SetStatus("SALIDA A LA DERECHA  >  " + UnityEngine.Mathf.CeilToInt(match.EscapeRemaining) + " s");
            if (match.EscapeRemaining <= 0) match.ChangePhase(new EnragedPhase());
        }
    }
    public sealed class EnragedPhase : IMatchPhase
    {
        public void Enter(MatchController match)
        { match.SetExit(false); match.SetEnraged(); match.SetStatus("¡FURIA! LOS ENEMIGOS SON MÁS AGRESIVOS"); }
        public void Tick(MatchController match, float dt) { }
    }
}
