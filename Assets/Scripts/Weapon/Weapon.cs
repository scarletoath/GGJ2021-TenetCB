using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using UnityEngine;

namespace Tenet.Weapon
{
    public class Weapon : MonoBehaviour
    {

        [SerializeField] private float BlackoutDuration = 0.75f;

        [SerializeField] private GameObject RendererGameObject;
        [SerializeField] private Transform ProjectileSpawnPoint;
        [SerializeField] private Ammo[] AmmoTypes;

        private int Mode = -1;
        private Ammo CurrentAmmoType;

        private float BlackoutEndTime;

        // Start is called before the first frame update
        void Awake()
        {
			foreach (var AmmoType in AmmoTypes)
			{
                AmmoType.enabled = false;
			}
            ChangeMode(0);
        }

        // Update is called once per frame
        void Update()
        {
            if (IsBlackout && Time.time >= BlackoutEndTime)
            {
                EnableBlackout(false);
            }
            Debug.DrawRay(ProjectileSpawnPoint.position, ProjectileSpawnPoint.forward * 100, Color.green);
        }

        private void OnGUI()
        {
            using (new GUILayout.AreaScope(new Rect(Screen.width - 200, Screen.height - 50, 200, 50), string.Empty, GUI.skin.box))
            {
                GUILayout.Label($"Weapon : {name}");
				if (IsBlackout)
					GUILayout.Label($"    Blackout");
            }
        }

        public bool IsBlackout { get; private set; } // prevents any weapon-related actions, triggered when shot

        public void Activate(bool IsActivated)
        {
            if (enabled != IsActivated)
            {
                enabled = IsActivated;
                if (RendererGameObject.activeSelf != enabled)
                    RendererGameObject.SetActive(enabled);

                foreach (var AmmoType in AmmoTypes)
                {
                    AmmoType.enabled = false;
                }
                if (enabled)
                {
                    CurrentAmmoType.enabled = true;
                    EnableBlackout(true, true);
				}
            }
        }

        public void ChangeMode (int NewMode)
        {
            NewMode = Mathf.Clamp(NewMode, 0, AmmoTypes.Length);
            if (Mode != NewMode)
            {
                Mode = NewMode;
                if (CurrentAmmoType != null)
                    CurrentAmmoType.enabled = false;
                CurrentAmmoType = AmmoTypes[Mode];
                if (CurrentAmmoType != null)
                    CurrentAmmoType.enabled = true;
            }
		}

        public bool TryShoot ()
        {
            if (!SessionManager.Instance.GameMode.CanUseWeapon(SessionManager.Instance.CurrentInversionState, ProjectileSpawnPoint, out var Marker))
            {
                return false;
			}

			if (!IsBlackout)
			{
				if (SessionManager.Instance.GameMode.ShouldAutoReload(SessionManager.Instance.CurrentInversionState, CurrentAmmoType))
				{
                    Reload();
				}
				else
				{
                    SessionManager.Instance.GameMode.ConsumeAmmo(SessionManager.Instance.CurrentInversionState, CurrentAmmoType, ProjectileSpawnPoint, Marker);
                    EnableBlackout(true);
				}
                return true;
            }
            return false;
		}

        public bool Reload()
        {
            if (!IsBlackout && SessionManager.Instance.GameMode.TryReloadAmmo(SessionManager.Instance.CurrentInversionState, CurrentAmmoType))
            {
				EnableBlackout(true);
                return true;
            }
            return false;
		}

        private void EnableBlackout(bool IsEnable, bool Force = false)
        {
            if (Force || IsBlackout != IsEnable)
            {
                IsBlackout = IsEnable;
                if (IsEnable)
                {
                    BlackoutEndTime = Time.time + DifficultySettings.Instance.CurrentDifficulty.WeaponBlackoutMultiplier * BlackoutDuration;
                }
            }
		}
    }
}