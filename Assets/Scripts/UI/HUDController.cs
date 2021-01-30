using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using UnityEngine;

namespace Tenet.UI
{
    public class HUDController : MonoBehaviour
    {

        [SerializeField] private float HealthWarningPercent = 0.2f;
        [SerializeField] private float PromptDuration = 3.0f; // seconds

        private float PromptEndTIme;
        private string PromptText;

        // Start is called before the first frame update
        private void Start()
        {
            SessionManager.Instance.OnInversionStateChanged += UpdateInversionStateVisuals;
            SessionManager.Instance.Player.OnHealthChanged += UpdateHealthVisuals;
            SessionManager.Instance.Player.OnHealFailed += ShowHealFailReason;

            UpdateInversionStateVisuals(SessionManager.Instance.CurrentInversionState);
            UpdateHealthVisuals(new HealthChangeArgs { Current = SessionManager.Instance.Player.CurrentHealth });
        }

		private void OnDestroy()
        {
            SessionManager.Instance.OnInversionStateChanged -= UpdateInversionStateVisuals;
            SessionManager.Instance.Player.OnHealthChanged -= UpdateHealthVisuals;
            SessionManager.Instance.Player.OnHealFailed -= ShowHealFailReason;			
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

        private void UpdateInversionStateVisuals(InversionState InversionState)
        {

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