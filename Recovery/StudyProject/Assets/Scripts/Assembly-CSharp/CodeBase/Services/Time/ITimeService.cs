using System;

namespace Codebase.Services.Time
{
	public interface ITimeService
	{
		GameTime Now { get; }

		TimeSpan CheatOffset { get; }

		bool InPast(GameTime time);

		bool InFuture(GameTime time);

		void UpdateCheatOffset(float seconds);
	}
}
