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

		[SerializeField] private Vector3 DefaultDamageDirection = Vector3.down;

        private Ammo SourceAmmo;
		private Vector3 InitialDirection = Vector3.down;
		private Transform Target;

		private void Update()
		{
			if (Target != null) // tracking target => face and move towards source
			{
				transform.LookAt(Target);
				transform.Translate(0, 0, Speed * Time.deltaTime, Space.Self);
				if (Vector3.Distance(transform.position, Target.position) < 0.5f)
				{
					Destroy(gameObject);
				}
			}
			else if (Rigidbody == null) // not return and not using physics -> manual move
			{
				transform.Translate(0, 0, Speed * Time.deltaTime, Space.Self);
			}
		}

		public void Configure(Ammo SourceAmmo, Transform Target = null)
        {
            this.SourceAmmo = SourceAmmo;
			this.Target = Target;
			if (DestroyAfterLifetime)
				Destroy(gameObject, MaxLifetime);

			if (Target != null)
			{
				if (Rigidbody != null)
				{
					Rigidbody.detectCollisions = false; // disable collisions
				}
			}
			else
			{
				if (!DamageOnCollision)
					Invoke(nameof(ApplyDamage), MaxLifetime);
				else
					InitialDirection = transform.forward;

				if (Rigidbody != null)
				{
					Rigidbody.velocity = transform.forward * Speed;
					if (RandomSpin)
						Rigidbody.angularVelocity = Random.onUnitSphere;
				}
			}

			if (TrailEffect != null)
			{
				var Color = SessionManager.Instance.GameMode.GetInversionStateProfile(SessionManager.Instance.CurrentInversionState).Color;
				var EndColor = Color;
				EndColor.a = 0;
				TrailEffect.startColor = Color;
				TrailEffect.endColor = EndColor;
			}
		}

		private void ApplyDamage() => ApplyDamage(null, transform.position, DefaultDamageDirection, $"damage after duration {MaxLifetime}s");

		private void ApplyDamage(GameObject Target, Vector3 Location, Vector3 Direction, string DebugMessage)
		{
			Debug.Log($"Projectile {SourceAmmo.name} {DebugMessage}.");
			SourceAmmo.ApplyDamage(Target, Location, Direction);

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
			{
				ApplyDamage(collision.gameObject, collision.GetContact(0).point, collision.GetContact(0).normal, $"hit {collision.gameObject}");
				if (Rigidbody != null)
					Rigidbody.detectCollisions = false;
			}
		}
	}
}