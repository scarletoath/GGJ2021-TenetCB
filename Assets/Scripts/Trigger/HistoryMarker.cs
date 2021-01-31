using System;
using System.Collections;
using System.Collections.Generic;
using Tenet.Weapon;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tenet.Triggers
{
	public class HistoryInfo
	{
		public long Timestamp;
		public readonly HashSet<HistoryTarget> AffectedTargets = new HashSet<HistoryTarget>();

		public bool IsConsumed { get; private set; }
		public HistoryInfo Consume ()
		{
			IsConsumed = true;
			return this;
		}
	}

	public class HistoryMarker : MonoBehaviour
	{

		private static readonly int NumNonRandomDamageTypes = Enum.GetValues(typeof(DamageType)).Length - 1;

		[SerializeField] private DamageType DamageType;
		[SerializeField] private SphereCollider Trigger;

        private readonly Stack<HistoryInfo> History = new Stack<HistoryInfo>();

		public float TriggerRadius => Trigger.radius;

		private void Awake()
		{
			if (DamageType == DamageType.Random)
			{
				DamageType = (DamageType)(UnityEngine.Random.Range(0, (int)DamageType.Random) % NumNonRandomDamageTypes);
			}
		}

		public HistoryMarker FindAtLocation(Vector3 Location)
		{
			var CandidateTriggers = Physics.OverlapSphere(Location, Trigger.radius, 1 << gameObject.layer, QueryTriggerInteraction.Collide);
			float ClosestSqrDistance = float.MaxValue;
			HistoryMarker ClosestMarker = null;
			foreach (var Trigger in CandidateTriggers)
			{
				if (Trigger.TryGetComponent(out HistoryMarker Marker) && Marker.DamageType == DamageType)
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

		public void Enable (bool IsEnable)
		{
			enabled = Trigger.enabled = IsEnable;
		}

		public DamageType Type => DamageType;

		public IEnumerable<HistoryInfo> GetInfos() => History;

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
				EditorGUILayout.Toggle("Enabled", Marker.enabled);
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