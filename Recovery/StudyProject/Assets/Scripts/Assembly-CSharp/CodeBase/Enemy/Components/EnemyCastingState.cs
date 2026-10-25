using System;
using Scellecs.Morpeh;

namespace Codebase.Enemy.Components
{
	public struct EnemyCastingState : IComponent
	{
		public float Duration;

		public float StartTime;

		public string AnimationBool;

		public string AnimationTrigger;

		public string SpecialCastKey;

		public Action OnComplete;
	}
}
