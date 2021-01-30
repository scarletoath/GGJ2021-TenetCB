using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tenet.Level
{
	public class LevelGenerator : MonoBehaviour
    {

		[SerializeField] private float TileSize = 20.0f;

		private LevelTemplate LevelTemplate;
		private Transform Root;
		private readonly Dictionary<Vector2Int, Tile> Tiles = new Dictionary<Vector2Int, Tile>();

		public float TileLength => TileSize;
		public LevelTemplate CurrentTemplate => LevelTemplate;

		public Tile Generate(LevelTemplate Template) // Returns randomized player start tile
		{
			Root ??= new GameObject("LevelRoot").transform;
			LevelTemplate = Template;

			var PlayerStarts = new List<Tile>();
			var Offset = Vector2.one * (TileSize * 0.5f);
			// Generate all
			foreach ((int x, int y, TileUsage Usage) in Template.GetTileInfos())
			{
				var SpawnPos = new Vector3(x * TileSize + Offset.x, 0, y * TileSize + Offset.y);
				var Tile = Instantiate(LevelTemplate.GetRandomTile(Usage), SpawnPos, Quaternion.identity, Root);
				Tile.Configure(x, y);
				Tiles.Add(new Vector2Int(x, y), Tile);
				if (Tile.Usage == TileUsage.PlayerStart)
					PlayerStarts.Add(Tile);
			}

			// Assign neighbors
			foreach (var Tile in Tiles)
			{
				Vector2Int Coord = Tile.Value.Coord;
				if (Tiles.TryGetValue(Coord + Vector2Int.left, out var NeighborTile)) Tile.Value.SetNeighbor(MoveDirection.Left, NeighborTile);
				if (Tiles.TryGetValue(Coord + Vector2Int.right, out NeighborTile)) Tile.Value.SetNeighbor(MoveDirection.Right, NeighborTile);
				if (Tiles.TryGetValue(Coord + Vector2Int.up, out NeighborTile)) Tile.Value.SetNeighbor(MoveDirection.Up, NeighborTile);
				if (Tiles.TryGetValue(Coord + Vector2Int.down, out NeighborTile)) Tile.Value.SetNeighbor(MoveDirection.Down, NeighborTile);
			}

			if (PlayerStarts.Count == 0)
				PlayerStarts.AddRange(Tiles.Values);
			return PlayerStarts[Random.Range(0, PlayerStarts.Count)];
		}

		public void Clear()
		{
			Tiles.Clear();
			if (Root != null)
			{
				Destroy(Root.gameObject);
			}
			LevelTemplate = null;
		}

	}
}