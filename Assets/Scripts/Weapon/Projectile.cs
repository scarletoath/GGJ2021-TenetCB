using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tenet.Game;

namespace Tenet.Weapon
{
    public class Projectile : MonoBehaviour
    {

        [SerializeField] private float Speed = 5.0f; // m/s
		[SerializeField] private float MaxLifetime = 2.0f; // seconds

		[SerializeField] private bool DestroyAfterLifetime = false;
		[SerializeField] private bool DestroyOnDamage = false;
		[SerializeField] private bool DamageOnCollision = true;
		[SerializeField] private bool KinematicOnDamage = false;
		[SerializeField] private bool RandomSpin = false;
		[SerializeField] private Rigidbody Rigidbody;

		[SerializeField] private GameObject DamageEffect;
		[SerializeField] private TrailRenderer TrailEffect;

        private Ammo SourceAmmo;

		private void Update()
		{
			if (Rigidbody == null)
				transform.Translate(0, 0, Speed * Time.deltaTime, Space.Self);
		}

		public void Configure(Ammo SourceAmmo)
        {
            this.SourceAmmo = SourceAmmo;
			if (DestroyAfterLifetime)
				Destroy(gameObject, MaxLifetime);

			if (!DamageOnCollision)
				Invoke(nameof(ApplyDamage), MaxLifetime);

			if (Rigidbody != null)
			{
				Rigidbody.velocity = transform.forward * Speed;
				if (RandomSpin)
					Rigidbody.angularVelocity = Random.onUnitSphere;
			}

			if (TrailEffect != null)
			{
				var Color = SessionManager.Instance.GameMode.GetInversionStateColor(SessionManager.Instance.CurrentInversionState);
				var EndColor = Color;
				EndColor.a = 0;
				TrailEffect.startColor = Color;
				TrailEffect.endColor = EndColor;
			}
		}

		private void ApplyDamage() => ApplyDamage(null, transform.position, $"damage after duration {MaxLifetime}s");

		private void ApplyDamage(GameObject Target, Vector3 Location, string DebugMessage)
		{
			Debug.Log($"Projectile {SourceAmmo.name} {DebugMessage}.");
			SourceAmmo.ApplyDamage(Target, Location);

			if (KinematicOnDamage && Rigidbody != null)
			{
				Rigidbody.isKinematic = true;
				Rigidbody.detectCollisions = false;
			}
			
			if (DestroyOnDamage)
				Destroy(gameObject);

			if (DamageEffect != null)
				Instantiate(DamageEffect, transform.position, transform.rotation);
		}

		private void OnCollisionEnter(Collision collision)
		{
			if (DamageOnCollision)
				ApplyDamage(collision.gameObject, collision.GetContact(0).point, $"hit {collision.gameObject}");
		}
	}
}