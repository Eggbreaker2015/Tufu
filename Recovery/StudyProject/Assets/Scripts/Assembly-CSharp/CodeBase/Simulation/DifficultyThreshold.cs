using System;
using UnityEngine;

namespace Codebase.Simulation
{
	[Serializable]
	public class DifficultyThreshold
	{
		[Range(0f, 200f)]
		public float Threshold;

		public Color Color;
	}
}
