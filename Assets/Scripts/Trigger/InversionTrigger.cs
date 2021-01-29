using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using UnityEngine;

namespace Tenet.Triggers
{
    public class InversionTrigger : MonoBehaviour
    {
		private void OnTriggerEnter(Collider other) => SessionManager.Instance.SetInvertability(true, other);
		private void OnTriggerExit(Collider other) => SessionManager.Instance.SetInvertability(false, other);
	}
}