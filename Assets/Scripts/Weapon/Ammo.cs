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
        [SerializeField] private int ClipCount = 15;
        [SerializeField] private Projectile Projectile;

        [SerializeField] private float Damage = 10.0f;
        [SerializeField] private float DamageRadius = 0.0f;

        private int SpareCount;
        private int CurrentCount;

		private void Awake()
		{
            SpareCount = MaxCount;
			Refill();
		}

		public bool IsEmpty => CurrentCount == 0;
        public bool IsFull => CurrentCount == ClipCount;

		public void Refill() => SpareCount -= Add(Mathf.Clamp(ClipCount - CurrentCount, 0, SpareCount));
		public void Clear() => SpareCount -= Remove(CurrentCount);

		public int Add(int Change = 1) => ChangeCount(Change);
        public int Remove(int Change = 1) => ChangeCount(-Change);

		private void OnGUI()
		{
            using (new GUILayout.AreaScope(new Rect(Screen.width - 200, 0, 200, 50), string.Empty, GUI.skin.box))
            {
                GUILayout.Label($"Ammo Type {name}\n{CurrentCount} / {ClipCount}, {SpareCount}");
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

		private int ChangeCount(int Change)
        {
            Change = Mathf.Clamp(Change, -CurrentCount, ClipCount - CurrentCount + 1);
            CurrentCount += Change;
            return Change;
		}
    }
}