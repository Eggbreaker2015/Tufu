using System;
using Codebase.Core;
using Codebase.Services.Time;
using Scellecs.Morpeh;

namespace Codebase.Shop.Systems
{
	public class ShopRepositorySystem : SystemBase
	{
		[Inject]
		private ShopLotRepository shopLotRepository;

		[Inject]
		private ITimeService timeService;

		private float timer;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
			timer -= deltaTime;
			if (timer > 0f) return;
			timer = 1f;
			EnsureNextFreeClaimReset(timeService.Now);
		}

		private GameTime EnsureNextFreeClaimReset(GameTime now)
		{
			GameTime? stored = shopLotRepository.TryReadStoredReset();
			if (!stored.HasValue)
			{
				GameTime initial = CalculateNextReset(now);
				StoreReset(initial);
				return initial;
			}
			if (timeService.InFuture(stored.Value)) return stored.Value;
			GameTime next = CalculateNextReset(now);
			StoreReset(next);
			shopLotRepository.ResetFreeClaims();
			return next;
		}

		private void StoreReset(GameTime time)
		{
			shopLotRepository.SetNextFreeClaimReset(time);
		}

		private GameTime CalculateNextReset(GameTime now)
		{
			GameTime date = now.Date;
			return now < date ? date : date.Add(TimeSpan.FromDays(1.0));
		}
	}
}
