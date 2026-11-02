using UnityEngine;
using UnityEngine.UI;

namespace GeometricSurvival
{
    public sealed class SceneMenu : MonoBehaviour
    {
        [SerializeField] private Text bestLabel;
        [SerializeField] private Text resultTitle;
        [SerializeField] private Text resultScore;
        [SerializeField] private Text resultKills;
        private void Start()
        {
            if (bestLabel) bestLabel.text = "RÉCORD PERSONAL   " + RunSession.Best.ToString("D4");
            if (resultTitle) resultTitle.text = RunSession.Escaped ? "ESCAPASTE" : "FIN DE LA PARTIDA";
            if (resultScore) resultScore.text = "PUNTUACIÓN   " + RunSession.Score;
            if (resultKills) resultKills.text = "ENEMIGOS ELIMINADOS   " + RunSession.Kills;
        }
        public void Play() { GameFlowFacade.Play(); }
        public void MainMenu() { GameFlowFacade.MainMenu(); }
        public void Quit() { GameFlowFacade.Quit(); }
    }
}
