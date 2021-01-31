using System;
using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using Tenet.Triggers;
using UnityEngine;

namespace Tenet.Weapon
{

	public enum DamageType
	{
		Normal,
		Explosive,

		Random = 1000,
	}

	public class Ammo : MonoBehaviour
    {

		[SerializeField] private DamageType DamageType = DamageType.Normal;

		[Space]

        [SerializeField] private int MaxCount = 15;
        [SerializeField] private int ClipCount = 15;

        [Space]

        [SerializeField] private Projectile Projectile;
        [SerializeField] private float Damage = 10.0f;
        [SerializeField] private float DamageRadius = 0.0f;

        [Space]

        [SerializeField] private HistoryMarker Marker;

        private int SpareCount;
        private int CurrentCount;

		private void Awake()
		{
			Marker.gameObject.SetActive(false);
            SpareCount = MaxCount;
			Refill();
		}

		public void Configure(int InClipCount, int TotalCount)
		{
			Debug.Log($"{name}/{Type} configured with in clip = {InClipCount}, total = {TotalCount}", this);
			CurrentCount = InClipCount;
			SpareCount = TotalCount;
		}

		public DamageType Type => DamageType;
		public HistoryMarker MarkerPrefab => Marker;

		public bool IsEmpty => CurrentCount == 0;
        public bool IsFull => CurrentCount == ClipCount;

		public void Refill() => SpareCount -= Add(Mathf.Clamp(ClipCount - CurrentCount, 0, SpareCount));
		public void Clear() => SpareCount -= Remove(CurrentCount);

		public int Add(int Change = 1, bool TransferOverflow = false) => ChangeCount(Change,  TransferOverflow);
        public int Remove(int Change = 1, bool TransferUnderflow = false) => ChangeCount(-Change, TransferUnderflow);

		private void OnGUI()
		{
            using (new GUILayout.AreaScope(new Rect(Screen.width - 200, 0, 200, 50), string.Empty, GUI.skin.box))
            {
                GUILayout.Label($"Ammo Type {name}\n{CurrentCount} / {ClipCount}, {SpareCount}");
			}
		}

		public Projectile CreateProjectile (Vector3 Position, Quaternion Rotation, Transform Target)
        {
            var ProjectileInstance = Instantiate(Projectile, Position, Rotation);
            ProjectileInstance.Configure(this, Target);
            return ProjectileInstance;
		}

        public HistoryMarker GetOrCreateMarker (Vector3 Position, Vector3 Direction)
        {
            var MarkerInstance = Marker.FindAtLocation(Position);
			if (MarkerInstance == null)
			{
				MarkerInstance = Instantiate(Marker, Position, Quaternion.LookRotation(Direction));
				MarkerInstance.gameObject.SetActive(true);
			}
            return MarkerInstance;
		}

		public void ApplyDamage(GameObject TargetGameObject, Vector3 TargetPoint, Vector3 Direction)
		{
            var Marker = GetOrCreateMarker(TargetPoint, Direction);
			Marker.CreateRecord();
			if (Marker.TriggerRadius <= 0)
			{
                ApplyDamage(TargetGameObject, Marker);
			}
			else
			{
                ApplyDamage(TargetPoint, Marker);
			}
		}

        private bool ApplyDamage(GameObject TargetGameObject, HistoryMarker Marker)
        {
			if (TargetGameObject != null)
			{
				if (TargetGameObject.GetComponentInParent<HistoryTarget>() is HistoryTarget Target) // TODO : Also check if can affect target; e.g. only explosive can affect destructible target
				{
					Target.RegisterMarker(Marker);
					Marker.GetLastRecord().AffectedTargets.Add(Target);
				}
				if (TargetGameObject.GetComponentInParent<IHealth>() is IHealth IHealth)
				{
					IHealth.Damage(Damage);
					return true;
				}
			}

			return false;
		}

        private bool ApplyDamage(Vector3 TargetPoint, HistoryMarker Marker)
        {
            var Colliders = Physics.OverlapSphere(TargetPoint, Marker.TriggerRadius);
            bool HasApplied = false;
			foreach (var Collider in Colliders)
			{
				// 9 is player, 10 is enemy
				if( Collider.gameObject.layer >= 9 )
				{
					HasApplied |= ApplyDamage( Collider.gameObject, Marker );
				}
			}
            return HasApplied;
		}

		private int ChangeCount(int Change, bool AllowTransfer)
        {
			if (AllowTransfer)
			{
				CurrentCount += Change;
				if (CurrentCount > ClipCount) // Overflow into spare
				{
					SpareCount += CurrentCount - (CurrentCount %= ClipCount);
				}
				else if (CurrentCount < 0)
				{
					int DeltaFullClip = CurrentCount % ClipCount;
					int DeltaSpareCount = (CurrentCount - ClipCount) - DeltaFullClip;
					CurrentCount = ClipCount + DeltaFullClip; // delta is negative
					SpareCount += DeltaSpareCount; // delta is negative
				}
			}
			else
			{
				Change = Mathf.Clamp(Change, -CurrentCount, ClipCount - CurrentCount + 1);
				CurrentCount += Change;
			}
            return Change;
		}
    }
}