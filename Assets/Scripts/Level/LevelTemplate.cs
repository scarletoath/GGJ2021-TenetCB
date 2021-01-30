using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tenet.Level
{
	public enum TileUsage
	{
		__ = 0, // None

		Random,
		Normal,
		Destructible,
		PlayerStart,
		Landmark,
	}

	[Obsolete]
	public class LevelTemplate : ScriptableObject
	{

		[SerializeField] private Vector2Int Dimensions = new Vector2Int(2, 2);
		[SerializeField] private TileUsage[] Layout = Array.Empty<TileUsage>();

		public TileUsage this[int x, int y]
		{
			get
			{
				int Index = y * Dimensions.x + x;
				return Index < Layout.Length ? Layout[Index] : TileUsage.__;
			}
		}

		public int Width => Dimensions.x;
		public int Height => Dimensions.y;

		public IEnumerable<(int x, int y, TileUsage Usage)> GetTileInfos()
		{
			for (int y = 0; y < Dimensions.y; y++)
			{
				for (int x = 0; x < Dimensions.x; x++)
				{
					var Usage = Layout[y * Dimensions.x + x];
					if (Usage != TileUsage.__)
					{
						yield return (x, y, Usage);
					}
				}
			}
		}

		private void Resize(int Width, int Height, TileUsage DefaultUsage = TileUsage.__)
		{
			if (Dimensions.x == Width && Dimensions.y == Height)
				return;

			var NewLayout = new TileUsage[Width * Height];
			int MinWidth = Mathf.Min(Dimensions.x, Width);
			int MinHeight = Mathf.Min(Dimensions.y, Height);
			int MaxWidth = Mathf.Max(Dimensions.x, Width);
			int MaxHeight = Mathf.Max(Dimensions.y, Height);
			for (int y = 0; y < MaxHeight; y++)
			{
				for (int x = 0; x < MaxWidth; x++)
				{
					if (x < MinWidth && y < MinHeight)
						NewLayout[y * Width + x] = Layout[y * Dimensions.x + x];
					else if (x < Width && y < Height)
						NewLayout[y * Width + x] = DefaultUsage;
				}
			}
			Dimensions.Set(Width, Height);
			Layout = NewLayout;
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			if (Dimensions.x * Dimensions.y != Layout.Length)
			{
				var SavedDimensions = Dimensions;
				Dimensions.Set(-1, -1);
				Resize(SavedDimensions.x, SavedDimensions.y);
			}
		}

		[CustomPropertyDrawer(typeof(TileUsage))]
		private class UsageDrawer : PropertyDrawer
		{
			private static readonly float Height = EditorGUIUtility.singleLineHeight * 1.5f;
			public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => Height;
			public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
			{
				var CurrentTileUsage = (TileUsage)property.enumValueIndex;
				var NewTileUsage = (TileUsage)EditorGUI.EnumPopup(position, CurrentTileUsage);
				if (NewTileUsage != CurrentTileUsage)
				{
					property.enumValueIndex = (int)NewTileUsage;
					property.serializedObject.ApplyModifiedProperties();
				}
			}
		}

		[CustomEditor(typeof(LevelTemplate), true)]
		private class Inspector : Editor
		{
			private SerializedProperty DimensionsSP;
			private SerializedProperty LayoutSP;

			private static TileUsage DefaultUsage = TileUsage.Normal;
			private Vector2Int NewDimensions;

			[MenuItem("Level/Create Template")]
			private static void Create()
			{
				string DestPath = AssetDatabase.GetAssetPath(Selection.activeInstanceID);
				if (string.IsNullOrEmpty(DestPath))
					DestPath = "Assets";
				else if (!AssetDatabase.IsValidFolder(DestPath))
					DestPath = Path.GetDirectoryName(DestPath);
				DestPath = EditorUtility.SaveFilePanelInProject("Create Level Template", "NewLevelTemplate", "asset", "Where you want to save the template, dummy.", DestPath);
				if (!string.IsNullOrEmpty(DestPath))
					AssetDatabase.CreateAsset(CreateInstance<LevelTemplate>(), DestPath);
			}

			private void OnEnable()
			{
				DimensionsSP = serializedObject.FindProperty(nameof(Dimensions));
				LayoutSP = serializedObject.FindProperty(nameof(Layout));

				NewDimensions = DimensionsSP.vector2IntValue;
				Undo.undoRedoPerformed += OnUndoRedo;
			}

			private void OnDisable()
			{
				Undo.undoRedoPerformed -= OnUndoRedo;
			}

			private void OnUndoRedo()
			{
				NewDimensions = DimensionsSP.vector2IntValue;
				serializedObject.Update();
			}

			public override void OnInspectorGUI()
			{
				var Template = (LevelTemplate)target;
				using (var ChangeCheck = new EditorGUI.ChangeCheckScope())
				{
					NewDimensions = EditorGUILayout.Vector2IntField(DimensionsSP.displayName, NewDimensions);
					using (new EditorGUI.DisabledScope(NewDimensions == DimensionsSP.vector2IntValue))
					using (new EditorGUILayout.HorizontalScope())
					{
						DefaultUsage = (TileUsage)EditorGUILayout.EnumPopup(nameof(DefaultUsage), DefaultUsage);
						if (GUILayout.Button("Apply"))
						{
							Undo.RecordObject(Template, "Resize Level Template");
							Template.Resize(NewDimensions.x, NewDimensions.y, DefaultUsage);
							serializedObject.Update();
							GUIUtility.ExitGUI();
						}
					}
				}
				EditorGUILayout.Space();

				using (var ChangeCheck = new EditorGUI.ChangeCheckScope())
				{
					EditorGUILayout.LabelField(LayoutSP.displayName);
					int Width = Template.Dimensions.x, Height = Template.Dimensions.y;
					for (int y = Height - 1; y >= 0; y--)
					{
						using (new EditorGUILayout.HorizontalScope())
						{
							for (int x = 0; x < Width; x++)
							{
								EditorGUILayout.PropertyField(LayoutSP.GetArrayElementAtIndex(y * Width + x), true, GUILayout.Width(40));
							}
						}
					}
					if (ChangeCheck.changed)
					{
						serializedObject.ApplyModifiedProperties();
						GUIUtility.ExitGUI();
					}
				}
			}
		}
#endif

	}
}