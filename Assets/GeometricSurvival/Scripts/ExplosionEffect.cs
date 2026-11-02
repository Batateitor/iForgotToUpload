using UnityEngine;

namespace GeometricSurvival
{
    public sealed class ExplosionEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private float duration = .25f;
        private float elapsed;
        private void Update()
        {
            elapsed += Time.deltaTime;
            var color = visual.color; color.a = Mathf.Lerp(.5f, 0, elapsed / duration); visual.color = color;
            if (elapsed >= duration) Destroy(gameObject);
        }
    }
}
