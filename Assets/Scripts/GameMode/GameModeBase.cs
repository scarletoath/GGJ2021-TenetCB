using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tenet.Game;
using Tenet.GameMode;
using Tenet.Triggers;
using Tenet.Utils.Editor;
using Tenet.Weapon;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tenet.GameMode
{
	[Serializable]
	public class InversionStateProfile
	{
		public string Name; // Matches InversionState enum
		public Color Color;
		public VolumeProfile VolumeProfile;
		public AudioClip SoundClip;
		public GameObject MarkerVisual;
		public GameObject MarkerVisualHighlighted;
	}

	public class GameModeBase : ScriptableObject
    {

		[SerializeField] private InversionStateProfile[] InversionStateProfiles = Array.Empty<InversionStateProfile>();
		[SerializeField] private string[] ReservedTags = { "Landmark", "InversionForward", "InversionBackward" };
		[SerializeField] private string[] GeneralTags = Array.Empty<string>();
		[SerializeField] private HistoryMarker[] MarkerLibrary = Array.Empty<HistoryMarker>();

		private Coroutine InversionRoutine;

		private readonly Dictionary<DamageType, HistoryMarker> MarkerLibraryMap = new Dictionary<DamageType, HistoryMarker>();

		private void OnEnable()
		{
			foreach (var Marker in MarkerLibrary)
			{
				if (Marker.AssociatedDamageType == DamageType.Random) // Do not cache random markers (should not really be added, but just in case)
					continue;
				if (!MarkerLibraryMap.ContainsKey(Marker.AssociatedDamageType))
					MarkerLibraryMap.Add(Marker.AssociatedDamageType, Marker);
				else
					Debug.LogWarning($"Duplicate marker found for DamageType:{Marker.AssociatedDamageType} not added to map.", Marker);
			}
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			TagUtils.DirtyTagsForCategory(TagCategory.ReservedTag);
			TagUtils.DirtyTagsForCategory(TagCategory.GeneralTag);

			var MissingStates = Enum.GetNames(typeof(InversionState)).Where(n => InversionStateProfiles.All(isp => isp.Name != n)).ToArray();
			if (MissingStates.Length > 0)
			{
				InversionStateProfiles = InversionStateProfiles.Concat(MissingStates.Select(s => new InversionStateProfile { Name = s })).ToArray();
			}
		}
#endif

		public InversionStateProfile GetInversionStateProfile(InversionState InversionState) => InversionStateProfiles[Mathf.Clamp((int)InversionState, 0, InversionStateProfiles.Length)];

		public IEnumerable<string> GetReservedTags() => ReservedTags;
		public IEnumerable<string> GetGeneralTags() => GeneralTags;

		public HistoryMarker GetRandomMarker() => MarkerLibrary[UnityEngine.Random.Range(0, MarkerLibrary.Length)];
		public HistoryMarker GetMarker(DamageType DamageType) => DamageType == DamageType.Random
				? GetRandomMarker()
				: MarkerLibraryMap.TryGetValue(DamageType, out var Marker) ? Marker : default;

		public bool CanHeal(InversionState InversionState)
		{
			return InversionState != InversionState.Inverted;
		}

		public float CalculateValue(InversionState InversionState, float Value, float Min, float Max, float Delta)
		{
			switch (SessionManager.Instance.CurrentInversionState)
			{
				case InversionState.Normal:
			        if (Value > Min)
                    {
                        return Mathf.Clamp(Value - Delta, 0, Max);
                    }
					break;
				case InversionState.Inverted:
                    if (Value <= Max)
                    {
                        return Mathf.Clamp(Value + Delta, 0, Max);
                    }
                    break;
			}
			return Value;
		}

		public bool CanUseWeapon(InversionState InversionState, Ammo AmmoType, Transform StartPoint, out HistoryMarker Marker, out AmmoDrop AmmoDrop)
		{
			Marker = null;
			AmmoDrop = null;
			switch (InversionState)
			{
				case InversionState.Normal:
					return true;
				case InversionState.Inverted:
					var HasResult = Physics.Raycast(StartPoint.position, StartPoint.forward, out var Hit, float.MaxValue, 0xFFFF, QueryTriggerInteraction.Collide);
					if (!HasResult)
						return false;

					var Results = Physics.OverlapSphere(Hit.point, 1, 0xFFFF, QueryTriggerInteraction.Collide);
					long NewestTimestamp = long.MinValue;
					foreach (var Result in Results)
					{
						if (Result.TryGetComponent(out AmmoDrop))
						{
							return true;
						}
						if (Result.TryGetComponent(out HistoryMarker ResultMarker) && ResultMarker.GetLastRecord().Timestamp > NewestTimestamp) // assume if it exists it has a record; if no records it should already be removed
						{
							Marker = ResultMarker;
							NewestTimestamp = ResultMarker.GetLastRecord().Timestamp;
						}
					}
					Debug.Log($"Raycast {Results.Length} results at {Hit.point} and found marker={Marker} :\n{string.Join<Collider>("\n", Results)}", Marker);
					return Marker != null && Marker.Type == AmmoType.Type; // check object history
				default:
					return false;
			}
		}

		public void ConsumeAmmo(InversionState InversionState, Ammo Ammo, Transform Origin, HistoryMarker Marker)
		{
			switch (InversionState)
			{
				case InversionState.Normal: // decrease ammo
					Ammo.CreateProjectile(Origin.position, Origin.rotation, null);
					Ammo.Remove();
					break;
				case InversionState.Inverted: // increase ammo
					foreach (var HistoryInfo in Marker.GetInfos()) // Remove marker from all affected targets
					{
						foreach (var Target in HistoryInfo.AffectedTargets)
						{
							Target.UnregisterMarker(Marker);
						}
					}
					int NewAmmoCount = Marker.DequeueAll();
					var InitialDirection = (Origin.position - Marker.transform.position).normalized;
					Ammo.CreateProjectile(Marker.transform.position + InitialDirection * 0.25f, Quaternion.LookRotation(InitialDirection), Origin); // Small depentration to prevent self-collision with original target
					Ammo.Add(NewAmmoCount, true);
					break;
			}
		}

		public bool ShouldAutoReload(InversionState InversionState, Ammo Ammo) => InversionState switch
		{
			InversionState.Normal => Ammo.IsEmpty,
			InversionState.Inverted => Ammo.IsFull,
			_ => false,
		};

		public bool TryReloadAmmo(InversionState InversionState, Ammo Ammo)
		{
			switch (InversionState)
			{
				case InversionState.Normal when !Ammo.IsFull: // max ammo
					Ammo.Refill();
					return true;
				case InversionState.Inverted when !Ammo.IsEmpty: // clear ammo
					Ammo.Clear();
					return true;
			}
			return false;
		}

        public void ApplyInversionEffects(InversionState InversionState, Volume InversionVolume)
        {
			if (InversionRoutine != null)
				SessionManager.Instance.StopCoroutine(InversionRoutine);

			InversionVolume.sharedProfile = GetInversionStateProfile(InversionState).VolumeProfile;
			var TargetClip = GetInversionStateProfile(InversionState).SoundClip;
			if (SessionManager.Instance.BGMAudioSource.clip != TargetClip)
			{
				SessionManager.Instance.BGMAudioSource.clip = TargetClip;
				SessionManager.Instance.BGMAudioSource.Play();
			}
			switch (InversionState)
			{
				case InversionState.Normal:
					break;
				case InversionState.Inverted:
					InversionRoutine = SessionManager.Instance.StartCoroutine(RunInvertedEffects()); ;
					IEnumerator RunInvertedEffects()
					{
						var Config = DifficultySettings.Instance.CurrentDifficulty;
						yield return new WaitForSeconds(Config.InversionMaxDuration);

						var HealthLossInterval = new WaitForSeconds(Config.InversionHealthLossInterval);
						while (true)
						{
							SessionManager.Instance.Player.DamagePercent(Config.InversionHealthLossPercent);
							yield return HealthLossInterval;
						}
					}
					break;
			}
		}

		public void HighlightMarkers(InversionState InversionState, Transform Source, float DetectionRadius)
		{
			switch (InversionState)
			{
				case InversionState.Normal:
					break;
				case InversionState.Inverted:
					var Results = Physics.SphereCastAll(Source.position, DetectionRadius, Source.forward);
					foreach (var Result in Results)
					{
						if (Result.transform.GetComponentInParent<HistoryMarker>() is HistoryMarker Marker)
						{
							Marker.ChangeVisuals(InversionState);
						}
					}
					break;
			}
		}

		public void ConsumeAmmoDrop(InversionState InversionState, AmmoDrop AmmoDrop, Weapon.Weapon Weapon)
		{
			var Position = AmmoDrop.transform.position + UnityEngine.Random.insideUnitSphere;
			var InitialDirection = (Weapon.transform.position - Position).normalized;
			Weapon.CurrentAmmo.CreateProjectile(Position + InitialDirection * 0.25f, Quaternion.LookRotation(InitialDirection), Weapon.transform); // Small depentration to prevent self-collision with original target
			Weapon.CurrentAmmo.Add(1, true);
		}

		public void DestroyAmmoDrop(InversionState InversionState, AmmoDrop AmmoDrop, Player Player)
		{
			switch (InversionState)
			{
				case InversionState.Normal:
					foreach (var Marker in Player.GetAllMarkers())
					{
						var Direction = UnityEngine.Random.insideUnitSphere;
						var Position = AmmoDrop.transform.position + Direction * AmmoDrop.BurstRadius;
						if (Physics.SphereCast(AmmoDrop.transform.position, Marker.TriggerRadius, Direction, out var Hit, AmmoDrop.BurstRadius)) // Collision in direction towards offset
						{
							Position = Hit.point;
							Direction = Hit.normal;
						}
						else if (Physics.SphereCast(Position, Marker.TriggerRadius, Vector3.down, out Hit)) // Thunk collision from offset
						{
							Position = Hit.point;
							Direction = Hit.normal;
						}
						Instantiate(Marker, Position, Quaternion.LookRotation(Direction)).gameObject.SetActive(true);
					}
					Destroy(AmmoDrop.gameObject);
					break;
				case InversionState.Inverted:
					Player.CollectAmmoDrop(AmmoDrop);
					break;
			}
		}
    }
}

namespace Tenet.Utils.Editor
{
	public enum TagCategory
	{
		Difficulty,
		ReservedTag,
		GeneralTag,
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class TagAttribute : PropertyAttribute
	{
		public readonly TagCategory Category;
		public TagAttribute(TagCategory Category) => this.Category = Category;
	}

#if UNITY_EDITOR
	public static class TagUtils
	{
		[CustomPropertyDrawer(typeof(TagAttribute))]
		private class TagAttributeEDrawer : PropertyDrawer
		{
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				var TagAttribute = (TagAttribute)attribute;
				TagField(position, property, GetTagsForCategory(TagAttribute.Category));
			}
		}

		public static string TagField(Rect Position, SerializedProperty Property, string[] Tags)
		{
			string Tag = Property.stringValue;
			int SelectedIndex = Array.IndexOf(Tags, Tag);
			int NewIndex = EditorGUI.Popup(Position, string.Empty, SelectedIndex, Tags);
			if (NewIndex != SelectedIndex)
			{
				Tag = Property.stringValue = Tags[NewIndex];
				Property.serializedObject.ApplyModifiedProperties();
			}
			if (SelectedIndex == -1)
			{
				EditorGUI.LabelField(Position, Tag + "?");
			}
			return Tag;
		}

		private static readonly Dictionary<TagCategory, string[]> TagSets = new Dictionary<TagCategory, string[]>();
		private static string[] GetTagsForCategory(TagCategory Category)
		{
			if (TagSets.TryGetValue(Category, out var Tags))
				return Tags;

			switch (Category)
			{
				case TagCategory.Difficulty:
					Tags = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameManager.prefab")
										.GetComponentInChildren<DifficultySettings>(true).GetDifficulties()
										.Select(d => d.Name)
										.Distinct().OrderBy(n => n).ToArray();
					break;
				case TagCategory.ReservedTag:
					Tags = AssetDatabase.FindAssets($"t:{nameof(GameModeBase)}")
										.Select(AssetDatabase.GUIDToAssetPath)
										.Select(AssetDatabase.LoadAssetAtPath<GameModeBase>)
										.SelectMany(gm => gm.GetReservedTags())
										.Distinct().OrderBy(n => n).ToArray();
					break;
				case TagCategory.GeneralTag:
					Tags = AssetDatabase.FindAssets($"t:{nameof(GameModeBase)}")
										.Select(AssetDatabase.GUIDToAssetPath)
										.Select(AssetDatabase.LoadAssetAtPath<GameModeBase>)
										.SelectMany(gm => gm.GetGeneralTags())
										.Distinct().OrderBy(n => n).ToArray();
					break;
				default:
					return Array.Empty<string>();
			}
			TagSets.Add(Category, Tags);
			return Tags;
		}

		public static void DirtyTagsForCategory(TagCategory Category)
		{
			TagSets.Remove(Category);
		}
	}
#endif
}