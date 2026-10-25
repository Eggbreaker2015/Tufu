using UnityEngine;

namespace FIMSpace.FTail
{
	public static class FTail_Skinning
	{
		public static FTail_SkinningVertexData[] CalculateVertexWeightingData(Mesh baseMesh, Transform[] bonesCoords, Vector3 spreadOffset, int weightBoneLimit = 2, float spreadValue = 0.8f, float spreadPower = 0.185f)
		{
			return null;
		}

		public static FTail_SkinningVertexData[] CalculateVertexWeightingData(Mesh baseMesh, Vector3[] bonesPos, Quaternion[] bonesRot, Vector3 spreadOffset, int weightBoneLimit = 2, float spreadValue = 0.8f, float spreadPower = 0.185f)
		{
			return null;
		}

		public static SkinnedMeshRenderer SkinMesh(Mesh baseMesh, Transform skinParent, Transform[] bonesStructure, FTail_SkinningVertexData[] vertData)
		{
			return null;
		}

		public static SkinnedMeshRenderer SkinMesh(Mesh baseMesh, Vector3[] bonesPositions, Quaternion[] bonesRotations, FTail_SkinningVertexData[] vertData)
		{
			return null;
		}

		public static BoneWeight SetWeightIndex(BoneWeight weight, int bone = 0, int index = 0)
		{
			return default;
		}

		public static BoneWeight SetWeightToBone(BoneWeight weight, int bone = 0, float value = 1f)
		{
			return default;
		}
	}
}
