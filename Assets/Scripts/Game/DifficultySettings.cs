using System;
using System.Collections;
using System.Collections.Generic;
using Tenet.Level;
using UnityEngine;

namespace Tenet.Game
{
    public class DifficultySettings : MonoBehaviour
    {
        [System.Serializable]
        public class DifficultyConfig
        {
            public string Name = "Normal";

            public float MaxPlayerHealth = 100.0f;
            public float PlayerHealCooldown = 60.0f; // seconds
            public float PlayerHealPercent = 0.5f;

            public float WeaponBlackoutMultiplier = 1.0f;

            public float InversionMaxDuration = 5.0f; // seconds
            public float InversionHealthLossPercent = 0.02f;
            public float InversionHealthLossInterval = 1.0f; // seconds

            public LevelTemplate[] LevelTemplates = Array.Empty<LevelTemplate>();
        }

        public static DifficultySettings Instance { get; private set; }

        [SerializeField] private int DefaultDifficulty = 0;
		[SerializeField] private DifficultyConfig[] Difficulties = new DifficultyConfig[1];

        private int CurrentDifficultyLevel;

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SetDifficulty(DefaultDifficulty);
        }

        public DifficultyConfig CurrentDifficulty { get; private set; }

        public void SetDifficulty(int DifficultyLevel)
		{
			CurrentDifficultyLevel = Mathf.Clamp(DifficultyLevel, 0, Difficulties.Length);
			CurrentDifficulty = Difficulties[CurrentDifficultyLevel];
            Debug.Assert(CurrentDifficulty != null, $"Cannot find configuration for difficulty level {DifficultyLevel}."); ;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
            if (Difficulties?.Length <= 0)
				Difficulties = new DifficultyConfig[1]; // Make sure there is always at least one difficulty config
		}
#endif

	}
}