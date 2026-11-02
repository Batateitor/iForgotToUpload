using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GeometricSurvival
{
    public sealed class WeaponController : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform aimPivot;
        [SerializeField] private ProjectileActor[] projectilePrefabs;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private EnemySpawner spawner;
        private ComponentPool<ProjectileActor>[] pools;
        private float nextShot;
        private readonly CommandHistory commands = new CommandHistory();
        public int Selected { get; private set; } = 2;
        public event Action<int> WeaponChanged;
        private void Awake()
        {
            pools = new ComponentPool<ProjectileActor>[projectilePrefabs.Length];
            for (int i = 0; i < pools.Length; i++) pools[i] = new ComponentPool<ProjectileActor>(projectilePrefabs[i]);
        }
        private void Update()
        {
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.digit1Key.wasPressedThisFrame) Select(0);
                if (k.digit2Key.wasPressedThisFrame) Select(1);
                if (k.digit3Key.wasPressedThisFrame) Select(2);
                if (k.zKey.wasPressedThisFrame) UndoWeaponChange();
            }
            var mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 aim = (Vector2)aimCamera.ScreenToWorldPoint(mouse.position.ReadValue()) - (Vector2)transform.position;
            if (aim.sqrMagnitude < .001f) aim = Vector2.right;
            aim.Normalize();
            aimPivot.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            if (!mouse.leftButton.isPressed || Time.time < nextShot || (EventSystem.current && EventSystem.current.IsPointerOverGameObject())) return;
            int index = Selected;
            var projectile = pools[index].Rent();
            projectile.Fire(muzzle.position, aim, spawner, p => pools[index].Return(p));
            nextShot = Time.time + projectile.Definition.cooldown;
        }
        public void Select(int index)
        {
            if (index < 0 || index >= pools.Length || index == Selected) return;
            commands.Execute(new SelectWeaponCommand(this, index));
        }
        public void UndoWeaponChange() { commands.UndoLast(); }
        public WeaponMemento CaptureMemento() { return new WeaponMemento(this, Selected); }
        public void RestoreMemento(WeaponMemento snapshot)
        {
            if (snapshot == null || snapshot.Owner != this) return;
            ApplySelection(snapshot.Selected);
        }
        internal void ApplySelection(int index)
        {
            Selected = index; WeaponChanged?.Invoke(index);
        }
    }
}
