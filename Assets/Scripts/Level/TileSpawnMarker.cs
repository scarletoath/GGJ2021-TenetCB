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
		public TileObjects TileObjects { get; private set; }
		public float Rotation { get; private set; }

		public void Spawn(TileObjects TileObjects, float Rotation)
		{
			// Disable any debug markers
			foreach (Transform Child in transform)
			{
				Child.gameObject.SetActive(false);
			}
			this.Rotation = Rotation;
			this.TileObjects = Instantiate(TileObjects, transform.position, Quaternion.Euler(0, Rotation, 0), transform);
		}

	}
}