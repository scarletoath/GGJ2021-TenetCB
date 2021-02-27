using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Tenet.GameMode;
using Tenet.Level;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Tenet.Game
{

	public enum InversionState
	{
		Normal,
		Inverted,
	}

    public class SessionManager : MonoBehaviour
    {

		public static SessionManager Instance { get; private set; }

		[SerializeField] private LevelGenerator LevelGenerator;
		[SerializeField] private GameModeBase DefaultGameMode;
		[SerializeField] private AudioSource BGMSource;

		[Space]

		[SerializeField] private Volume InversionVolume;

		[Header("Randomizer")]

		[Tooltip("If true, will always generate the same \"randomized\" level, mainly for testing. Otherwise, each generation is randomized. RandomSeedOverride will always contain the currently used seed.")]
		[SerializeField] private bool UseRandomSeedOverride;
		[SerializeField] private int RandomSeedOverride;

		public delegate void InversionStateChangedHandler(InversionState NewInversionState, bool IsRefreshed);
		public event InversionStateChangedHandler OnInversionStateChanged;

		private void Awake()
		{
            Instance = this;
			//DontDestroyOnLoad(gameObject);

			GameMode = DefaultGameMode;
		}

		private void Update()
		{
			if (Input.GetKeyUp(KeyCode.Delete)) // Restart
			{
				RestartLevel();
			}
			if (Input.GetKeyUp(KeyCode.F1)) // God mode
			{
				GodMode = !GodMode;
			}
			if (Input.GetKeyUp(KeyCode.Home))
			{
				EndLevel(true);
			}
			if (Input.GetKeyUp(KeyCode.End))
			{
				EndLevel(false);
			}
		}

		private readonly Stopwatch InversionTimer = new Stopwatch();

		public bool GodMode{ get; private set; }
		public GameModeBase GameMode { get; private set; }
		public Player Player { get; private set; }
		public AudioSource BGMAudioSource => BGMSource;

		public InversionState? CanInvertTargetState { get; private set; } = null;
		public InversionState CurrentInversionState { get; private set; } = InversionState.Normal;
		public float CurrentInversionStateDuration => (float)InversionTimer.Elapsed.TotalSeconds;

		public void SetRandomSeed(int Seed, bool ForceRefresh = false)
		{
			if (ForceRefresh || Seed != RandomSeedOverride)
			{
				UnityEngine.Random.InitState(Seed);
				RandomSeedOverride = Seed;
			}
		}

		private void RefreshRandomSeed() => SetRandomSeed(UseRandomSeedOverride ? RandomSeedOverride : UnityEngine.Random.Range(int.MinValue, int.MaxValue), true);

		internal void StartLevel()
		{
			RefreshRandomSeed();

			CurrentInversionState = DifficultySettings.Instance.CurrentDifficulty.GetRandomInversionState(); // Need to do this first so that anything in the level that relies on it queries the correct state

			var StartTile = GenerateLevel(false);
			var Landmarks = LevelGenerator.GetTilesForTag("Landmark"); // Hard-coded like a MF, no good place to store this
			var LookAtLandmark = Landmarks[UnityEngine.Random.Range(0, Landmarks.Count)];
			var LookAtDir = Vector3.ProjectOnPlane(LookAtLandmark.transform.position - Player.transform.position, Vector3.up).normalized;
			Player.transform.position = StartTile.transform.position + Vector3.up;
			Player.transform.forward = LookAtDir;
			Player.Enable(true);
			RefreshInversion();
			UnityEngine.Debug.Log($"Player start facing {LookAtLandmark.name} with rotation = {Quaternion.LookRotation(LookAtDir).eulerAngles} and initial inversion state = {CurrentInversionState}", LookAtLandmark);
		}

		public void RestartLevel()
		{
			SceneManager.LoadScene(gameObject.scene.name);
		}

		public void EndLevel(bool IsWin)
		{
			if (!GodMode)
				SceneManager.LoadScene("Main Menu");
		}

		public TileSpawnMarker GenerateLevel (bool RegenerateSeed = true)
		{
			if (RegenerateSeed)
				RefreshRandomSeed();

			string LevelTag = DifficultySettings.Instance.CurrentDifficulty.GetRandomGeneralTag();
			var Pattern = DifficultySettings.Instance.CurrentDifficulty.GetRandomPattern();
			var StartTile = LevelGenerator.Generate(Pattern, LevelTag, DifficultySettings.Instance.CurrentDifficulty.LevelTagPercent, DifficultySettings.Instance.CurrentDifficulty.ReservedTags, DifficultySettings.Instance.CurrentDifficulty.GeneralTags);
			LevelGenerator.Configure(DifficultySettings.Instance.CurrentDifficulty.ExpendedAmmoRange.GetRandom());
			return StartTile;
		}

		public void SetGameMode ( GameModeBase GameMode )
		{
			this.GameMode = GameMode;
		}

		public void SetPlayer ( Player Player )
		{
			this.Player = Player;
			Player.Enable(false);
		}

		public void SetInvertability ( InversionState? TargetState , Collider other )
		{
			if (CanInvertTargetState != TargetState && other.TryGetComponent ( out Player Player ) && Player == this.Player)
			{
				CanInvertTargetState = TargetState;
			}
		}

		public bool ActivateInversion ()
		{
			var NewInversionState = CanInvertTargetState.HasValue ? CanInvertTargetState.Value : (CurrentInversionState == InversionState.Normal ? InversionState.Inverted : InversionState.Normal);
			if (UnityEngine.Debug.isDebugBuild && Input.GetKey(KeyCode.LeftShift) || CanInvertTargetState.HasValue)
			{
				CurrentInversionState = NewInversionState;
				GameMode.ApplyInversionEffects(CurrentInversionState, InversionVolume);
				OnInversionStateChanged?.Invoke(CurrentInversionState, false);
				InversionTimer.Restart();
				return true;
			}
			return false;
		}

		public void RefreshInversion ()
		{
			GameMode.ApplyInversionEffects(CurrentInversionState, InversionVolume);
			OnInversionStateChanged?.Invoke(CurrentInversionState, true);
			InversionTimer.Restart();
		}

    }
}