using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tenet.NPC
{
    public class Turret : MonoBehaviour
    {
		[SerializeField] float baseAccuracy;
		[SerializeField] float maxAccuracy;
		[SerializeField] float durationToReachMaxAccuracy;
		float currAccuracy;

		Weapon.Weapon weapon;
		float autoShootEndTime;
		[SerializeField] float autoShootDuration;
		float shootDuration;
		Vector3 positionToShootAt;

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
			if ( shootDuration < autoShootDuration )
			{
				shootDuration += Time.deltaTime;

				if( shootDuration < durationToReachMaxAccuracy )
				{
					currAccuracy = shootDuration / durationToReachMaxAccuracy * ( maxAccuracy - baseAccuracy ) + baseAccuracy;
				}

				RotateAndShoot();
			}
		}

		public void StartShootingAtPosition(Vector3 targetPosition)
		{
			autoShootEndTime	= Time.time + autoShootDuration;
			positionToShootAt	= targetPosition;
			RotateAndShoot();
		}

		public void RotateAndShoot()
		{
			Vector3 shootDir = positionToShootAt - weapon.gameObject.transform.position;
			shootDir.Normalize();
			Quaternion rot = Quaternion.FromToRotation( weapon.gameObject.transform.forward, shootDir );
			float percentageAccuracy = currAccuracy / maxAccuracy;
			//Debug.Log( "Rotate n shoot : " + percentageAccuracy );
			weapon.gameObject.transform.rotation = Quaternion.Lerp( weapon.gameObject.transform.rotation, rot, percentageAccuracy );			
			weapon.TryShoot();
		}

		void ResetAccuracy()
		{
			currAccuracy = baseAccuracy;
		}
    }
}