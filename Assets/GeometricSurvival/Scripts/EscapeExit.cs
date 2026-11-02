using UnityEngine;

namespace GeometricSurvival
{
    public sealed class EscapeExit : MonoBehaviour
    {
        [SerializeField] private MatchController match;
        private void OnTriggerStay2D(Collider2D other)
        { if (other.GetComponent<PlayerController>()) match.Escape(); }
    }
}
