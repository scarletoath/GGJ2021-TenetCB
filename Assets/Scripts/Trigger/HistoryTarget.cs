using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tenet.Triggers
{
    public class HistoryTarget : MonoBehaviour
    {
        private readonly HashSet<HistoryMarker> Markers = new HashSet<HistoryMarker>();

		public event System.Action<HistoryMarker, bool> OnMarkerChanged;

		public void Enable (bool IsEnable)
		{
			enabled = IsEnable;
			foreach (var Marker in Markers)
			{
				Marker.Enable(IsEnable);
			}
		}

        public void RegisterMarker(HistoryMarker Marker)
        {
			if (Markers.Add(Marker))
				OnMarkerChanged?.Invoke(Marker, true);
		}

		public void UnregisterMarker(HistoryMarker Marker)
		{
			if (Markers.Remove(Marker))
				OnMarkerChanged?.Invoke(Marker, false);
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(HistoryTarget))]
		private class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				var Target = (HistoryTarget)target;

				using (var ChangeCheck = new EditorGUI.ChangeCheckScope())
				{
					bool NewEnabled = EditorGUILayout.Toggle("Enabled", Target.enabled);
					if (ChangeCheck.changed)
					{
						Target.Enable(NewEnabled);
					}
				}

				EditorGUILayout.Space();
				EditorGUILayout.LabelField(nameof(Markers));
				foreach (var Marker in Target.Markers)
				{
					EditorGUILayout.ObjectField(GUIContent.none, Marker, typeof(HistoryMarker), true);
				}
			}
		}
#endif
	}
}