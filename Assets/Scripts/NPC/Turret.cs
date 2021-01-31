using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tenet.NPC
{
    public class Turret : MonoBehaviour
	{
		bool isAutoShooting									= false;
		[SerializeField] float baseAccuracy					= 0.75f;
		[SerializeField] float maxAccuracy					= 1.0f;
		[SerializeField] float durationToReachMaxAccuracy	= 5.0f;
		float currAccuracy									= 0.0f;

		Weapon.Weapon weapon								= null;
		float autoShootEndTime								= 0.0f;
		[SerializeField] float autoShootDuration			= 20.0f;
		float shootDuration;
		Vector3 positionToShootAt;
		private Vector3 velocity = Vector3.zero;

		// Start is called before the first frame update
		void Start()
        {
			weapon			= gameObject.GetComponentInChildren<Weapon.Weapon>();
			currAccuracy	= baseAccuracy;
			shootDuration	= autoShootDuration;
		}

        // Update is called once per frame
        void Update()
        {
			if (Time.time < autoShootEndTime)
			{
				shootDuration += Time.deltaTime;
				if( shootDuration < durationToReachMaxAccuracy )
				{
					currAccuracy = shootDuration / durationToReachMaxAccuracy * ( maxAccuracy - baseAccuracy ) + baseAccuracy;
				}
				else
				{
					currAccuracy = maxAccuracy;
				}
				RotateAndShoot();
			}
			else
			{
				//Debug.Log( "update else, time : " + Time.time + ", end time " + autoShootEndTime );
				shootDuration = 0.0f;
			}
		}

		public void StartShootingAtPosition(Vector3 targetPosition)
		{
			autoShootEndTime	= Time.time + autoShootDuration;
			//Debug.Log( "New auto shoot end time : " + autoShootEndTime );
			positionToShootAt	= targetPosition;
		}

		void RotateAndShoot()
		{
			Vector3 shootDir = positionToShootAt - weapon.gameObject.transform.position;
			shootDir.Normalize();
			float percentageAccuracy = currAccuracy / maxAccuracy;
			//Debug.Log( "Rotate n shoot : " + percentageAccuracy );
			//weapon.gameObject.transform.forward = Vector3.Lerp( weapon.gameObject.transform.forward, shootDir, percentageAccuracy );
			weapon.gameObject.transform.forward = Vector3.SmoothDamp( weapon.gameObject.transform.forward, shootDir, ref velocity, 0.3f);
			weapon.TryShoot();
		}

		void ResetAccuracy()
		{
			currAccuracy = baseAccuracy;
		}
    }
}