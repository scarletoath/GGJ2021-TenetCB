using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tenet.Environment
{
    public class DestructibleCover : MonoBehaviour
    {

        [SerializeField] private GameObject NormalObject;
        [SerializeField] private GameObject DestructionObject;

		private Animation[] DestructionAnims;
		private Coroutine AnimCoroutine;

		private void Awake()
		{
			DestructionAnims = DestructionObject.GetComponentsInChildren<Animation>();
			DestructionObject.SetActive(false);
		}

		public bool IsDestroyed { get; private set; }

		public void Destroy()
		{
			if (IsDestroyed)
				return;

			NormalObject.SetActive(false);
			DestructionObject.SetActive(true);
			foreach (var Anim in DestructionAnims)
			{
				var State = Anim[Anim.clip.name];
				State.speed = 1;
				State.normalizedTime = 0;
				Anim.Play(State.name);
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

			float MaxDuration = 0;
			foreach (var Anim in DestructionAnims)
			{
				var State = Anim[Anim.clip.name];
				State.speed = -1;
				State.normalizedTime = 1;
				MaxDuration = Mathf.Max(MaxDuration, State.length);
				Anim.Play(State.name);
			}
			if (AnimCoroutine != null)
				StopCoroutine(AnimCoroutine);
			AnimCoroutine = StartCoroutine(WaitForAnimCompletion(MaxDuration));
			IsDestroyed = false;

			IEnumerator WaitForAnimCompletion (float Duration)
			{
				yield return new WaitForSeconds(Duration);
				NormalObject.SetActive(true);
				DestructionObject.SetActive(false);
			}
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