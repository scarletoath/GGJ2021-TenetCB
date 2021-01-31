using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tenet.NPC
{
    public class Target : MonoBehaviour
    {
		int health;
		bool isForward;
		float moveSpeed;
		float turnSpeed;
		//bool isNormalPatrol; //TBC, check if this is a thing
		Turret turret					= null;
		Collider playerCollider			= null;
		int playerLayerMask				= 0;
		bool isPlayerDetected			= false;
		[SerializeField] bool isDebug	= false;

        // Start is called before the first frame update
        void Start()
		{
			turret = GetComponent<Turret>();
		}

        // Update is called once per frame
        void Update()
        {
			if ( playerCollider != null)
			{
				Vector3 playerPos	= playerCollider.transform.position;
				Vector3 playerDir	= playerPos - transform.position;
				playerDir.Normalize();
				RaycastHit hit;

				if(Physics.Raycast(transform.position, playerDir, out hit, Mathf.Infinity, playerLayerMask ) )
				{
					isPlayerDetected	= true;
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
					isPlayerDetected	= false;
					if( isDebug )
					{
						Debug.DrawLine( transform.position, transform.position + playerDir * hit.distance, Color.red, 2.0f );
					}					
				}
			}

		}

		private void OnTriggerEnter( Collider other )
		{
			Game.Player player = other.gameObject.GetComponent<Game.Player>();
			if(player != null)
			{
				playerLayerMask = 1 << player.gameObject.layer;
				playerCollider = player.GetComponent<Collider>();
			}			
		}

		private void OnTriggerExit( Collider other )
		{
			Game.Player player = other.gameObject.GetComponent<Game.Player>();
			if( player != null )
			{
				playerCollider = null;
			}
		}
	}
}