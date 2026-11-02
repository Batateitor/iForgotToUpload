using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GeometricSurvival
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private SpriteRenderer body;
        private Rigidbody2D physicsBody;
        private Vector2 movement;
        private float invulnerableUntil;
        private Color normalColor;
        public float Health { get; private set; }
        public event Action<float> HealthChanged;
        public event Action Died;
        private void Awake()
        {
            physicsBody = GetComponent<Rigidbody2D>();
            normalColor = body.color;
            Health = config.playerHealth;
        }
        private void Update()
        {
            movement = Vector2.zero;
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed || k.upArrowKey.isPressed) movement.y++;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) movement.y--;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) movement.x--;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) movement.x++;
            }
            body.color = Time.time < invulnerableUntil ? Color.white : normalColor;
        }
        private void FixedUpdate()
        {
            Vector2 p = physicsBody.position + movement.normalized * (config.playerSpeed * Time.fixedDeltaTime);
            p.x = Mathf.Clamp(p.x, -config.arenaHalfSize.x + .6f, config.arenaHalfSize.x - .6f);
            p.y = Mathf.Clamp(p.y, -config.arenaHalfSize.y + .6f, config.arenaHalfSize.y - .6f);
            physicsBody.MovePosition(p);
        }
        public void TakeDamage(float amount)
        {
            if (Health <= 0 || Time.time < invulnerableUntil) return;
            invulnerableUntil = Time.time + .65f;
            Health = Mathf.Max(0, Health - amount);
            HealthChanged?.Invoke(Health);
            if (Health <= 0) Died?.Invoke();
        }
    }
}
