using System.Collections.Generic;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Particles
{
	public class SoftParticlesGpuWorkaround : MonoBehaviour
	{
		private static readonly int SoftParticlesEnabledId;

		private const string SoftParticlesKeyword = "_FADING_ON";

		private static readonly HashSet<int> ProcessedMaterialIds;

		[SerializeField]
		private List<Material> materials;

		private void Awake()
		{
		}

		private static bool IsAffectedDevice()
		{
			return false;
		}

		private void FindMaterials()
		{
		}

		[StaticReset]
		private static void ResetStaticState()
		{
		}
	}
}
