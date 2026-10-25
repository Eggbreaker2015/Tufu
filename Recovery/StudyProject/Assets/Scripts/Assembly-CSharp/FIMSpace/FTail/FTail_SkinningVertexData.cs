using System;
using UnityEngine;

namespace FIMSpace.FTail
{
	[Serializable]
	public class FTail_SkinningVertexData
	{
		public Vector3 position;

		public int[] bonesIndexes;

		public int allMeshBonesCount;

		public float[] weights;

		public float[] debugDists;

		public float[] debugDistWeights;

		public float[] debugWeights;

		public FTail_SkinningVertexData(Vector3 pos)
		{
		}

		public float DistanceToLine(Vector3 pos, Vector3 lineStart, Vector3 lineEnd)
		{
			return 0f;
		}

		public void CalculateVertexParameters(Vector3[] bonesPos, Quaternion[] bonesRot, Vector3[] boneAreas, int maxWeightedBones, float spread, Vector3 spreadOffset, float spreadPower = 1f)
		{
		}

		public void AutoSetBoneWeights(float[] weightForBone, float[] distToBone, float spread, float spreadPower, Vector3[] boneAreas)
		{
		}

		public static float DistributionIn(float k, float power)
		{
			return 0f;
		}

		public static Color GetBoneIndicatorColor(int boneIndex, int bonesCount, float s = 0.9f, float v = 0.9f)
		{
			return default;
		}

		public Color GetWeightColor()
		{
			return default;
		}
	}
}
