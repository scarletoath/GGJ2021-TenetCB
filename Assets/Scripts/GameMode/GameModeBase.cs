using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tenet.Game;
using Tenet.GameMode;
using Tenet.Utils.Editor;
using Tenet.Weapon;
using UnityEditor;
using UnityEngine;

namespace Tenet.GameMode
{
    public class GameModeBase : ScriptableObject
    {

		[SerializeField] private Color[] InversionStateColors = new Color[Enum.GetValues(typeof(InversionState)).Length];
		[SerializeField] private string[] ReservedTags = { "Landmark", "InversionForward", "InversionBackward" };
		[SerializeField] private string[] GeneralTags = Array.Empty<string>();

		private Coroutine InversionRoutine;

#if UNITY_EDITOR
		private void OnValidate()
		{
			TagUtils.DirtyTagsForCategory(TagCategory.ReservedTag);
			TagUtils.DirtyTagsForCategory(TagCategory.GeneralTag);
		}
#endif

		public Color GetInversionStateColor(InversionState InversionState) => InversionStateColors[Mathf.Clamp((int)InversionState, 0, InversionStateColors.Length)];

		public IEnumerable<string> GetReservedTags() => ReservedTags;
		public IEnumerable<string> GetGeneralTags() => GeneralTags;

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

		public bool CanUseWeapon(InversionState InversionState)
		{
			switch (InversionState)
			{
				case InversionState.Normal:
					return true;
				case InversionState.Inverted:
					return true; // check object history
				default:
					return false;
			}
		}

		public void ConsumeAmmo(InversionState InversionState, Ammo Ammo)
		{
			switch (InversionState)
			{
				case InversionState.Normal: // decrease ammo
					Ammo.Remove();
					break;
				case InversionState.Inverted: // increase ammo
					Ammo.Add();
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

        public void ApplyInversionEffects(InversionState InversionState)
        {
			if (InversionRoutine != null)
				SessionManager.Instance.StopCoroutine(InversionRoutine);

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