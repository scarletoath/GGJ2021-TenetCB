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
    public partial class DestructibleCover : MonoBehaviour
    {

		[System.Serializable]
		private class RebuildKeyframe
		{
			[Range(0, 1)] public float NormalizedStartTime;
			[Range(0, 1)] public float NormalizedDuration;
			public float Speed = 1.0f;

			[System.NonSerialized] public float ModifiedStartTime;
			[System.NonSerialized] public float ModifiedEndTime;
		}

		[SerializeField] private GameObject NormalObject;
        [SerializeField] private GameObject DestructionObject;
		[SerializeField] private float TargetRebuildDuration = 0.3f; // seconds
		[Tooltip("Check that keyframes do not overlap (later keyframes must start after previous keyframe's start+duration) and have non-zero speeds.")]
		[SerializeField] private RebuildKeyframe[] RebuildKeyframes = System.Array.Empty<RebuildKeyframe>();

		[Space]

		[SerializeField] private HistoryTarget HistoryTarget;
		[SerializeField] private DamageTypeFlags DamageTypes;

		[Space]

		[SerializeField] private AudioSource AudioSource;

		private Animation[] DestructionAnims = System.Array.Empty<Animation>();
		private Coroutine AnimCoroutine;

		private void Awake()
		{
			Register_Editor();
			if (DestructionObject != null)
				DestructionAnims = DestructionObject.GetComponentsInChildren<Animation>();

			UpdateVisuals(false);

			if (HistoryTarget != null)
				HistoryTarget.OnMarkerChanged += CheckDestroyRebuild;
			if (AudioSource != null)
			{
				AudioSource.playOnAwake = false;
				AudioSource.Stop();
			}
		}

		private void OnDestroy() => Unregister_Editor();

		public bool IsDestroyed { get; private set; }

		public void Destroy(bool IsInstant = false)
		{
			if (IsDestroyed)
				return;

			IsDestroyed = true;

			UpdateVisuals(true);

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
				UpdateVisuals(false);
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
				float TargetSpeed = -1f;
				MaxDuration = CalcModifiedDuration(MaxDuration);
				if (TargetRebuildDuration > 0 && TargetRebuildDuration < MaxDuration)
				{
					TargetSpeed *= MaxDuration / TargetRebuildDuration;
					MaxDuration = TargetRebuildDuration;
				}
				
				foreach (var Anim in DestructionAnims) // scale anim durations and play in reverse
				{
					var State = Anim[Anim.clip.name];
					State.speed = TargetSpeed;
					State.normalizedTime = 1;
					Anim.Play(State.name);
				}

				if (AnimCoroutine != null) // clear any existing rebuild coroutines so a new one can be started
					StopCoroutine(AnimCoroutine);
				AnimCoroutine = StartCoroutine(WaitForAnimCompletion(MaxDuration, TargetSpeed, RebuildKeyframes));

				IEnumerator WaitForAnimCompletion(float Duration, float SpeedMultiplier, RebuildKeyframe[] ModifierKeyframes) // hide destruction and show normal once reverse destruction anim is completed
				{
					Debug.Log($"Playing anim for {name} with duration={Duration:F2}s, base speed={SpeedMultiplier}", this);
					if (ModifierKeyframes.Length > 0)
					{
						float CurrentTime = 0.0f;
						float ModifiedSpeedEndTime = Duration;
						float BaseSpeedScale = Mathf.Abs(SpeedMultiplier);
						foreach (var Keyframe in ModifierKeyframes)
						{
							float TargetTime = Keyframe.ModifiedStartTime / BaseSpeedScale; // Need to scale by base speed in case target duration was applied
							if (TargetTime > ModifiedSpeedEndTime)
							{
								yield return WaitThenChangeSpeed(SpeedMultiplier, ModifiedSpeedEndTime, CurrentTime); // reset speed to -1 if needed (previous segment ends before current)
								CurrentTime = ModifiedSpeedEndTime;
							}
							yield return WaitThenChangeSpeed(Keyframe.Speed * SpeedMultiplier, TargetTime, CurrentTime); // set target speed in reverse (wait til target time if needed)
							CurrentTime = TargetTime;
							ModifiedSpeedEndTime = Keyframe.ModifiedEndTime / BaseSpeedScale; // Need to scale by base speed in case target duration was applied
						}

						if (Duration > ModifiedSpeedEndTime) // Reset speed one more time if last segment ends before full duration
						{
							yield return WaitThenChangeSpeed(SpeedMultiplier, ModifiedSpeedEndTime, CurrentTime);
							CurrentTime = ModifiedSpeedEndTime;
						}
						Duration -= CurrentTime; // Change to remaining duration to reach end of anim

						IEnumerator WaitThenChangeSpeed(float TargetSpeed, float TargetTime, float SeekTime)
						{
							while (TargetTime > SeekTime)
							{
								//yield return new WaitForSeconds(TargetTime - SeekTime);
								yield return null;
								SeekTime += Time.deltaTime;
							}

							foreach (var Anim in DestructionAnims)
							{
								//Debug.Log($"{Anim[Anim.clip.name].name} @ {Anim[Anim.clip.name].normalizedTime:F2} with speed={Anim[Anim.clip.name].speed:F2}x"); // Uncomment for verbose logging
								Anim[Anim.clip.name].speed = TargetSpeed;
							}
							Debug.Log($"Changed speed to {TargetSpeed} for {name} at T+{TargetTime:F2}s", this);
						}
					}

					yield return new WaitForSeconds(Duration); // Wait for end of anim

					UpdateVisuals(false);
				}
				float CalcModifiedDuration(float OriginalDuration)
				{
					if (RebuildKeyframes.Length == 0)
						return MaxDuration;

					float CurrentSeekTime = 0.0f; // Seek on unmodified duration
					float CurrentModifiedSeekTime = 0.0f; // Seek on modified duration
					float NewDuration = 0.0f; // Start unmodified and apply deltas from modified segments
					foreach (var Keyframe in RebuildKeyframes)
					{
						float StartTime = MaxDuration * Keyframe.NormalizedStartTime;
						float SegmentDuration = MaxDuration * Keyframe.NormalizedDuration;
						Debug.Assert(StartTime >= CurrentSeekTime, "Cannot apply invalid keyframe with reverse normalized times.");

						float ModifiedStartTime = CurrentModifiedSeekTime;
						if (StartTime > CurrentSeekTime) // Add unmodified duration if desired start is after current seek
						{
							float UnmodifiedDurationBeforeKeyframe = StartTime - CurrentSeekTime;
							ModifiedStartTime += UnmodifiedDurationBeforeKeyframe;
							NewDuration += UnmodifiedDurationBeforeKeyframe;
						}

						NewDuration += SegmentDuration / Keyframe.Speed;
						CurrentSeekTime = StartTime + SegmentDuration; // Move seek time to end of unmodified segment
						CurrentModifiedSeekTime = NewDuration; // Move modified seek time to end of modified segment

						Keyframe.ModifiedStartTime = ModifiedStartTime;
						Keyframe.ModifiedEndTime = CurrentModifiedSeekTime;
					}
					Debug.Log($"Modified duration from {OriginalDuration:F2}s to {NewDuration:F2}s for {name}", this);
					return NewDuration;
				}
			}
		}

		private void CheckDestroyRebuild(HistoryMarker Marker, bool IsAdded)
		{
			if (!DamageTypes.HasFlag(Marker.Type)) // Only change state if Marker's type matches
				return;
			if (IsAdded)
				Destroy();
			else
				Rebuild();
		}

		private void UpdateVisuals(bool IsDestroyed, bool IsHiddenOverride = false)
		{
			ShowObject(NormalObject, !IsHiddenOverride && !IsDestroyed);
			ShowObject(DestructionObject, !IsHiddenOverride && IsDestroyed);
		}

		private void ShowObject(GameObject TargetObject, bool IsVisible)
		{
			if (TargetObject == null)
				return;

			ShowObject_Editor(TargetObject, IsVisible);
#if UNITY_EDITOR
			if (EditorApplication.isPlayingOrWillChangePlaymode && (!UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()?.IsPartOfPrefabContents(TargetObject) ?? true))
#endif
			{
				if (TargetObject.activeSelf != IsVisible)
					TargetObject.SetActive(IsVisible);
			}
		}

		#region Scene Visibility Interface
		partial void Register_Editor();
		partial void Unregister_Editor();
		partial void ShowObject_Editor(GameObject TargetObject, bool IsVisible);
		#endregion

#if UNITY_EDITOR
		#region Scene Visibility Utils
		partial void Register_Editor() => SceneVisibilityUtils.Covers.Add(this);
		partial void Unregister_Editor() => SceneVisibilityUtils.Covers.Remove(this);
		partial void ShowObject_Editor(GameObject TargetObject, bool IsVisible)
		{
			if (IsVisible)
				SceneVisibilityManager.instance.Show(TargetObject, true);
			else
				SceneVisibilityManager.instance.Hide(TargetObject, true);
		}

		private static class SceneVisibilityUtils
		{
			internal static readonly HashSet<DestructibleCover> Covers = new HashSet<DestructibleCover>();
			static SceneVisibilityUtils() => SceneVisibilityManager.visibilityChanged += UpdateVisibility;
			private static bool IsUpdated = true;
			private static void UpdateVisibility()
			{
				if (IsUpdated) // Skip the next visibility update as it was triggered by this very call, otherwise stuck in infinite visibility update loop
				{
					IsUpdated = false;
					return;
				}

				IsUpdated = true;
				var Temp = new List<(DestructibleCover Cover, bool IsHidden)>(Covers.Count);
				foreach (var Cover in Covers)
					Temp.Add((Cover, SceneVisibilityManager.instance.AreAllDescendantsHidden(Cover.gameObject)));
				foreach (var (Cover, IsHidden) in Temp)
					Cover.UpdateVisuals(Cover.IsDestroyed, IsHidden);
			}
		}
		#endregion

		[CustomEditor(typeof(DestructibleCover))]
		private class Inspector : Editor
		{

			[CustomPropertyDrawer(typeof(RebuildKeyframe))]
			private class KeyframeDrawer : PropertyDrawer
			{
				internal static bool ShowModifiedTimes = true;
				internal static readonly Dictionary<Object, Dictionary<string, string>> CachedModifiedTimes = new Dictionary<Object, Dictionary<string, string>>();

				public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
				{
					return EditorGUI.GetPropertyHeight(property, label) + (property.isExpanded && ShowModifiedTimes ? EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing : 0.0f);
				}

				public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
				{
					EditorGUI.PropertyField(position, property, label, true);
					if (property.isExpanded && ShowModifiedTimes && 
						CachedModifiedTimes.TryGetValue(property.serializedObject.targetObject, out var TargetModifiedTimes) && 
						TargetModifiedTimes.TryGetValue(property.propertyPath, out string ModifiedTime))
						using (new EditorGUI.IndentLevelScope())
							EditorGUI.LabelField(new Rect(position) { yMin = position.yMax - EditorGUIUtility.singleLineHeight }, "Modified Time Range", ModifiedTime);
				}
			}

			private SerializedProperty KeyframesSP;

			private void OnEnable() => KeyframesSP = serializedObject.FindProperty(nameof(RebuildKeyframes));

			private void OnDisable() => KeyframeDrawer.CachedModifiedTimes.Remove(target);

			private bool IsInPrefabMode => UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()?.IsPartOfPrefabContents(((DestructibleCover)target).gameObject) ?? false;

			public override void OnInspectorGUI()
			{
				DrawDefaultInspector();

				var Cover = (DestructibleCover)target;
				if (PrefabUtility.IsPartOfPrefabAsset(target))
					return;

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Debug Controls", EditorStyles.boldLabel);
				EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(nameof(IsDestroyed)), Cover.IsDestroyed);
				//KeyframeDrawer.ShowModifiedTimes = EditorGUILayout.Toggle(ObjectNames.NicifyVariableName(nameof(KeyframeDrawer.ShowModifiedTimes)), KeyframeDrawer.ShowModifiedTimes); // Disabled to always show due to bug in ReorderableList caching height values
				if(KeyframeDrawer.ShowModifiedTimes)
				{
					if (!KeyframeDrawer.CachedModifiedTimes.TryGetValue(target, out var CachedModifiedTimes))
						KeyframeDrawer.CachedModifiedTimes.Add(target, CachedModifiedTimes = new Dictionary<string, string>());

					if (CachedModifiedTimes.Count != Cover.RebuildKeyframes.Length)
						CachedModifiedTimes.Clear();

					for (int Index = 0; Index < Cover.RebuildKeyframes.Length; Index++)
					{
						var Keyframe = Cover.RebuildKeyframes[Index];
						CachedModifiedTimes[KeyframesSP.GetArrayElementAtIndex(Index).propertyPath] = $"{Keyframe.ModifiedStartTime:F2}s -> {Keyframe.ModifiedEndTime:F2}s";
					}
				}

				using (var ChangeCheck = new EditorGUILayout.HorizontalScope())
				{
					using (new EditorGUI.DisabledScope(Cover.IsDestroyed))
						if (GUILayout.Button(nameof(Destroy)))
							Cover.Destroy(!EditorApplication.isPlaying || IsInPrefabMode);
					using (new EditorGUI.DisabledScope(!Cover.IsDestroyed))
						if (GUILayout.Button(nameof(Rebuild)))
							Cover.Rebuild(!EditorApplication.isPlaying || IsInPrefabMode);
				}
			}
		}
#endif
	}
}