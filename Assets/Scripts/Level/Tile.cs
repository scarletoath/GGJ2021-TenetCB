using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tenet.Level
{
	public enum TileUsage
	{
		__ = 0, // None

		Random,
		Normal,
		Destructible,
		PlayerStart,
		Landmark,
	}

	public class Tile : MonoBehaviour
	{

		[HideInInspector, SerializeField] private Transform _Root;
		[SerializeField] private TileUsage _Usage = TileUsage.Normal;

		public TileUsage Usage => _Usage;
		public Transform Root => _Root;
		public Tile[] Neighbors { get; } = new Tile[4];

		public int x { get; private set; }
		public int y { get; private set; }
		public Vector2Int Coord => new Vector2Int(x, y);

		private void OnValidate()
		{
			if (_Root == null)
				TryGetComponent(out _Root);
		}

		public void Configure(int x,int y)
		{
			this.x = x;
			this.y = y;
		}

		public void SetNeighbor(MoveDirection Direction, Tile Tile)
		{
			Neighbors[(int)Direction] = Tile;
		}

		public Vector3 GetRandomNeighborDir()
		{
			int Index = Random.Range(0, 9283749) % Neighbors.Length;
			Tile NeighborTile = null;
			while (true)
			{
				NeighborTile = Neighbors[Index];
				if (NeighborTile != null)
					break;
				Index = Index + 1 < Neighbors.Length ? Index + 1 : 0;
			}
			return NeighborTile.transform.position - transform.position;
		}

	}
}