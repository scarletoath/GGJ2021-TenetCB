using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using Tenet.Weapon;
using UnityEngine;

namespace Tenet.Triggers
{
    public class AmmoDrop : MonoBehaviour
    {

		[SerializeField] private float _BurstRadius = 1.5f;

		public float BurstRadius => _BurstRadius;

		private void OnTriggerEnter(Collider other)
		{
			if (other.GetComponentInParent<Projectile>() != null)
			{
				SessionManager.Instance.GameMode.DestroyAmmoDrop(SessionManager.Instance.CurrentInversionState, this, SessionManager.Instance.Player);
			}
		}
	}
}