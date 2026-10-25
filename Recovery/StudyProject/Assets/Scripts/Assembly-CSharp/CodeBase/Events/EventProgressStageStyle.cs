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
			return null;
		}
	}
}
