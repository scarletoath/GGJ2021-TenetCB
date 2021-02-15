using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using UnityEngine;

namespace Tenet.NPC
{
    public class Target : MonoBehaviour, Game.IHealth
	{
		float health;
		[SerializeField] float maxHealth;
		[SerializeField] bool isForward;

		//float moveSpeed;
		//float turnSpeed;
		//bool isNormalPatrol; //TBC, check if this is a thing
		Turret turret							= null;
		[SerializeField] float detectionRadius	= 0.0f;
		[SerializeField] GameObject ForwardDeathParticle = null;
		[SerializeField] GameObject InverseDeathParticle = null;

		// Start is called before the first frame update
		void Start()
		{
			turret	= GetComponent<Turret>();
			health	= maxHealth;

			if (turret == null)
			{
				gameObject.SetActive(false);
				Debug.LogWarning("Disabling Target as no turret available.", this);
			}
		}

		private void Update()
		{
			var Player = SessionManager.Instance.Player;
			var Position = transform.position;
			bool IsInRange = Vector3.Distance(Position, Player.ClosestPoint(Position, true)) <= detectionRadius;
			if (IsInRange)
			{
				var PlayerPosition = Player.transform.position;
				bool HasLineOfSight = Player.Raycast(new Ray(Position, (PlayerPosition - Position).normalized), out var Hit, detectionRadius);
				if (HasLineOfSight)
					turret.StartShootingAtPosition(PlayerPosition);
			}
		}

		void OnDeath()
		{
			Object particle = isForward ? ForwardDeathParticle : InverseDeathParticle;
			Instantiate( particle, transform.position, Quaternion.identity);
			Destroy(gameObject);
		}

		// IHealth interface functions
		public float CurrentHealth => health;
		public float Damage( float Amount ) => ChangeHealth( -Amount );
		public float DamagePercent( float Percent ) => Damage( Percent * maxHealth );
		public float Heal( float Amount ) { return 0.0f; }
		public float HealPercent( float Percent ) { return 0.0f; }
		
		private float ChangeHealth( float Amount )
		{
			if( !Mathf.Approximately( Amount, 0.0f ) )
			{
				float PreviousHealth = health;
				health = Mathf.Clamp( health + Amount, 0, maxHealth );
			}

			Debug.Log("TestTarget Health : " + health);
			if(health <= 0.0f)
			{
				OnDeath();
			}

			return health;
		}
	}
}