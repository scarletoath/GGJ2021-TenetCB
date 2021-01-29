using System;
using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using Tenet.Weapon;
using UnityEngine;

namespace Tenet.GameMode
{
    public class GameModeBase : ScriptableObject
    {

		[SerializeField] private Color[] InversionStateColors = new Color[Enum.GetValues(typeof(InversionState)).Length];

		private Coroutine InversionRoutine;

		public Color GetInversionStateColor(InversionState InversionState) => InversionStateColors[Mathf.Clamp((int)InversionState, 0, InversionStateColors.Length)];

		public bool CanHeal(InversionState InversionState)
		{
			return InversionState != InversionState.Inverted;
		}

		public float CalculateValue(InversionState InversionState, float Value, float Min, float Max, float Delta)
		{
			switch (SessionManager.Instance.CurrentInversionState)
			{
				case InversionState.Normal:
			        if (Value > Min)
                    {
                        return Mathf.Clamp(Value - Delta, 0, Max);
                    }
					break;
				case InversionState.Inverted:
                    if (Value <= Max)
                    {
                        return Mathf.Clamp(Value + Delta, 0, Max);
                    }
                    break;
			}
			return Value;
		}

		public bool CanUseWeapon(InversionState InversionState)
		{
			return InversionState == InversionState.Normal || true; // TODO : check object history
		}

		public void ConsumeAmmo(InversionState InversionState, Ammo Ammo)
		{
			switch (InversionState)
			{
				case InversionState.Normal: // decrease ammo
					Ammo.Remove();
					break;
				case InversionState.Inverted: // increase ammo
					Ammo.Add();
					break;
			}
		}

		public bool ShouldAutoReload(InversionState InversionState, Ammo Ammo) => InversionState switch
		{
			InversionState.Normal => Ammo.IsEmpty,
			InversionState.Inverted => Ammo.IsFull,
			_ => false,
		};

		public void ReloadAmmo(InversionState InversionState, Ammo Ammo)
		{
			switch (InversionState)
			{
				case InversionState.Normal: // max ammo
					Ammo.Refill();
					break;
				case InversionState.Inverted: // clear ammo
					Ammo.Clear();
					break;
			}
		}

        public void ApplyInversionEffects(InversionState InversionState)
        {
			if (InversionRoutine != null)
				SessionManager.Instance.StopCoroutine(InversionRoutine);

			switch (InversionState)
			{
				case InversionState.Normal:
					break;
				case InversionState.Inverted:
					InversionRoutine = SessionManager.Instance.StartCoroutine(RunInvertedEffects()); ;
					IEnumerator RunInvertedEffects()
					{
						var Config = DifficultySettings.Instance.CurrentDifficulty;
						yield return new WaitForSeconds(Config.InversionMaxDuration);

						var HealthLossInterval = new WaitForSeconds(Config.InversionHealthLossInterval);
						while (true)
						{
							SessionManager.Instance.Player.DamagePercent(Config.InversionHealthLossPercent);
							yield return HealthLossInterval;
						}
					}
					break;
			}
		}
    }
}