using Codebase.Core;
using Scellecs.Morpeh;

namespace Codebase.Analytics.Systems
{
	public class AppPauseTimeSkipSystem : SystemBase
	{
		[Inject]
		private UserModel userModel;
		private const float PAUSE_THRESHOLD = 3f;
		private float lastFrameTime;
		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{ // Original ELF0x15ac120; deltaTime unused. Native inlines the same SkipTimers predicate.
			CalcTimeSkip();
			SkipTimers();
		}

		private void CalcTimeSkip()
		{ // Original ELF0x15ac150; single FSUB, strict ordered gates.
			float now = global::UnityEngine.Time.realtimeSinceStartup;
			float skippedSeconds = global::System.BitConverter.Int32BitsToSingle(global::System.BitConverter.SingleToInt32Bits(now - lastFrameTime));
			if (lastFrameTime > 0f && skippedSeconds > PAUSE_THRESHOLD)
				userModel.SkipSeconds = skippedSeconds;
			lastFrameTime = now;
		}

		private void SkipTimers()
		{ // Original ELF0x15ac194 FCMP+B.LS: preserve <=0, clear positive and unordered/NaN. No other timer calls.
			if (!(userModel.SkipSeconds <= 0f))
				userModel.SkipSeconds = 0f;
		}
	}
}
