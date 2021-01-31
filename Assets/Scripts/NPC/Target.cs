using System.Collections;
using System.Collections.Generic;
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
		Collider playerCollider					= null;
		int playerLayerMask						= 0;
		[SerializeField] float detectionRadius	= 0.0f;
		[SerializeField] bool isDebug			= false;
		[SerializeField] Object ForwardDeathParticle = null;
		[SerializeField] Object InverseDeathParticle = null;

		// Start is called before the first frame update
		void Start()
		{
			turret	= GetComponent<Turret>();
			health	= maxHealth;
		}

  //      // Update is called once per frame
  //      void Update()
		//{
		//	if( playerCollider != null )
		//	{
		//		Vector3 playerPos = playerCollider.transform.position;
		//		Vector3 playerDir = playerPos - transform.position;
		//		playerDir.Normalize();
		//		RaycastHit hit;

		//		if( Physics.Raycast( transform.position, playerDir, out hit, Mathf.Infinity, playerLayerMask ) )
		//		{
		//			if( turret != null )
		//			{
		//				turret.StartShootingAtPosition( playerPos );
		//			}

		//			if( isDebug )
		//			{
		//				Debug.DrawLine( transform.position, transform.position + playerDir * hit.distance, Color.green, 2.0f );
		//			}
		//		}
		//		else
		//		{
		//			if( isDebug )
		//			{
		//				Debug.DrawLine( transform.position, transform.position + playerDir * hit.distance, Color.red, 2.0f );
		//			}
		//		}
		//	}
		//}

		void FixedUpdate()
		{
			var Colliders = Physics.OverlapSphere( transform.position, detectionRadius );

			playerCollider = null;
			foreach( var Collider in Colliders )
			{
				Game.Player player = Collider.gameObject.GetComponent<Game.Player>();
				if( player != null )
				{
					playerLayerMask = 1 << player.gameObject.layer;
					playerCollider = player.GetComponent<Collider>();
					playerCollider = Collider;
				}
			}

			if( playerCollider != null )
			{
				Vector3 playerPos = playerCollider.transform.position;
				Vector3 playerDir = playerPos - transform.position;
				playerDir.Normalize();
				RaycastHit hit;

				if( Physics.Raycast( transform.position, playerDir, out hit, Mathf.Infinity, playerLayerMask ) )
				{
					if( turret != null )
					{
						turret.StartShootingAtPosition( playerPos );
					}

					if( isDebug )
					{
						Debug.DrawLine( transform.position, transform.position + playerDir * hit.distance, Color.green, 2.0f );
					}
				}
				else
				{
					if( isDebug )
					{
						Debug.DrawLine( transform.position, transform.position + playerDir * hit.distance, Color.red, 2.0f );
					}
				}
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