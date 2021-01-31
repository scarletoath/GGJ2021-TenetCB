using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tenet.Triggers
{
    public class HistoryTarget : MonoBehaviour
    {
        private readonly List<HistoryMarker> Markers = new List<HistoryMarker>();

        public void RecordMarker(HistoryMarker Marker)
        {
			Markers.Add(Marker);
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(HistoryTarget))]
		private class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				var Target = (HistoryTarget)target;
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