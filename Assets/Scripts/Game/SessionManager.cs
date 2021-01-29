using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Tenet.GameMode;
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

		public bool CanInvert { get; private set; } = false;
		public InversionState CurrentInversionState { get; private set; } = InversionState.Normal;
		public float CurrentInversionStateDuration => (float)InversionTimer.Elapsed.TotalSeconds;

		public void SetGameMode ( GameModeBase GameMode )
		{
			this.GameMode = GameMode;
		}

		public void SetPlayer ( Player Player )
		{
			this.Player = Player;
		}

		public void SetInvertability ( bool CanInvert , Collider other )
		{
			if (this.CanInvert != CanInvert && other.TryGetComponent ( out Player Player ) && Player == this.Player)
			{
				this.CanInvert = CanInvert;
			}
		}

		public bool ActivateInversion ( InversionState NewInversionState )
		{
			if (CurrentInversionState != NewInversionState)
			{
				if (UnityEngine.Debug.isDebugBuild && Input.GetKey(KeyCode.LeftShift) || CanInvert)
				{
					CurrentInversionState = NewInversionState;
					GameMode.ApplyInversionEffects(CurrentInversionState);
					OnInversionStateChanged?.Invoke(CurrentInversionState);
					InversionTimer.Restart();
				}
				return CanInvert;
			}
			return false;
		}

    }
}