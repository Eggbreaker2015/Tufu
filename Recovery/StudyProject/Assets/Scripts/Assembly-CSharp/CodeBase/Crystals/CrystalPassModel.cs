using System;
using Codebase.Core;
using Codebase.Services.Time;
using UniMob;
using UniMob.Core;

namespace Codebase.Crystals
{
	public class CrystalPassModel : Model
	{
		[Inject]
		private CrystalConfig config;

		[Inject]
		private ITimeService timeService;

		private readonly CrystalUd data;

		private ComputedAtom<bool> __IsActive;

		private ComputedAtom<long> __ExpiryTimestamp;

		private ComputedAtom<TimeSpan> __TimeRemaining;

		public bool IsActive => false;

		public long ExpiryTimestamp => 0L;

		public TimeSpan TimeRemaining => default;

		public CrystalPassModel(Lifetime lifetime)
			: base(default)
		{
		}

		public void Activate()
		{
		}
	}
}
