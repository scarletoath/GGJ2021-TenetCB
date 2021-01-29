using System;
using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using UnityEngine;

namespace Tenet.Weapon
{

    public class Ammo : MonoBehaviour
    {

        [SerializeField] private int MaxCount = 15;
        [SerializeField] private Projectile Projectile;

        [SerializeField] private float Damage = 10.0f;
        [SerializeField] private float DamageRadius = 0.0f;

        private int CurrentCount;

		private void Awake() => Refill();

        public int Count => CurrentCount;

		public bool IsEmpty => CurrentCount == 0;
        public bool IsFull => CurrentCount == MaxCount;

		public void Refill() => ChangeCount(MaxCount);
		public void Clear() => ChangeCount(0);

        public void Add(int Change = 1) => ChangeCount(CurrentCount + Change);
        public void Remove(int Change = 1) => ChangeCount(CurrentCount - Change);

		private void OnGUI()
		{
            using (new GUILayout.AreaScope(new Rect(Screen.width - 200, 0, 200, 50), string.Empty, GUI.skin.box))
            {
                GUILayout.Label($"Ammo Type {name} : {CurrentCount} / {MaxCount}");
			}
		}

		public Projectile CreateProjectile (Vector3 Position, Quaternion Rotation)
        {
            var ProjectileInstance = Instantiate(Projectile, Position, Rotation);
            ProjectileInstance.Configure(this);
            return ProjectileInstance;
		}

		public void ApplyDamage(GameObject TargetGameObject, Vector3 TargetPoint)
		{
			if (DamageRadius <= 0)
			{
                ApplyDamage(TargetGameObject);
			}
			else
			{
                ApplyDamage(TargetPoint);
			}
		}

        public bool ApplyDamage(GameObject TargetGameObject)
        {
            if (TargetGameObject != null && TargetGameObject.TryGetComponent(out IHealth IHealth))
            {
                IHealth.Damage(Damage);
                return true;
			}
            return false;
		}

        public bool ApplyDamage(Vector3 TargetPoint)
        {
            var Colliders = Physics.OverlapSphere(TargetPoint, DamageRadius);
            bool HasApplied = false;
			foreach (var Collider in Colliders)
			{
                HasApplied |= ApplyDamage(Collider.gameObject);
			}
            return HasApplied;
		}

		private void ChangeCount(int Count)
        {
            CurrentCount = Mathf.Clamp(Count, 0, MaxCount);
		}
    }
}