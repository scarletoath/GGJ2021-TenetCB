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

		[Header("UI Elements")]

		[SerializeField] private Image InversionForward;
        [SerializeField] private Image InversionBackward;
        [SerializeField] private Image InversionTimer;
		[SerializeField] private Gradient InversionTimerColors = new Gradient { colorKeys = new[] { new GradientColorKey(Color.white, 1) } };

        [Space]
		[SerializeField] private Image WeaponPistol;
        [SerializeField] private Image WeaponRPG;

        [Space]
        [SerializeField] private Image HealthBarMax;
        [SerializeField] private Image HealthBar;
        [SerializeField] private float PixelsPerHealth = 100.0f / 25.0f;

        private float PromptEndTIme;
        private string PromptText;

        private Coroutine InversionCoroutine;
        private float InversionStartTime;
        private float InversionEndTime;

        // Start is called before the first frame update
        private void Start()
        {
            SessionManager.Instance.OnInversionStateChanged += UpdateInversionStateVisuals;
            SessionManager.Instance.Player.OnHealthChanged += UpdateHealthVisuals;
            SessionManager.Instance.Player.OnHealFailed += ShowHealFailReason;
            SessionManager.Instance.Player.OnWeaponChanged += UpdateWeaponVisuals;

            HealthBarMax.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, SessionManager.Instance.Player.CurrentHealth * PixelsPerHealth);
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

        private void UpdateInversionStateVisuals(InversionState InversionState, bool IsRefreshed = false)
        {
			switch (InversionState)
			{
				case InversionState.Normal:
                    InversionForward.enabled = true;
                    InversionBackward.enabled = false;

                    if(InversionCoroutine!=null)
                    {
                        StopCoroutine(InversionCoroutine);
                        InversionTimer.enabled = false;
					}
					break;
				case InversionState.Inverted:
                    InversionForward.enabled = false;
                    InversionBackward.enabled = true;

					if (InversionTimer != null)
					{
                        if (InversionCoroutine != null)
                            StopCoroutine(InversionCoroutine);
						InversionStartTime = Time.time;
						InversionEndTime = InversionStartTime + DifficultySettings.Instance.CurrentDifficulty.InversionMaxDuration;
						InversionCoroutine = StartCoroutine(UpdateInversionTimer());
					}
					IEnumerator UpdateInversionTimer()
					{
						InversionTimer.CrossFadeColor(InversionTimerColors.Evaluate(1), 0, false, true);
						if (!InversionTimer.enabled)
                            InversionTimer.enabled = true;
						while (true)
						{
							float PercentTimer = Mathf.Clamp01(Mathf.InverseLerp(InversionEndTime, InversionStartTime, Time.time));
							InversionTimer.fillAmount = PercentTimer;

							var TargetColor = InversionTimerColors.Evaluate(PercentTimer);
							if (InversionTimer.color != TargetColor)
								InversionTimer.CrossFadeColor(TargetColor, 0.25f, false, true);

							if (PercentTimer <= 0)
								yield break;
							else
								yield return null;
						}
                        InversionTimer.enabled = false;
					}
                    break;
			}
		}

        private void UpdateHealthVisuals(HealthChangeArgs HealthChangeArgs)
        {
            HealthBar.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, HealthChangeArgs.Current * PixelsPerHealth);
        }

        private void ShowHealFailReason(HealFailReason HealFailReason)
        {
            PromptEndTIme = Time.time + PromptDuration;
            PromptText = HealFailReason.ToString();
		}
    }
}