using UnityEngine;

namespace GeometricSurvival
{
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCamera : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        private Camera view;
        private void Awake() { view = GetComponent<Camera>(); }
        private void LateUpdate()
        { view.orthographicSize = Mathf.Max(config.arenaHalfSize.y + 5.5f, (config.arenaHalfSize.x + 2) / view.aspect); }
    }
}
