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

		public event Action<InversionState> OnInversionStateChanged;

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

		internal void StartLevel()
		{
			CurrentInversionState = DifficultySettings.Instance.CurrentDifficulty.GetRandomInversionState(); // Need to do this first so that anything in the level that relies on it queries the correct state

			var StartTile = GenerateLevel();
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

		public TileSpawnMarker GenerateLevel ()
		{
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
				OnInversionStateChanged?.Invoke(CurrentInversionState);
				InversionTimer.Restart();
				return true;
			}
			return false;
		}

		public void RefreshInversion ()
		{
			GameMode.ApplyInversionEffects(CurrentInversionState, InversionVolume);
			InversionTimer.Restart();
		}

    }
}