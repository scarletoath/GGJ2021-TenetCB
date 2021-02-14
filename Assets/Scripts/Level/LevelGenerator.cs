using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tenet.Game;
using UnityEngine;
using Tenet.Triggers;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tenet.Level
{
	public class LevelGenerator : MonoBehaviour
    {

		[SerializeField] private float TileSize = 20.0f;
		[SerializeField] private TileObjects[] TileObjectsLibrary = Array.Empty<TileObjects>();

		private TilePattern LevelPattern;
		private Transform Root;
		private readonly List<TileSpawnMarker> Tiles = new List<TileSpawnMarker>();
		private readonly Dictionary<string, List<TileSpawnMarker>> TileObjectsMap = new Dictionary<string, List<TileSpawnMarker>>(); // In spawned level

		private readonly Dictionary<string, List<TileObjects>> TileObjectLibrarysMap = new Dictionary<string, List<TileObjects>>(); // From complete library

		public float TileLength => TileSize;
		public TilePattern CurrentTemplate => LevelPattern;

		private void OnEnable()
		{
			foreach (var TileObjects in TileObjectsLibrary)
			{
				foreach (var Tag in TileObjects.GetAllTags())
				{
					if (!TileObjectLibrarysMap.TryGetValue(Tag, out var TileObjectsSubLibrary))
						TileObjectLibrarysMap.Add(Tag, TileObjectsSubLibrary = new List<TileObjects>());
					TileObjectsSubLibrary.Add(TileObjects);
				}
			}
		}

#if UNITY_EDITOR
		private static bool IsObjectsDirty;

		public static void RefreshTileObjectsLibrary()
		{
			if (IsObjectsDirty)
				return;
			IsObjectsDirty = true;
			EditorApplication.delayCall += Refresh;

			void Refresh()
			{
				var LevelGen = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameManager.prefab").GetComponentInChildren<LevelGenerator>(true);

				// Auto-search for and add TileObjects
				LevelGen.TileObjectsLibrary = AssetDatabase.FindAssets($"t:Prefab", new[] { "Assets/Prefabs/Level/TileObjects" })
															.Select(AssetDatabase.GUIDToAssetPath)
															.Select(AssetDatabase.LoadAssetAtPath<GameObject>)
															.Select(p => p.GetComponentInChildren<TileObjects>(true))
															.Where(to => to != null).ToArray();
				EditorUtility.SetDirty(LevelGen);
				IsObjectsDirty = false;
			}
		}
#endif

		public TileSpawnMarker Generate(TilePattern Pattern, string LevelTag, float LevelTagPercent, ReservedTagInfo[] ReservedTags, string[] GeneralTags) // Returns randomized player start tile
		{
			Debug.Log($"Generating level using Pattern {Pattern?.name} with level tag = {LevelTag} ...", Pattern);
			LevelPattern = Pattern;

			var PlayerStarts = new List<TileSpawnMarker>();
			var Offset = Vector2.one * (TileSize * 0.5f);

			Root = Instantiate(Pattern, Vector3.zero, Quaternion.identity).transform;
			Root.GetComponentsInChildren(true, Tiles);
			ShuffleList(Tiles);

			int TileIndex = 0; // also tracks total number of reserved tiles
			foreach (var Tag in ReservedTags)
			{
				var TagCount = Tag.GetRandomCount();
				Debug.Log($"- Reserved {TagCount} tiles for reserved tag = {Tag.Tag}.");
				for (int i = 0; i < TagCount && TileIndex < Tiles.Count; i++, TileIndex++)
				{
					var Tile = Tiles[TileIndex];
					Tile.AssignedTag = Tag.Tag;
				}
			}

			int LevelTagTilesCount = Mathf.FloorToInt((Tiles.Count - TileIndex) * LevelTagPercent);
			int NumLevelTagTilesCreated = 0;
			foreach (var Tile in Tiles)
			{
				string TileObjectsTargetTag;
				if (string.IsNullOrEmpty(Tile.AssignedTag))
				{
					if (NumLevelTagTilesCreated + 1 < LevelTagTilesCount) // Use level tag up to desired amount
					{
						++NumLevelTagTilesCreated;
						TileObjectsTargetTag = LevelTag;
					}
					else // Randomize then rest
					{
						TileObjectsTargetTag = GeneralTags[UnityEngine.Random.Range(0, GeneralTags.Length)];
					}
					Tile.AssignedTag = TileObjectsTargetTag;
				}
				else
				{
					TileObjectsTargetTag = Tile.AssignedTag; // Use reserved if assigned
				}
				
				if (!TileObjectLibrarysMap.TryGetValue(TileObjectsTargetTag, out var TileObjectsForTag))
				{
					Debug.LogWarning($"Skipped spawning TileObjects for TileSpawnMarker {Tile?.name} as there are no TileObjects for tag : {TileObjectsTargetTag}.", Tile);
					continue;
				}

				// Spawn TileObjects layer
				var TileObjects = TileObjectsForTag[UnityEngine.Random.Range(0, TileObjectsForTag.Count)];
				Tile.Spawn(TileObjects, UnityEngine.Random.Range(0, 4) * 90);

				// Cache tile for all tags that the TileObjects specifies
				foreach (var Tag in TileObjects.GetAllTags())
				{
					if (!TileObjectsMap.TryGetValue(Tag, out var TilesForTag))
						TileObjectsMap.Add(Tag, TilesForTag = new List<TileSpawnMarker>());
					TilesForTag.Add(Tile);
				}

				// Cache if player can start on tile
				if (Tile.IsPlayerSpawnable)
					PlayerStarts.Add(Tile);
			}

			Debug.Log($"> Spawned {Tiles.Count(t => t.TileObjects != null)}/{Tiles.Count} TileObjects ({TileIndex} reserved, {LevelTagTilesCount} level tag '{LevelTag}').\n{string.Join("\n", Tiles.OrderBy(t => t.name).Select(t => $"  - {t.name} : {t.AssignedTag} / {t.TileObjects?.name} @ {t.Rotation}"))}");

			// Return randomized start tile
			if (PlayerStarts.Count == 0)
			{
				Debug.LogWarning("> Did not find any available tiles for spawning player. Randomizing from all tiles ...");
				PlayerStarts.AddRange(Tiles);
			}
			return PlayerStarts[UnityEngine.Random.Range(0, PlayerStarts.Count)];
		}

		public void Configure(int NumExpendedAmmo)
		{
			var Markers = Root.GetComponentsInChildren<IHistoryMarker>();
			ShuffleList(Markers);
			for (int i = NumExpendedAmmo; i < Markers.Length; i++)
				Markers[i].gameObject.SetActive(false);
			Debug.Log($"Disabled {Markers.Length - NumExpendedAmmo} markers to result in {NumExpendedAmmo} / {Markers.Length} active markers.");
		}

		public List<TileSpawnMarker> GetTilesForTag(string Tag) => TileObjectsMap.TryGetValue(Tag, out var TilesForTag) ? TilesForTag : null;

		public Bounds GetBounds()
		{
			var Renderers = Root.GetComponentsInChildren<Renderer>();
			var Bounds = new Bounds(Root.position, Vector3.zero);
			foreach (var Renderer in Renderers)
			{
				Bounds.Encapsulate(Renderer.bounds);
			}
			return Bounds;
		}

		public void Clear()
		{
			Tiles.Clear();
			TileObjectsMap.Clear();
			if (Root != null)
			{
				Destroy(Root.gameObject);
			}
			LevelPattern = null;
		}

		/// <summary>
		/// https://stackoverflow.com/questions/273313/randomize-a-listt
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="List"></param>
		private static void ShuffleList <T> (IList<T> List)
		{
			int n = List.Count;
			while (n > 1)
			{
				n--;
				int k = UnityEngine.Random.Range(0, n + 1);
				(List[k], List[n]) = (List[n], List[k]);
			}
		}

	}
}