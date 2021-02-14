using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using Tenet.Triggers;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tenet.Weapon
{

	public class Ammo : MonoBehaviour
    {

		[SerializeField] private DamageType DamageType = DamageType.Normal;

		[Space]

        [SerializeField] private int MaxCount = 15;
        [SerializeField] private int ClipCount = 15;

        [Space]

        [SerializeField] private Projectile Projectile;
        [SerializeField] private float Damage = 10.0f;
        [SerializeField] private float DamageRadius = 0.0f;

        [Space]

        [SerializeField] private HistoryMarker Marker;

        private int SpareCount;
        private int CurrentCount;

		private void Awake()
		{
			Marker.gameObject.SetActive(false);
            SpareCount = MaxCount;
			Refill();
		}

		public void Configure(int InClipCount, int TotalCount)
		{
			Debug.Log($"{name}/{Type} configured with in clip = {InClipCount}, total = {TotalCount}", this);
			CurrentCount = InClipCount;
			SpareCount = TotalCount;
		}

		public bool IsPlayerOwned { get; set; }
		public DamageType Type => DamageType;
		public HistoryMarker MarkerPrefab => Marker;

		public bool IsEmpty => CurrentCount == 0;
        public bool IsFull => CurrentCount == ClipCount;

		public void Refill() => SpareCount -= Add(Mathf.Clamp(ClipCount - CurrentCount, 0, SpareCount));
		public void Clear() => SpareCount -= Remove(CurrentCount);

		public int Add(int Change = 1, bool TransferOverflow = false) => ChangeCount(Change,  TransferOverflow);
        public int Remove(int Change = 1, bool TransferUnderflow = false) => ChangeCount(-Change, TransferUnderflow);

		private void OnGUI()
		{
			if (IsPlayerOwned)
				using (new GUILayout.AreaScope(new Rect(Screen.width - 200, 0, 200, 50), string.Empty, GUI.skin.box))
				{
					GUILayout.Label($"Ammo Type {name}\n{CurrentCount} / {ClipCount}, {SpareCount}");
				}
		}

		public Projectile CreateProjectile (Vector3 Position, Quaternion Rotation, Transform Target)
        {
            var ProjectileInstance = Instantiate(Projectile, Position, Rotation);
            ProjectileInstance.Configure(this, Target);
            return ProjectileInstance;
		}

        public HistoryMarker GetOrCreateMarker (Vector3 Position, Vector3 Direction)
        {
            var MarkerInstance = Marker.FindAtLocation(Position);
			if (MarkerInstance == null)
			{
				MarkerInstance = Instantiate(Marker, Position, Quaternion.LookRotation(Direction));
				MarkerInstance.gameObject.SetActive(true);
			}
            return MarkerInstance;
		}

		public void ApplyDamage(GameObject TargetGameObject, Vector3 TargetPoint, Vector3 Direction)
		{
            var Marker = GetOrCreateMarker(TargetPoint, Direction);
			Marker.CreateRecord();
			if (Marker.TriggerRadius <= 0)
			{
                ApplyDamage(TargetGameObject, Marker);
			}
			else
			{
                ApplyDamage(TargetPoint, Marker);
			}
		}

        private bool ApplyDamage(GameObject TargetGameObject, HistoryMarker Marker)
        {
			if (TargetGameObject != null)
			{
				if (TargetGameObject.GetComponentInParent<HistoryTarget>() is HistoryTarget Target) // TODO : Also check if can affect target; e.g. only explosive can affect destructible target
				{
					Target.RegisterMarker(Marker);
					Marker.GetLastRecord().AffectedTargets.Add(Target);
				}

				// 9 is player, 10 is enemy
				if( TargetGameObject.layer >= 9 )
				{ }

				if (TargetGameObject.GetComponentInParent<IHealth>() is IHealth IHealth)
				{
					Debug.Log( "TargetGameObject.layer : " + TargetGameObject.layer + " | IsPlayerOwned? " + IsPlayerOwned);
					if( TargetGameObject.layer == 9 && !IsPlayerOwned )
					{
						if ( IHealth.Damage( Damage ) <= 0.0f )
						{
							SessionManager.Instance.EndLevel(false);
						}
					}
					else if( TargetGameObject.layer == 10 && IsPlayerOwned)
					{
						IHealth.Damage(Damage);
					}
					return true;
				}
			}

			return false;
		}

        private bool ApplyDamage(Vector3 TargetPoint, HistoryMarker Marker)
        {
            var Colliders = Physics.OverlapSphere(TargetPoint, Marker.TriggerRadius);
            bool HasApplied = false;
			foreach (var Collider in Colliders)
			{
				HasApplied |= ApplyDamage( Collider.gameObject, Marker );
			}
            return HasApplied;
		}

		private int ChangeCount(int Change, bool AllowTransfer)
        {
			if (AllowTransfer)
			{
				CurrentCount += Change;
				if (CurrentCount > ClipCount) // Overflow into spare
				{
					SpareCount += CurrentCount - (CurrentCount %= ClipCount);
				}
				else if (CurrentCount < 0)
				{
					int DeltaFullClip = CurrentCount % ClipCount;
					int DeltaSpareCount = (CurrentCount - ClipCount) - DeltaFullClip;
					CurrentCount = ClipCount + DeltaFullClip; // delta is negative
					SpareCount += DeltaSpareCount; // delta is negative
				}
			}
			else
			{
				Change = Mathf.Clamp(Change, -CurrentCount, ClipCount - CurrentCount + 1);
				CurrentCount += Change;
			}
            return Change;
		}
    }

	#region Damage Type
	public enum DamageType
	{
		Normal,
		Explosive,

		Random = 1000,
	}

	[System.Serializable]
	public class DamageTypeFlags
	{
		[SerializeField] private DamageType[] _DamageTypes = System.Array.Empty<DamageType>();
		public IList<DamageType> DamageTypes => _DamageTypes;
		public bool HasFlag(DamageType DamageType) => System.Array.IndexOf(_DamageTypes, DamageType) != -1;

#if UNITY_EDITOR
		[CustomPropertyDrawer(typeof(DamageTypeFlags))]
		public class DamageTypeFlagsAttributeDrawer : PropertyDrawer
		{
			private class DamageTypeInfo
			{
				private readonly DamageType DamageType;
				private readonly GUIContent Label;
				public int Index = -1;
				public bool IsSelected => Index != -1;
				public DamageTypeInfo(DamageType DamageType) => (this.DamageType, this.Label) = (DamageType, new GUIContent(DamageType.ToString()));
				public void Clear() => Index = -1;
				public void AddToMenu(GenericMenu Menu, SerializedProperty ArrayProp) => Menu.AddItem(Label, IsSelected, ToggleSelection, ArrayProp);
				private void ToggleSelection(object Data)
				{
					var ArrayProp = (SerializedProperty)Data;
					if (IsSelected)
					{
						ArrayProp.DeleteArrayElementAtIndex(Index);
					}
					else
					{
						ArrayProp.InsertArrayElementAtIndex(ArrayProp.arraySize);
						ArrayProp.GetArrayElementAtIndex(ArrayProp.arraySize - 1).intValue = (int)DamageType;
					}
					ArrayProp.serializedObject.ApplyModifiedProperties();
				}
			}

			private static readonly GUIContent Label_None = new GUIContent("None");
			private static readonly GUIContent Label_Everything = new GUIContent("Everything");
			private static readonly Dictionary<DamageType, DamageTypeInfo> DamageTypes = ((DamageType[])System.Enum.GetValues(typeof(DamageType))).Except(new[] { DamageType.Random }).ToDictionary(t => t, t => new DamageTypeInfo(t));

			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				EditorGUI.PrefixLabel(position, label);
				
				property = property.FindPropertyRelative(nameof(_DamageTypes));
				switch (property.arraySize)
				{
					case 0: label.text = "None"; break;
					case 1 when property.GetArrayElementAtIndex(0) is SerializedProperty firstProp : label.text = firstProp.enumDisplayNames[firstProp.enumValueIndex]; break;
					default: label.text = property.arraySize == DamageTypes.Count ? "All" : "Mixed ..."; break;
				}

				var DropdownPosition = EditorGUI.IndentedRect(new Rect(position) { xMin = EditorGUIUtility.labelWidth + 20 });
				if (EditorGUI.DropdownButton(DropdownPosition, label, FocusType.Passive))
				{
					foreach (var Info in DamageTypes)
						Info.Value.Clear();

					var Menu = new GenericMenu();
					Menu.AddItem(Label_None, property.arraySize == 0, RemoveEverything, property);
					Menu.AddItem(Label_Everything, property.arraySize == DamageTypes.Count, AddEverything, property);
					Menu.AddSeparator(string.Empty);
					for (int i = 0; i < property.arraySize; i++)
						if (DamageTypes.TryGetValue((DamageType)property.GetArrayElementAtIndex(i).intValue, out var Info))
							Info.Index = i;

					foreach (var Info in DamageTypes)
						Info.Value.AddToMenu(Menu, property);

					Menu.DropDown(DropdownPosition);

					void AddEverything(object Data)
					{
						var ArrayProp = (SerializedProperty)Data;
						ArrayProp.arraySize = DamageTypes.Count;
						int Index = 0;
						foreach (var Info in DamageTypes)
							ArrayProp.GetArrayElementAtIndex(Index++).intValue = (int)Info.Key;
						ArrayProp.serializedObject.ApplyModifiedProperties();
					}
					void RemoveEverything(object Data)
					{
						var ArrayProp = (SerializedProperty)Data;
						ArrayProp.arraySize = 0;
						ArrayProp.serializedObject.ApplyModifiedProperties();
					}
				}
			}
		}
#endif
	}
	#endregion

}