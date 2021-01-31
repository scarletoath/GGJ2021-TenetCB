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
    public class DestructibleCover : MonoBehaviour
    {

        [SerializeField] private GameObject NormalObject;
        [SerializeField] private GameObject DestructionObject;
		[SerializeField] private float TargetRebuildDuration = 0.3f; // seconds

		[Space]

		[SerializeField] private HistoryTarget HistoryTarget;
		[SerializeField] private DamageType DamageType;

		private Animation[] DestructionAnims;
		private Coroutine AnimCoroutine;

		private void Awake()
		{
			if (DestructionObject != null)
			{
				DestructionAnims = DestructionObject.GetComponentsInChildren<Animation>();
				DestructionObject.SetActive(false);
			}

			HistoryTarget.OnMarkerChanged += CheckDestroyRebuild;
		}

		public bool IsDestroyed { get; private set; }

		public void Destroy()
		{
			if (IsDestroyed)
				return;

			NormalObject.SetActive(false);
			if (DestructionObject != null)
			{
				DestructionObject.SetActive(true);
				foreach (var Anim in DestructionAnims)
				{
					var State = Anim[Anim.clip.name];
					State.speed = 1;
					State.normalizedTime = 0;
					Anim.Play(State.name);
				}

			}
			if (AnimCoroutine != null)
				StopCoroutine(AnimCoroutine);
			AnimCoroutine = null;
			IsDestroyed = true;
		}

		public void Rebuild()
		{
			if (!IsDestroyed)
				return;

			if (DestructionObject != null)
			{
				float MaxDuration = 0;
				foreach (var Anim in DestructionAnims)
				{
					var State = Anim[Anim.clip.name];
					MaxDuration = Mathf.Max(MaxDuration, State.length);
				}

				bool OverrideRebuildDuration = TargetRebuildDuration > 0 && TargetRebuildDuration < MaxDuration;
				float TargetSpeed = OverrideRebuildDuration ? -MaxDuration / TargetRebuildDuration : -1f;
				foreach (var Anim in DestructionAnims)
				{
					var State = Anim[Anim.clip.name];
					State.speed = TargetSpeed;
					State.normalizedTime = 1;
					Anim.Play(State.name);
				}
				if (AnimCoroutine != null)
					StopCoroutine(AnimCoroutine);
				AnimCoroutine = StartCoroutine(WaitForAnimCompletion(OverrideRebuildDuration ? TargetRebuildDuration : MaxDuration));

				IEnumerator WaitForAnimCompletion(float Duration)
				{
					yield return new WaitForSeconds(Duration);
					NormalObject.SetActive(true);
					DestructionObject.SetActive(false);
				}
			}
			else
			{
				NormalObject.SetActive(true);
			}
			IsDestroyed = false;
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

#if UNITY_EDITOR
		[CustomEditor(typeof(DestructibleCover))]
		private class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				DrawDefaultInspector();
				if (!EditorApplication.isPlayingOrWillChangePlaymode)
					return;

				var Cover = (DestructibleCover)target;

				EditorGUILayout.Space();
				EditorGUILayout.Toggle(nameof(IsDestroyed), Cover.IsDestroyed);
				using (var ChangeCheck = new EditorGUILayout.HorizontalScope())
				{
					using (new EditorGUI.DisabledScope(Cover.IsDestroyed))
						if (GUILayout.Button(nameof(Destroy)))
							Cover.Destroy();
					using (new EditorGUI.DisabledScope(!Cover.IsDestroyed))
						if (GUILayout.Button(nameof(Rebuild)))
							Cover.Rebuild();
				}
			}
		}
#endif
	}
}