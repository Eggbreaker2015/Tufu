using System;
using UnityEngine;

namespace Codebase.Events
{
	[Serializable]
	public class EventProgressStageStyle
	{
		public Sprite NotPassed;

		public Sprite Passed;

		public Sprite Failed;

		public Sprite GetSprite(ProgressStageState state)
		{
			// Original APK ARM64 ELF VA 0x14daee0.
			return state == ProgressStageState.Passed ? Passed : state == ProgressStageState.Failed ? Failed : NotPassed;
		}
	}
}
