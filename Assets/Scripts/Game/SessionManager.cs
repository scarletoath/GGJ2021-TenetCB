using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Tenet.GameMode;
using Tenet.Level;
using UnityEngine;

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

		public event Action<InversionState> OnInversionStateChanged;

		private void Awake()
		{
            Instance = this;
			DontDestroyOnLoad(gameObject);

			GameMode = DefaultGameMode;
		}

		private readonly Stopwatch InversionTimer = new Stopwatch();

		public GameModeBase GameMode { get; private set; }
		public Player Player { get; private set; }

		public InversionState? CanInvertTargetState { get; private set; } = null;
		public InversionState CurrentInversionState { get; private set; } = InversionState.Normal;
		public float CurrentInversionStateDuration => (float)InversionTimer.Elapsed.TotalSeconds;

		internal void StartLevel()
		{
			string LevelTag = DifficultySettings.Instance.CurrentDifficulty.GetRandomGeneralTag();
			var Pattern = DifficultySettings.Instance.CurrentDifficulty.GetRandomPattern();
			var StartTile = LevelGenerator.Generate(Pattern, LevelTag, DifficultySettings.Instance.CurrentDifficulty.ReservedTags);
			var Landmarks = LevelGenerator.GetTilesForTag("Landmark"); // Hard-coded like a MF, no good place to store this
			var LookAtLandmark = Landmarks[UnityEngine.Random.Range(0, Landmarks.Count)];
			UnityEngine.Debug.Log($"Player start facing {LookAtLandmark.name}", LookAtLandmark);
			Player.transform.position = StartTile.transform.position + Vector3.up;
			Player.transform.LookAt(LookAtLandmark.transform, Vector3.up);
			Player.Enable(true);
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
			if (UnityEngine.Debug.isDebugBuild && Input.GetKey(KeyCode.LeftShift) || CanInvertTargetState.HasValue)
			{
				CurrentInversionState = CanInvertTargetState.Value;
				GameMode.ApplyInversionEffects(CurrentInversionState);
				OnInversionStateChanged?.Invoke(CurrentInversionState);
				InversionTimer.Restart();
				return true;
			}
			return false;
		}

		public void RefreshInversion ()
		{
			GameMode.ApplyInversionEffects(CurrentInversionState);
			InversionTimer.Restart();
		}

    }
}