using System;
using System.Collections;
using System.Collections.Generic;
using Tenet.Weapon;
using UnityEngine;
using Tenet.Game;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tenet.Triggers
{
	public class HistoryInfo
	{
		public static readonly HistoryInfo Empty = new HistoryInfo();

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
		[SerializeField] private Transform RendererRoot;

        private readonly Stack<HistoryInfo> History = new Stack<HistoryInfo>();

		private readonly Dictionary<InversionState, GameObject> StateVisuals = new Dictionary<InversionState, GameObject>();
		private GameObject CurrentVisual;
		private bool IsHighlighted;

		public float TriggerRadius => Trigger.radius;

		private void Awake()
		{
			if (DamageType == DamageType.Random)
			{
				DamageType = (DamageType)(UnityEngine.Random.Range(0, (int)DamageType.Random) % NumNonRandomDamageTypes);
			}
			SessionManager.Instance.OnInversionStateChanged += ChangeVisuals;
			ChangeVisuals(SessionManager.Instance.CurrentInversionState);
		}

		private void OnDestroy()
		{
			SessionManager.Instance.OnInversionStateChanged -= ChangeVisuals;
		}

		public void ChangeVisuals(InversionState InversionState)
		{
			if (CurrentVisual != null)
				CurrentVisual.SetActive(false);
			var InversionStateKey = IsHighlighted ? (InversionState) -(int)InversionState : InversionState;
			if (!StateVisuals.TryGetValue(InversionStateKey, out var Visual))
			{
				var Profile = SessionManager.Instance.GameMode.GetInversionStateProfile(InversionState);
				Visual = IsHighlighted ? Profile?.MarkerVisualHighlighted : Profile?.MarkerVisual; // prefab
				if (Visual != null)
				{
					Visual = Instantiate(Visual, RendererRoot.position, RendererRoot.rotation, RendererRoot); // instance
					StateVisuals.Add(InversionStateKey, Visual);
				}
			}
			CurrentVisual = Visual;
			if (!Visual.activeSelf)
				Visual.SetActive(true);
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
			RendererRoot.gameObject.SetActive(IsEnable);
		}

		public DamageType Type => DamageType;

		public IEnumerable<HistoryInfo> GetInfos() => History;

		public HistoryInfo GetLastRecord() => History.Count > 0 ? History.Peek() : HistoryInfo.Empty;

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

		private void OnTriggerEnter(Collider other)
		{
			if (other.GetComponentInParent<Player>())
			{
				IsHighlighted = true;
				ChangeVisuals(SessionManager.Instance.CurrentInversionState);
			}
		}

		private void OnTriggerExit(Collider other)
		{
			if (other.GetComponentInParent<Player>())
			{
				IsHighlighted = false;
				ChangeVisuals(SessionManager.Instance.CurrentInversionState);
			}
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