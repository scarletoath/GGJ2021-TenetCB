using System;
using System.Collections;
using System.Collections.Generic;
using Tenet.Weapon;
using UnityEditor;
using UnityEngine;

namespace Tenet.Triggers
{
	public class HistoryInfo
	{
		public long Timestamp;
		public readonly List<HistoryTarget> AffectedTargets = new List<HistoryTarget>();

		public bool IsConsumed { get; private set; }
		public HistoryInfo Consume ()
		{
			IsConsumed = true;
			return this;
		}
	}

	public class HistoryMarker : MonoBehaviour
	{

		[SerializeField] private SphereCollider Trigger;

        private readonly Stack<HistoryInfo> History = new Stack<HistoryInfo>();

		public Ammo AmmoType { get; private set; }
		public float TriggerRadius => Trigger.radius;

		public HistoryMarker FindAtLocation(Vector3 Location)
		{
			var CandidateTriggers = Physics.OverlapSphere(Location, Trigger.radius, 1 << gameObject.layer, QueryTriggerInteraction.Collide);
			float ClosestSqrDistance = float.MaxValue;
			HistoryMarker ClosestMarker = null;
			foreach (var Trigger in CandidateTriggers)
			{
				if (Trigger.TryGetComponent(out HistoryMarker Marker) && Marker.AmmoType == AmmoType)
				{
					float SqrDistancce = (Trigger.transform.position - Location).sqrMagnitude;
					if (SqrDistancce < ClosestSqrDistance)
					{
						ClosestSqrDistance = SqrDistancce;
						ClosestMarker = Marker;
					}
				}
			}
			return ClosestMarker;
		}

		public void Configure(Ammo AmmoType)
		{
			this.AmmoType = AmmoType;
		}

		public HistoryInfo GetLastRecord() => History.Count > 0 ? History.Peek() : null;

		public HistoryInfo CreateRecord()
		{
			var Info = new HistoryInfo { Timestamp = DateTime.UtcNow.Ticks };
			History.Push(Info);
			return Info;
		}

		public int DequeueAll()
		{
			Debug.Log($"Removed all {History.Count} records from marker {this}", this);
			int Count = History.Count;
			History.Clear();
			Destroy(gameObject);
			return Count;
		}

		public HistoryInfo Dequeue()
		{
			if (History.Count > 0)
			{
				var Info = History.Pop().Consume();
				if (History.Count == 0) // No more records => destroy marker
				{
					Destroy(gameObject);
				}
				return Info;
			}
			return null;
		}

#if UNITY_EDITOR
		[CustomEditor(typeof(HistoryMarker))]
		private class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				DrawDefaultInspector();
				EditorGUILayout.Space();

				var Marker = (HistoryMarker)target;
				EditorGUILayout.ObjectField(nameof(AmmoType), Marker.AmmoType, typeof(Ammo), true);
				EditorGUILayout.LabelField(nameof(History), EditorStyles.boldLabel);
				foreach (var HistoryInfo in Marker.History)
				{
					EditorGUILayout.LabelField($"{HistoryInfo.Timestamp}", $"Targets = {HistoryInfo.AffectedTargets.Count}");
					using (new EditorGUI.IndentLevelScope())
					{
						foreach (var Target in HistoryInfo.AffectedTargets)
						{
							EditorGUILayout.ObjectField(GUIContent.none, Target, typeof(HistoryTarget), true);
						}
					}
				}
			}
		}
#endif
	}
}