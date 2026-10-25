using UnityEngine;

namespace Codebase.Enemy
{
	public class SpawnerDifficultyBulkSetter : MonoBehaviour
	{
		[SerializeField]
		private int locationFrom;

		[SerializeField]
		private int locationTo;

		[SerializeField]
		private float maxSourceDifficulty;

		[SerializeField]
		private float targetDifficulty;

		private void Apply()
		{
		}
	}
}
