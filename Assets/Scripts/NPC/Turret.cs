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

		float autoShootEndTime								= 0.0f;
		[SerializeField] float autoShootDuration			= 20.0f;
		float shootDuration;
		Vector3 positionToShootAt;
		private Vector3 velocity = Vector3.zero;

		Weapon.Weapon weapon = null;
		Transform weaponTransform = null;
		[SerializeField] Weapon.Weapon [] UsableWeapons;

		// Start is called before the first frame update
		void Start()
        {
			weapon			= Instantiate(UsableWeapons[Random.Range(0,2)], transform);
			weaponTransform = weapon.transform;
			currAccuracy	= baseAccuracy;
			shootDuration	= autoShootDuration;
		}

        // Update is called once per frame
        void Update()
        {
			if( Time.time < autoShootEndTime )
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
				shootDuration = 0.0f;
			}
		}

		public void StartShootingAtPosition(Vector3 targetPosition)
		{
			autoShootEndTime	= Time.time + autoShootDuration;
			positionToShootAt	= targetPosition;
		}

		void RotateAndShoot()
		{
			Vector3 shootDir = positionToShootAt - weaponTransform.position;
			shootDir.Normalize();
			float percentageAccuracy = currAccuracy / maxAccuracy;
			weaponTransform.forward = Vector3.SmoothDamp( weaponTransform.forward, shootDir, ref velocity, 0.3f);
			if (!weapon.TryShoot())
			{
				// check if weapon does not have ammo, reload if true
				//weapon.
			}
		}

		void ResetAccuracy()
		{
			currAccuracy = baseAccuracy;
		}
    }
}