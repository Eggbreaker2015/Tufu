using System;
using BrunoMikoski.AnimationSequencer;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Events.Components
{
	[Serializable]
	public struct EventBossChestComponent : IComponent
	{
		public AnimationSequencerController Animation;

		public GameObject Root;

		public EventDifficulty Difficulty;
	}
}
