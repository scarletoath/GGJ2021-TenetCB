using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Tenet.Triggers;
using Tenet.Weapon;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tenet.Environment
{
	[ExecuteAlways]
    public class DestructibleCover : MonoBehaviour
    {

        [SerializeField] private GameObject NormalObject;
        [SerializeField] private GameObject DestructionObject;
		[SerializeField] private float TargetRebuildDuration = 0.3f; // seconds

		[Space]

		[SerializeField] private HistoryTarget HistoryTarget;
		[SerializeField] private DamageType DamageType;

		[Space]

		[SerializeField] private AudioSource AudioSource;

		private Animation[] DestructionAnims = System.Array.Empty<Animation>();
		private Coroutine AnimCoroutine;

		private void Awake()
		{
			if (DestructionObject != null)
			{
				DestructionAnims = DestructionObject.GetComponentsInChildren<Animation>();
				ShowObject(DestructionObject, false);
			}

			if (HistoryTarget != null)
				HistoryTarget.OnMarkerChanged += CheckDestroyRebuild;
			if (AudioSource != null)
			{
				AudioSource.playOnAwake = false;
				AudioSource.Stop();
			}
		}

		public bool IsDestroyed { get; private set; }

		public void Destroy(bool IsInstant = false)
		{
			if (IsDestroyed)
				return;

			IsDestroyed = true;

			ShowObject(NormalObject, false); // hide normal
			ShowObject(DestructionObject, true); // show destruction

			if (!IsInstant)
				foreach (var Anim in DestructionAnims) // play destruction anim
				{
					var State = Anim[Anim.clip.name];
					State.speed = 1;
					State.normalizedTime = 0;
					Anim.Play(State.name);
				}
			else
				foreach (var Anim in DestructionAnims) // sample anim end frame
				{
					var State = Anim[Anim.clip.name];
					State.normalizedTime = 1;
					State.weight = 1;
					State.enabled = true;
					Anim.Sample();
				}

			if (AudioSource != null) // play destruction sfx
				AudioSource.Play();

			if (AnimCoroutine != null) // clear any existing rebuild coroutines
				StopCoroutine(AnimCoroutine);
			AnimCoroutine = null;
		}

		public void Rebuild(bool IsInstant = false)
		{
			if (!IsDestroyed)
				return;

			IsDestroyed = false;

			if (IsInstant)
			{
				RebuildNow();
				return;
			}

			{
				float MaxDuration = 0;
				foreach (var Anim in DestructionAnims) // find longest duration animation
				{
					var State = Anim[Anim.clip.name];
					MaxDuration = Mathf.Max(MaxDuration, State.length);
				}

				// figure out how much to scale all anims such that longest anim fits in target duration
				bool OverrideRebuildDuration = TargetRebuildDuration > 0 && TargetRebuildDuration < MaxDuration;
				float TargetSpeed = OverrideRebuildDuration ? -MaxDuration / TargetRebuildDuration : -1f;
				
				foreach (var Anim in DestructionAnims) // scale anim durations and play in reverse
				{
					var State = Anim[Anim.clip.name];
					State.speed = TargetSpeed;
					State.normalizedTime = 1;
					Anim.Play(State.name);
				}

				if (AnimCoroutine != null) // clear any existing rebuild coroutines so a new one can be started
					StopCoroutine(AnimCoroutine);
				AnimCoroutine = StartCoroutine(WaitForAnimCompletion(OverrideRebuildDuration ? TargetRebuildDuration : MaxDuration));

				IEnumerator WaitForAnimCompletion(float Duration) // hide destruction and show normal once reverse destruction anim is completed
				{
					yield return new WaitForSeconds(Duration);
					RebuildNow();
				}
			}

			void RebuildNow()
			{
				ShowObject(NormalObject, true);
				ShowObject(DestructionObject, false);
			}
		}

		private void CheckDestroyRebuild(HistoryMarker Marker, bool IsAdded)
		{
			if (Marker.Type != DamageType) // Only change state if Marker's type matches
				return;
			if (IsAdded)
				Destroy();
			else
				Rebuild();
		}

		private static void ShowObject(GameObject TargetObject, bool IsVisible)
		{
			if (TargetObject == null)
				return;

#if UNITY_EDITOR
			if (IsVisible)
				SceneVisibilityManager.instance.Show(TargetObject, true);
			else
				SceneVisibilityManager.instance.Hide(TargetObject, true);

			if (EditorApplication.isPlayingOrWillChangePlaymode)
#endif
			{
				if (TargetObject.activeSelf != IsVisible)
					TargetObject.SetActive(IsVisible);
			}
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(DestructibleCover))]
		private class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				DrawDefaultInspector();

				var Cover = (DestructibleCover)target;
				if (PrefabUtility.IsPartOfPrefabAsset(target)/* || (UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()?.IsPartOfPrefabContents(Cover.gameObject) ?? false)*/)
					return;

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);
				EditorGUILayout.Toggle(nameof(IsDestroyed), Cover.IsDestroyed);
				using (var ChangeCheck = new EditorGUILayout.HorizontalScope())
				{
					using (new EditorGUI.DisabledScope(Cover.IsDestroyed))
						if (GUILayout.Button(nameof(Destroy)))
							Cover.Destroy(!EditorApplication.isPlaying);
					using (new EditorGUI.DisabledScope(!Cover.IsDestroyed))
						if (GUILayout.Button(nameof(Rebuild)))
							Cover.Rebuild(!EditorApplication.isPlaying);
				}
			}
		}
#endif
	}
}