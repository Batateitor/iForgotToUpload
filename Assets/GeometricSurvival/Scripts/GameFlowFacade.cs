using UnityEngine;
using UnityEngine.SceneManagement;

namespace GeometricSurvival
{
    // Facade: one API coordinates result persistence and scene navigation.
    public static class GameFlowFacade
    {
        public static void Play() { SceneManager.LoadScene("Game"); }
        public static void MainMenu() { SceneManager.LoadScene("MainMenu"); }
        public static void FinishRun(int score, int kills, bool escaped)
        {
            RunSession.Save(score, kills, escaped);
            SceneManager.LoadScene("Results");
        }
        public static void Quit()
        {
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}
