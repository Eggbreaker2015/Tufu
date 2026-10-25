using Codebase.Core;
using Codebase.Services.Time;
using Scellecs.Morpeh;

namespace Codebase.Crystals.Systems
{
	public class GeodeTimerSystem : SystemBase
	{
		[Inject]
		private CrystalModel crystalModel;
		[Inject]
		private ITimeService timeService;
		private float checkInterval;
		private float timer;
		public override void OnAwake()
		{
			ProcessOfflineGeodes();
		}

		public override void OnUpdate(float deltaTime)
		{
			timer += deltaTime;
			if (timer < checkInterval)
				return;
			timer = 0f;
			crystalModel.ProcessPendingGeodes();
			crystalModel.ResetDailyEnchantmentStones();
		}

		private void ProcessOfflineGeodes()
		{
			if (crystalModel.Data.LastGeodeTimestamp.Value <= 0)
			{
				crystalModel.Data.LastGeodeTimestamp.Value = timeService.Now.ToDateTime().Ticks;
				return;
			}

			crystalModel.ProcessPendingGeodes();
			crystalModel.ResetDailyEnchantmentStones();
		}

		public GeodeTimerSystem()
		{
			checkInterval = 1f;
		}
	}
}
