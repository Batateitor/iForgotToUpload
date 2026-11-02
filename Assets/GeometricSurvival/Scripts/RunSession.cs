using UnityEngine;

namespace GeometricSurvival
{
    public static class RunSession
    {
        private const string BestKey = "GeometricSurvival.HighScore";
        public static int Score { get; private set; }
        public static int Kills { get; private set; }
        public static bool Escaped { get; private set; }
        public static int Best => PlayerPrefs.GetInt(BestKey, 0);
        public static void Save(int score, int kills, bool escaped)
        {
            Score = score; Kills = kills; Escaped = escaped;
            if (score > Best) PlayerPrefs.SetInt(BestKey, score);
            PlayerPrefs.Save();
        }
    }
}
