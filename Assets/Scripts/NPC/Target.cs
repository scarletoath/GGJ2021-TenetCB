using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tenet.NPC
{
    public class Target : MonoBehaviour, Game.IHealth
	{
		float health;
		[SerializeField] float maxHealth;
		bool isDead;

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
			isDead	= false;
		}

        // Update is called once per frame
        void Update()
		{
			if (!isDead)
			{
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
		}

		void FixedUpdate()
		{
			if(!isDead)
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
			}			
		}

		void OnDeath()
		{
			Debug.Log( "TestTarget OnDeath!" );			
			var particle = Instantiate(ForwardDeathParticle, transform.position, Quaternion.identity);
			Destroy(gameObject);

			isDead	= true;
			if( turret != null )
			{
				turret.isDead = true;
			}
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
			if(health <= 0.0f && !isDead )
			{
				OnDeath();
			}

			return health;
		}
	}
}