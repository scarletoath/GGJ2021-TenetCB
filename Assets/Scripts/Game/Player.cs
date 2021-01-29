using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Tenet.Game
{

    public struct HealthChangeArgs
    {
        public float Previous;
        public float Current;
        public float Change;
    }

    public enum HealFailReason
    {
        InvalidInversionState,
        InCooldown,
        InBlackout,
    }

    public interface IHealth
    {
        float CurrentHealth { get; }

        float Damage(float Amount);
        float DamagePercent(float Percent);
        float Heal(float Amount);
        float HealPercent(float Percent);

	}

    public class Player : MonoBehaviour , IHealth
    {

		private float Health;

		private Weapon.Weapon[] Weapons;
        private Weapon.Weapon CurrentWeapon;
        private int CurrentWeaponIndex = -1;

        private float RemainingHealCooldown;

        public event Action<HealthChangeArgs> OnHealthChanged;
        public event Action<HealFailReason> OnHealFailed;

        private void Awake()
        {
			SessionManager.Instance.SetPlayer(this);
        }

		private void Start()
		{
            Weapons = GetComponentsInChildren<Weapon.Weapon>();
			foreach (var Weapon in Weapons)
			{
                Weapon.Activate(false);
			}
            ChangeHealth(DifficultySettings.Instance.CurrentDifficulty.MaxPlayerHealth);
            ChangeWeapon(0);			
		}

		// Update is called once per frame
		void Update()
        {
            TickHealCooldown();

            CheckWeaponInput();
            CheckHealInput();
            CheckInversionInput();
        }

        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(0, Screen.height - 50, 200, 50), string.Empty, GUI.skin.box))
            {
                GUILayout.Label($"Heal CD : {RemainingHealCooldown:F0}");
            }
        }

        public float CurrentHealth => Health;

		public float Damage(float Amount) => ChangeHealth(-Amount);
        public float DamagePercent(float Percent) => Damage(Percent * DifficultySettings.Instance.CurrentDifficulty.MaxPlayerHealth);
        public float Heal(float Amount) => ChangeHealth(Amount);
        public float HealPercent(float Percent) => Heal(Percent * DifficultySettings.Instance.CurrentDifficulty.MaxPlayerHealth);

        private float ChangeHealth(float Amount)
        {
            if (!Mathf.Approximately(Amount, 0.0f))
            {
                float PreviousHealth = Health;
                Health = Mathf.Clamp(Health + Amount, 0, DifficultySettings.Instance.CurrentDifficulty.MaxPlayerHealth);
                OnHealthChanged?.Invoke(new HealthChangeArgs { Previous = PreviousHealth , Current = Health , Change = Amount });
            }
            return Health;
        }

        private void ChangeWeapon (int WeaponIndex)
        {
            var NewWeapon = Weapons[WeaponIndex = Mathf.Clamp(WeaponIndex, 0, Weapons.Length)];
            if (!(CurrentWeapon?.IsBlackout ?? false) && WeaponIndex != CurrentWeaponIndex)
            {
                CurrentWeapon?.Activate(false);
                CurrentWeapon = NewWeapon;
                CurrentWeaponIndex = WeaponIndex;
                CurrentWeapon?.Activate(true);
            }
		}

		private void TickHealCooldown()
		{
            RemainingHealCooldown = SessionManager.Instance.GameMode.CalculateValue(SessionManager.Instance.CurrentInversionState, RemainingHealCooldown, 0, DifficultySettings.Instance.CurrentDifficulty.PlayerHealCooldown, Time.deltaTime);
		}

		private void CheckWeaponInput()
		{
			if (Input.GetButtonDown("Fire1"))
			{
				if (SessionManager.Instance.GameMode.CanUseWeapon(SessionManager.Instance.CurrentInversionState))
				{
                    CurrentWeapon.Shoot();
				}
			}
			if (Input.GetButtonDown("Swap")) // TODO : Swap weapon input
			{
                // next/prev/explicit selection
				if (!CurrentWeapon?.IsBlackout ?? true)
				{
                    int NewWeaponIndex = CurrentWeaponIndex + 1 < Weapons.Length ? CurrentWeaponIndex + 1 : 0;
                    ChangeWeapon(NewWeaponIndex);
				}
			}
			if (Input.GetButtonDown("Reload"))
			{
                // next/prev/explicit selection
				if (!CurrentWeapon?.IsBlackout ?? true)
				{
                    CurrentWeapon?.Reload();
				}
			}
		}

		private void CheckHealInput()
		{
            if (Input.GetButtonDown("Heal"))
            {
                if (RemainingHealCooldown > 0)
                {
                    OnHealFailed?.Invoke(HealFailReason.InCooldown);
                }
                else if (!SessionManager.Instance.GameMode.CanHeal(SessionManager.Instance.CurrentInversionState))
                {
                    OnHealFailed?.Invoke(HealFailReason.InvalidInversionState);
                }
                else if (CurrentWeapon?.IsBlackout ?? false)
                {
                    OnHealFailed?.Invoke(HealFailReason.InBlackout);
                }
                else
                {
                    HealPercent(DifficultySettings.Instance.CurrentDifficulty.PlayerHealPercent);
                    RemainingHealCooldown = DifficultySettings.Instance.CurrentDifficulty.PlayerHealCooldown;
                }
            }
		}

		private void CheckInversionInput()
		{
			if (Input.GetButtonDown("Inversion"))
			{
                InversionState NewInversionState;
				switch (SessionManager.Instance.CurrentInversionState)
				{
					case InversionState.Normal:
                        NewInversionState = InversionState.Inverted;
						break;
					case InversionState.Inverted:
                        NewInversionState = InversionState.Normal;
						break;
					default:
                        throw new Exception("Unknown Inversion State");
				}
                SessionManager.Instance.ActivateInversion(NewInversionState);
			}
		}
    }
}