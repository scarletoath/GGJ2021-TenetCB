using System;
using System.Collections;
using System.Collections.Generic;
using Tenet.Triggers;
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

        [SerializeField] private float DetectionRadius = 1.0f;

        private bool IsActive = false;

		private float Health;

        private CharacterController Controller;

		private Weapon.Weapon[] Weapons;
        private int CurrentWeaponIndex = -1;

        private float RemainingHealCooldown;

        public event Action<HealthChangeArgs> OnHealthChanged;
        public event Action<HealFailReason> OnHealFailed;
        public event Action<Weapon.Weapon> OnWeaponChanged;

        private void Awake()
        {
            TryGetComponent(out Controller);
			SessionManager.Instance?.SetPlayer(this);
        }

		private void Start()
		{
            Weapons = GetComponentsInChildren<Weapon.Weapon>();
			foreach (var Weapon in Weapons)
			{
                Weapon.Activate(false, true);
			}
            ChangeWeapon(0);

            MaxHealth = DifficultySettings.Instance?.CurrentDifficulty.MaxPlayerHealth ?? 100;
            ChangeHealth(MaxHealth);
            Debug.Assert(MaxHealth > 0 && CurrentHealth == MaxHealth, "Player's health and max health contains invalid values.", this);

            if (SessionManager.Instance != null)
                SessionManager.Instance.StartLevel();
            else
                Enable(true);
		}

		// Update is called once per frame
		void Update()
        {
            if (!IsActive)
                return;

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

		public void Enable(bool IsEnable)
		{
            IsActive = IsEnable;
            Controller.enabled = IsEnable;
		}

		public float CurrentHealth => Health;
		public float MaxHealth { get; private set; }
		public Weapon.Weapon CurrentWeapon { get; private set; }

		public float Damage(float Amount) => ChangeHealth(-Amount);
        public float DamagePercent(float Percent) => Damage(Percent * MaxHealth);
        public float Heal(float Amount) => ChangeHealth(Amount);
        public float HealPercent(float Percent) => Heal(Percent * MaxHealth);

        private float ChangeHealth(float Amount)
        {
            if (!Mathf.Approximately(Amount, 0.0f))
            {
                float PreviousHealth = Health;
                Health = Mathf.Clamp(Health + Amount, 0, MaxHealth);
                OnHealthChanged?.Invoke(new HealthChangeArgs { Previous = PreviousHealth , Current = Health , Change = Amount });
            }
            return Health;
        }

        private void ChangeWeapon (int WeaponIndex)
        {
            var NewWeapon = Weapons[WeaponIndex = Mathf.Clamp(WeaponIndex, 0, Weapons.Length)];
            if (WeaponIndex != CurrentWeaponIndex)
            {
                CurrentWeapon?.Activate(false, true);
                CurrentWeapon = NewWeapon;
                CurrentWeaponIndex = WeaponIndex;
                CurrentWeapon?.Activate(true, true);
                OnWeaponChanged?.Invoke(CurrentWeapon);
            }
		}

		private void TickHealCooldown()
		{
			if (RemainingHealCooldown > 0)
				RemainingHealCooldown = SessionManager.Instance.GameMode.CalculateValue(SessionManager.Instance.CurrentInversionState, RemainingHealCooldown, 0, DifficultySettings.Instance.CurrentDifficulty.PlayerHealCooldown, Time.deltaTime);
		}

		private void CheckWeaponInput()
		{
			if (Input.GetButtonUp("Fire1"))
			{
				CurrentWeapon.TryShoot();
			}
			if (Input.GetButtonDown("Swap")) // TODO : Swap weapon input
			{
                // next/prev/explicit selection
                int NewWeaponIndex = CurrentWeaponIndex + 1 < Weapons.Length ? CurrentWeaponIndex + 1 : 0;
                ChangeWeapon(NewWeaponIndex);
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
                SessionManager.Instance.ActivateInversion();
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (other.TryGetComponent(out AmmoDrop AmmoDrop)) // Collect ammo drop if it is one
			{
                CollectAmmoDrop(AmmoDrop);
			}
		}

        public void CollectAmmoDrop(AmmoDrop AmmoDrop)
		{
			foreach (var Weapon in Weapons)
			{
				SessionManager.Instance.GameMode.ConsumeAmmoDrop(SessionManager.Instance.CurrentInversionState, AmmoDrop, Weapon);
			}
			Destroy(AmmoDrop.gameObject);
		}

        public IEnumerable<HistoryMarker> GetAllMarkers()
        {
			foreach (var Weapon in Weapons)
			{
                yield return Weapon.CurrentAmmo.MarkerPrefab;
			}
		}
	}
}