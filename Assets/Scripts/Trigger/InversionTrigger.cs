using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using UnityEngine;

namespace Tenet.Triggers
{
    public class InversionTrigger : MonoBehaviour
    {
		[SerializeField] private InversionState TargetState;

		private void OnTriggerEnter(Collider other)
		{
			SessionManager.Instance.SetInvertability(TargetState, other);
			if (SessionManager.Instance.CurrentInversionState == TargetState)
				SessionManager.Instance.RefreshInversion();
		}

		private void OnTriggerExit(Collider other) => SessionManager.Instance.SetInvertability(null, other);
	}
}