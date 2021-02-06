using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using Tenet.Level;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tenet.UI
{
    public class MainMenuController : MonoBehaviour
    {

		public static int Difficulty { get; private set; } = -1;

		[SerializeField] private GraphicRaycaster Raycaster;
		[SerializeField] private LevelGenerator LevelGen;
		[SerializeField] private ToggleGroup DifficultyGroup;
		[SerializeField] private Camera Camera;
		[SerializeField] private float CameraInterval = 5.0f;
		[SerializeField] private float CameraMoveSpeed = 1.0f;
		[SerializeField] private float BoundsMargin = 0.8f;
		[SerializeField] private float YOffset = 10.0f;
		[SerializeField] private Image Fader;
		[SerializeField] private float FadeDuration = 1.0f; // seconds

		private Vector3 CameraMoveDir;

		private void Awake()
		{
			if (Difficulty == -1) // Only set if never set before
				Difficulty = DifficultySettings.Instance.Default;

			if (DifficultyGroup != null) // Set initial active difficulty toggle
			{
				int Index = 0;
				Toggle InitialToggle = null;
				foreach (var Toggle in DifficultyGroup.GetComponentsInChildren<Toggle>())
				{
					InitialToggle = InitialToggle == null || Index == DifficultySettings.Instance.Default ? Toggle : InitialToggle;
					++Index;
				}
				if (InitialToggle != null)
					InitialToggle.isOn = true;
			}

			SessionManager.Instance.GenerateLevel();
			StartCoroutine(PlayCameraAnim(LevelGen.GetBounds()));

			Cursor.lockState = CursorLockMode.None;
		}

		private void Update()
		{
			Camera.transform.Translate(CameraMoveDir * CameraMoveSpeed, Space.World);
		}

		public void StartGame()
        {
			Debug.Assert(Difficulty != -1, "Cannot start game with invalid difficulty.");
			Raycaster.enabled = false;
			Fader.transform.SetAsLastSibling();
			StartCoroutine(FadeThenStart());
			IEnumerator FadeThenStart()
			{
				Fader.CrossFadeAlpha(1, FadeDuration, false);
				yield return new WaitForSeconds(FadeDuration);
				SceneManager.LoadScene(1);
			}
		}

		public void SetDifficulty(int Difficulty)
		{
			MainMenuController.Difficulty = Difficulty;
		}

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
		}

		private IEnumerator PlayCameraAnim(Bounds Bounds)
		{
			var Wait = new WaitForSeconds(CameraInterval);
			var FadeWait = new WaitForSeconds(FadeDuration);
			var Min = Bounds.center - Bounds.extents * BoundsMargin;
			var Max = Bounds.center + Bounds.extents * BoundsMargin;
			while (true)
			{
				var Pos = new Vector3(Random.Range(Min.x, Max.x), Max.y + YOffset, Random.Range(Min.z, Max.z));
				var LookAt = new Vector3(Random.Range(Min.x, Max.x), Min.y, Random.Range(Min.z, Max.z));
				CameraMoveDir = Vector3.ProjectOnPlane(Random.onUnitSphere, Vector3.up);
				Camera.transform.position = Pos;
				Camera.transform.LookAt(LookAt, Vector3.up);

				// fade in
				Fader.CrossFadeAlpha(0, FadeDuration, false);
				yield return FadeWait;

				// animate
				yield return Wait;

				// fade out
				Fader.CrossFadeAlpha(1, FadeDuration, false);
				yield return FadeWait;
			}
		}
	}
}