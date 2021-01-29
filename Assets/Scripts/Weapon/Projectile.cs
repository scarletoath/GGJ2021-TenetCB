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

		[SerializeField] private bool DamageOnCollision = true;
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

		private void OnDestroy()
		{
			if (!DamageOnCollision)
			{
				Debug.Log($"Projectile {SourceAmmo.name} damage after duration.");
				SourceAmmo.ApplyDamage(null, transform.position);

				if (DamageEffect != null)
					Instantiate(DamageEffect, transform.position, transform.rotation);
			}
		}

		public void Configure(Ammo SourceAmmo)
        {
            this.SourceAmmo = SourceAmmo;
			Destroy(gameObject, MaxLifetime);

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

		private void OnCollisionEnter(Collision collision)
		{
			if (!DamageOnCollision)
				return;

            Debug.Log($"Projectile {SourceAmmo.name} hit {collision.gameObject}.", collision.gameObject);
			SourceAmmo.ApplyDamage(collision.gameObject, collision.GetContact(0).point);
			Destroy(gameObject);

			if (DamageEffect != null)
				Instantiate(DamageEffect, transform.position, transform.rotation);
		}
	}
}