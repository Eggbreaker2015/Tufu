using Cysharp.Threading.Tasks;
using UnityEngine.Playables;
using System;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Analytics;

namespace Codebase.Quests
{
	public class QuestCutsceneService
	{
		private const double LastFrameTimeOffset = 0.0001;
		public UniTask Play(PlayableDirector director)
		{ // Original ELF 0x1422c8c; candidate only.
			var completion = new UniTaskCompletionSource();
			void OnStopped(UnityEngine.Playables.PlayableDirector ignored)
			{
				director.stopped -= OnStopped;
				completion.TrySetResult();
			}

			director.stopped += OnStopped;
			director.gameObject.SetActive(true);
			director.time = 0;
			director.Play();
			return completion.Task;
		}

		public UniTask PlayAndHoldLastFrame(PlayableDirector director)
		{ // Original ELF 0x1422dfc; candidate only.
			var completion = new UniTaskCompletionSource();
			void OnStopped(UnityEngine.Playables.PlayableDirector ignored)
			{
				director.stopped -= OnStopped;
				HoldLastFrame(director);
				completion.TrySetResult();
			}

			director.stopped += OnStopped;
			director.gameObject.SetActive(true);
			director.time = 0;
			director.Play();
			return completion.Task;
		}

		public static void HoldLastFrame(PlayableDirector director)
		{ // Original ELF 0x1422f6c; candidate only.
			double duration = director.duration;
			if (duration > 0.0)
				director.time = duration + (-.0001);
			director.Evaluate();
			director.Pause();
		}

		public static void Release(PlayableDirector director)
		{ // Original ELF 0x1422fc0; candidate only.
			director.Stop();
			director.time = 0;
		}
	}
}
