using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tenet.Level
{
	public class TileSpawnMarker : MonoBehaviour
	{

		[SerializeField] private bool _IsPlayerSpawnable;
		public bool IsPlayerSpawnable => _IsPlayerSpawnable;

		public string AssignedTag { get; set; }
		public TileObjects TileObjects { get; set; }
		public float Rotation { get; set; }

	}
}