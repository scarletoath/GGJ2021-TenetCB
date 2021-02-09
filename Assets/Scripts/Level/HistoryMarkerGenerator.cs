using System.Collections;
using System.Collections.Generic;
using Tenet.Weapon;
using UnityEngine;
using Tenet.Game;
using Tenet.Triggers;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tenet.Level
{
    public class HistoryMarkerGenerator : MonoBehaviour , IHistoryMarker
    {

        [SerializeField] private DamageType DamageType = DamageType.Random;
        public DamageType AssociatedDamageType => DamageType;

        private HistoryMarker GeneratedMarker;

        private void Start()
        {
            var Marker = SessionManager.Instance?.GameMode.GetMarker(DamageType);
			if (Marker != null)
                GeneratedMarker = Instantiate(Marker, transform, false);
        }

#if UNITY_EDITOR
		private void OnDrawGizmos()
		{
			const float Size = 0.2f;
			/*using (new Handles.DrawingScope(new Color(0.2f, 0.5f, 0, 0.5f)))
				if (Handles.Button(transform.position - transform.forward * Size / 2.0f, Quaternion.Inverse(transform.rotation), Size, Size * 0, Handles.ConeHandleCap))
					Selection.activeObject = this;*/
			var m = Gizmos.matrix;
			Gizmos.matrix = Matrix4x4.Scale(Vector3.one * 0.1f) * m;
			Gizmos.DrawIcon(transform.position, "Assets/Gizmos/clock-128.png", true, new Color(0.4f, 1f, 0, 1f));
			Gizmos.matrix = m;
		}

		[CustomEditor(typeof(HistoryMarkerGenerator))]
		private class Inspector : Editor
        {
			public override void OnInspectorGUI()
			{
                DrawDefaultInspector();
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Debug Info", EditorStyles.boldLabel);
				
				var GeneratedMarker = ((HistoryMarkerGenerator)target).GeneratedMarker;
				EditorGUILayout.ObjectField(nameof(HistoryMarkerGenerator.GeneratedMarker), GeneratedMarker, typeof(HistoryMarker), true);
				EditorGUILayout.EnumPopup("Generated " + nameof(DamageType), GeneratedMarker?.AssociatedDamageType ?? DamageType.Random);
			}
		}
#endif
	}
}