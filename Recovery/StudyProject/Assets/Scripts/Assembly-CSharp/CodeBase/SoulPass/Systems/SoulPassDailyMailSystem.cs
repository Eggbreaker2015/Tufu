using System;
using Codebase.Core;
using Codebase.Services.Time;
using Scellecs.Morpeh;

namespace Codebase.SoulPass.Systems
{
	public class SoulPassDailyMailSystem : SystemBase
	{
		[Inject]
		private ITimeService timeService;

		private DateTime lastCheckedDate;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
