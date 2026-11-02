using UnityEngine;
using UnityEngine.UI;

namespace GeometricSurvival
{
    // Observer: subscribe to gameplay events; Canvas objects are authored in the scene.
    public sealed class GameHud : MonoBehaviour
    {
        [SerializeField] private MatchController match;
        [SerializeField] private PlayerController player;
        [SerializeField] private WeaponController weapons;
        [SerializeField] private Text scoreLabel, killsLabel, statusLabel, healthLabel;
        [SerializeField] private Slider healthBar;
        [SerializeField] private Image[] weaponHighlights;
        private void OnEnable()
        {
            match.ScoreChanged += UpdateScore; match.StatusChanged += UpdateStatus;
            player.HealthChanged += UpdateHealth; weapons.WeaponChanged += UpdateWeapon;
        }
        private void Start()
        {
            UpdateScore(match.Score, match.Kills); UpdateHealth(player.Health);
            UpdateStatus(match.Status); UpdateWeapon(weapons.Selected);
        }
        private void OnDisable()
        {
            match.ScoreChanged -= UpdateScore; match.StatusChanged -= UpdateStatus;
            player.HealthChanged -= UpdateHealth; weapons.WeaponChanged -= UpdateWeapon;
        }
        private void UpdateScore(int score, int kills)
        { scoreLabel.text = "PUNTOS  " + score + " / " + match.Config.targetScore; killsLabel.text = "ELIMINADOS  " + kills; }
        private void UpdateStatus(string value) { statusLabel.text = value; }
        private void UpdateHealth(float value)
        {
            healthBar.value = value / match.Config.playerHealth;
            healthLabel.text = "VIDA  " + Mathf.CeilToInt(value);
        }
        private void UpdateWeapon(int index)
        {
            for (int i = 0; i < weaponHighlights.Length; i++)
                weaponHighlights[i].color = i == index ? new Color(.15f,.5f,.35f) : new Color(.08f,.12f,.18f);
        }
    }
}
