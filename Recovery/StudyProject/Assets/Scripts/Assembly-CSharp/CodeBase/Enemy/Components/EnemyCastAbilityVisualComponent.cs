using System;
using BrunoMikoski.AnimationSequencer;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.Enemy.Components
{
	[Serializable]
	public struct EnemyCastAbilityVisualComponent : IComponent
	{
		public GameObject VisualObject;

		public AnimationSequencerController Sequencer;
	}
}
