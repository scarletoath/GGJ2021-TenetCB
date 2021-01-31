using System;
using System.Collections;
using System.Collections.Generic;
using Tenet.Level;
using UnityEngine;
using Tenet.Utils.Editor;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif

namespace Tenet.Game
{
	[Serializable]
	public class ReservedTagInfo
	{
		[Tag(TagCategory.ReservedTag)] public string Tag;
        public int MinCount = 1;
        public int MaxCount = 1;
        public int GetRandomCount() => UnityEngine.Random.Range(MinCount, MaxCount + 1);
	}

	[Serializable]
	public class InversionStateProbability
	{
		public InversionState InversionState;
		[Range(0, 1)]
		public float Percent = 1.0f;
	}

	public class DifficultySettings : MonoBehaviour
	{

		[Serializable]
        public class DifficultyConfig
        {
            public string Name = "Normal";
			
			[Header("Player")]
            
			public float MaxPlayerHealth = 100.0f;
            public float PlayerHealCooldown = 60.0f; // seconds
			[Range(0, 1)]
			public float PlayerHealPercent = 0.5f;
			
			[Header("Weapons")]
            
			public float WeaponBlackoutMultiplier = 1.0f;
			
			[Header("Inversion")]

            public float InversionMaxDuration = 5.0f; // seconds
			[Range(0, 1)]
			public float InversionHealthLossPercent = 0.02f;
            public float InversionHealthLossInterval = 1.0f; // seconds
			public InversionStateProbability[] StartInversionStateProbabilities = Enum.GetValues(typeof(InversionState)).Cast<InversionState>().Select(s => new InversionStateProbability { InversionState = s }).ToArray();

			[Header("Level Parameters")]

			[Range(0, 1)]
			public float LevelTagPercent = 0.8f;
			public Vector2Int ExpendedAmmoRange = Vector2Int.zero;
			public Vector2Int AdditionalEnemiesRange = Vector2Int.zero;

            public ReservedTagInfo[] ReservedTags = { new ReservedTagInfo { Tag = "Landmark" }, new ReservedTagInfo { Tag = "InversionForward" }, new ReservedTagInfo { Tag = "InversionBackward" } };
            [Tag(TagCategory.GeneralTag)] public string[] GeneralTags = Array.Empty<string>();
            public TilePattern[] TilePatterns = Array.Empty<TilePattern>();

            public TilePattern GetRandomPattern() => TilePatterns[UnityEngine.Random.Range(0, TilePatterns.Length)];
            public string GetRandomGeneralTag() => GeneralTags[UnityEngine.Random.Range(0, GeneralTags.Length)];
			public InversionState GetRandomInversionState()
			{
				float Roll = UnityEngine.Random.Range(0f, 1f);
				foreach (var State in StartInversionStateProbabilities)
				{
					if (Roll < State.Percent)
						return State.InversionState;
					Roll -= State.Percent;
				}
				return InversionState.Normal;
			}
        }

        public static DifficultySettings Instance { get; private set; }

        [SerializeField] private int DefaultDifficulty = 0;
		[SerializeField] private DifficultyConfig[] Difficulties = new DifficultyConfig[1];

        private int CurrentDifficultyLevel;

        private void Awake()
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);

            SetDifficulty(DefaultDifficulty);
        }

        public DifficultyConfig CurrentDifficulty { get; private set; }

        public IEnumerable<DifficultyConfig> GetDifficulties() => Difficulties;

        public void SetDifficulty(int DifficultyLevel)
		{
			CurrentDifficultyLevel = Mathf.Clamp(DifficultyLevel, 0, Difficulties.Length);
			CurrentDifficulty = Difficulties[CurrentDifficultyLevel];
            Debug.Assert(CurrentDifficulty != null, $"Cannot find configuration for difficulty level {DifficultyLevel}."); ;
		}

#if UNITY_EDITOR
		private void OnValidate()
        {
            TagUtils.DirtyTagsForCategory(TagCategory.Difficulty);

            if (Difficulties?.Length <= 0)
				Difficulties = new DifficultyConfig[1]; // Make sure there is always at least one difficulty config

        }

		private static bool IsPatternsDirty;

		public static void RefreshTilePatternsLibrary()
		{
			if (IsPatternsDirty)
				return;
			IsPatternsDirty = true;
			EditorApplication.delayCall += Refresh;

			void Refresh()
			{
				var DifficultySettings = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameManager.prefab").GetComponentInChildren<DifficultySettings>(true);

				// Auto-search for and add TilePatterns based on difficulty, but don't remove any that don't match (may be manually added)
				var Patterns = AssetDatabase.FindAssets($"t:Prefab", new[] { "Assets/Prefabs/Level/TilePatterns" })
											.Select(AssetDatabase.GUIDToAssetPath)
											.Select(AssetDatabase.LoadAssetAtPath<GameObject>)
											.Select(p => p.GetComponentInChildren<TilePattern>(true))
											.ToArray();
				var TempList = new List<TilePattern>();
				foreach (var Difficulty in DifficultySettings.Difficulties)
				{
					TempList.AddRange(Patterns.Where(p => p.Difficulty == Difficulty.Name && Array.IndexOf(Difficulty.TilePatterns, p) == -1));
					if (TempList.Count > 0)
					{
						Difficulty.TilePatterns = Difficulty.TilePatterns.Concat(TempList).ToArray();
					}
					TempList.Clear();
				}
				EditorUtility.SetDirty(DifficultySettings);
				IsPatternsDirty = false;
			}
		}

		[CustomPropertyDrawer(typeof(InversionStateProbability))]
		private class InversionStateProbabilityDrawer : PropertyDrawer
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				EditorGUI.PropertyField(new Rect(position) { width = EditorGUIUtility.labelWidth }, property.FindPropertyRelative(nameof(InversionStateProbability.InversionState)), GUIContent.none, true);
				EditorGUI.PropertyField(new Rect(position) { xMin = position.xMin + EditorGUIUtility.labelWidth + 4 }, property.FindPropertyRelative(nameof(InversionStateProbability.Percent)), GUIContent.none, true);
			}
		}
#endif

    }
}