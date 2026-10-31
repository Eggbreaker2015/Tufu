using Codebase.Enemy;
using UnityEngine;
using UnityEngine.AI;

namespace Codebase.Unit
{
	public class UnitMovement : MonoBehaviour
	{
		[SerializeField] private NavMeshAgent agent;

		public EnemyUnit VisualTarget { get; set; }
	}
}
