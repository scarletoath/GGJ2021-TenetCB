using System.Collections;
using System.Collections.Generic;
using Tenet.Game;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Tenet.NPC
{
    public partial class Target : MonoBehaviour, Game.IHealth
	{
		float health;
		[SerializeField] float maxHealth;
		[SerializeField] bool isForward;

		//float moveSpeed;
		//float turnSpeed;
		//bool isNormalPatrol; //TBC, check if this is a thing
		Turret turret							= null;
		[SerializeField] float detectionRadius	= 0.0f;
		[SerializeField] GameObject ForwardDeathParticle = null;
		[SerializeField] GameObject InverseDeathParticle = null;

		// Start is called before the first frame update
		void Start()
		{
			turret	= GetComponent<Turret>();
			health	= maxHealth;

			if (turret == null)
			{
				gameObject.SetActive(false);
				Debug.LogWarning("Disabling Target as no turret available.", this);
			}
		}

		private void Update()
		{
			var Player = SessionManager.Instance.Player;
			var Position = transform.position;
			bool IsInRange = Vector3.Distance(Position, Player.ClosestPoint(Position, true)) <= detectionRadius;
			if (IsInRange)
			{
				var PlayerPosition = Player.transform.position;
				bool HasLineOfSight = Player.Raycast(new Ray(Position, (PlayerPosition - Position).normalized), out var Hit, detectionRadius);
				if (HasLineOfSight)
					turret.StartShootingAtPosition(PlayerPosition);

				UpdateDebugInfo_Editor(IsInRange, HasLineOfSight, Position, Hit.point);
			}
			else
			{
				UpdateDebugInfo_Editor(false, false, default, default);
			}
		}

		void OnDeath()
		{
			Object particle = isForward ? ForwardDeathParticle : InverseDeathParticle;
			Instantiate( particle, transform.position, Quaternion.identity);
			Destroy(gameObject);
		}

		// IHealth interface functions
		public float CurrentHealth => health;
		public float Damage( float Amount ) => ChangeHealth( -Amount );
		public float DamagePercent( float Percent ) => Damage( Percent * maxHealth );
		public float Heal( float Amount ) { return 0.0f; }
		public float HealPercent( float Percent ) { return 0.0f; }
		
		private float ChangeHealth( float Amount )
		{
			if( !Mathf.Approximately( Amount, 0.0f ) )
			{
				float PreviousHealth = health;
				health = Mathf.Clamp( health + Amount, 0, maxHealth );
			}

			Debug.Log("TestTarget Health : " + health);
			if(health <= 0.0f)
			{
				OnDeath();
			}

			return health;
		}

		#region Inspector
		partial void UpdateDebugInfo_Editor(bool IsInRange, bool HasLineOfSight, Vector3 Position, Vector3 HitPoint);

#if UNITY_EDITOR
		private bool ShowDebug;
		private bool IsInRange;
		private bool HasLineOfSight;
		partial void UpdateDebugInfo_Editor(bool IsInRange, bool HasLineOfSight, Vector3 Position, Vector3 HitPoint)
		{
			if (ShowDebug)
			{
				this.IsInRange = IsInRange;
				this.HasLineOfSight = HasLineOfSight;
				if (IsInRange)
					Debug.DrawLine(Position, HitPoint, HasLineOfSight ? Color.green : Color.red, 2.0f);
			}
		}

		private void OnDrawGizmos()
		{
			if (ShowDebug && IsInRange)
			{
				Handles.Label(transform.position + Vector3.up, $"{(IsInRange ? "In Range" : string.Empty)}\n{(HasLineOfSight ? "Has LOS" : string.Empty)}");
			}
		}

		[CustomEditor(typeof(Target)), CanEditMultipleObjects]
		private class Inspector : Editor
		{
			public override void OnInspectorGUI()
			{
				base.OnInspectorGUI();
				if (!EditorApplication.isPlayingOrWillChangePlaymode)
					return;

				EditorGUILayout.Space();
				if (serializedObject.isEditingMultipleObjects)
				{
					var Targets = serializedObject.targetObjects;
					bool ShowDebugAll = ((Target)Targets[0]).ShowDebug;
					for (int i = 1; i < Targets.Length; i++)
						if(((Target)Targets[i]).ShowDebug != ShowDebugAll)
						{
							EditorGUI.showMixedValue = true;
							break;
						}

					using (var ChangeCheck = new EditorGUI.ChangeCheckScope())
					{
						bool NewShowDebugAll = EditorGUILayout.Toggle("Debug Info (All)", ShowDebugAll);
						if (ChangeCheck.changed)
							foreach (Target Target in Targets)
								Target.ShowDebug = NewShowDebugAll;
					}
					EditorGUI.showMixedValue = false;
				}
				else
				{
					var Target = (Target)target;
					Target.ShowDebug = EditorGUILayout.BeginFoldoutHeaderGroup(Target.ShowDebug, "Debug Info");
					if (Target.ShowDebug)
					{
						EditorGUILayout.Toggle(nameof(IsInRange), Target.IsInRange);
						EditorGUILayout.Toggle(nameof(HasLineOfSight), Target.HasLineOfSight);
					}
				}
				EditorGUILayout.EndFoldoutHeaderGroup();
			}
		}
#endif
		#endregion
	}

}