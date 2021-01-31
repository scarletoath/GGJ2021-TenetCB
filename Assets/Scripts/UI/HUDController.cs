using System;
using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Tenet.UI
{
    public class HUDController : MonoBehaviour
    {

        [SerializeField] private float HealthWarningPercent = 0.2f;
        [SerializeField] private float PromptDuration = 3.0f; // seconds

        [SerializeField] private Image InversionForward;
        [SerializeField] private Image InversionBackward;

        [SerializeField] private Image WeaponPistol;
        [SerializeField] private Image WeaponRPG;

        private float PromptEndTIme;
        private string PromptText;

        // Start is called before the first frame update
        private void Start()
        {
            SessionManager.Instance.OnInversionStateChanged += UpdateInversionStateVisuals;
            SessionManager.Instance.Player.OnHealthChanged += UpdateHealthVisuals;
            SessionManager.Instance.Player.OnHealFailed += ShowHealFailReason;
            SessionManager.Instance.Player.OnWeaponChanged += UpdateWeaponVisuals;

            UpdateWeaponVisuals(SessionManager.Instance.Player.CurrentWeapon);
            UpdateInversionStateVisuals(SessionManager.Instance.CurrentInversionState);
            UpdateHealthVisuals(new HealthChangeArgs { Current = SessionManager.Instance.Player.CurrentHealth });
        }

		private void OnDestroy()
        {
            SessionManager.Instance.OnInversionStateChanged -= UpdateInversionStateVisuals;
            SessionManager.Instance.Player.OnHealthChanged -= UpdateHealthVisuals;
            SessionManager.Instance.Player.OnHealFailed -= ShowHealFailReason;
			SessionManager.Instance.Player.OnWeaponChanged -= UpdateWeaponVisuals;
        }

		private void OnGUI()
        {
			using (new GUILayout.VerticalScope (GUI.skin.box))
			{
			    var InversionState = SessionManager.Instance.CurrentInversionState;
                GUILayout.Label($"Inversion State : {SessionManager.Instance.CurrentInversionState} ({SessionManager.Instance.GameMode.GetInversionStateProfile(SessionManager.Instance.CurrentInversionState).Color})");

                GUILayout.Label($"Player Health : {SessionManager.Instance.Player.CurrentHealth} / {DifficultySettings.Instance.CurrentDifficulty.MaxPlayerHealth}");
				if (SessionManager.Instance.Player.CurrentHealth <= DifficultySettings.Instance.CurrentDifficulty.MaxPlayerHealth * HealthWarningPercent)
				{
                    GUILayout.Label("LOW PLAYER HEALTH");
				}

                if (Time.time < PromptEndTIme)
                {
                    GUILayout.Label(PromptText);
				}
			}
        }

		private void UpdateWeaponVisuals(Weapon.Weapon Weapon)
		{
            if (Weapon == null)
                return;
			switch (Weapon.name)
			{
                case "Pistol":
                    WeaponPistol.enabled = true;
                    WeaponRPG.enabled = false;
                    break;
                case "RPG":
                    WeaponPistol.enabled = false;
                    WeaponRPG.enabled = true;
                    break;
			}
		}

        private void UpdateInversionStateVisuals(InversionState InversionState)
        {
			switch (InversionState)
			{
				case InversionState.Normal:
                    InversionForward.enabled = true;
                    InversionBackward.enabled = false;
					break;
				case InversionState.Inverted:
                    InversionForward.enabled = false;
                    InversionBackward.enabled = true;
                    break;
			}
		}

        private void UpdateHealthVisuals(HealthChangeArgs HealthChangeArgs)
        {

        }

        private void ShowHealFailReason(HealFailReason HealFailReason)
        {
            PromptEndTIme = Time.time + PromptDuration;
            PromptText = HealFailReason.ToString();
		}
    }
}