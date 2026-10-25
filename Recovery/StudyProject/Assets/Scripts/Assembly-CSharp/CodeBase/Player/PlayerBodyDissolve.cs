using Codebase.Player.Models;
using UnityEngine;

namespace Codebase.Player
{
	public class PlayerBodyDissolve : MonoBehaviour
	{
		[SerializeField]
		private SkinnedMeshRenderer body;

		[SerializeField]
		private Vector3 dissolveDirection;

		[SerializeField]
		private float startDelay;

		private static readonly int RevealId;

		private static readonly int EdgeProgressId;

		private MaterialPropertyBlock mpb;

		private Mesh bakedMesh;

		private float reveal;

		private float edgeProgress;

		private PlayerModel player;

		private float Reveal
		{
			get
			{
				return this.reveal;
			}
			set
			{
			}
		}

		private float EdgeProgress
		{
			get
			{
				return this.edgeProgress;
			}
			set
			{
			}
		}

		private void Awake()
		{
		}

		private void Start()
		{
		}

		private void OnDestroy()
		{
		}

		private void BakeDissolveT()
		{
		}

		private static Vector3 LongestAxis(Vector3[] verts)
		{
			return default;
		}

		private void HandleDeathPhase(DeathPhase phase, float duration)
		{
		}

		private void PlayDissolveOut(float duration)
		{
		}

		private void SnapVisible()
		{
		}
	}
}
