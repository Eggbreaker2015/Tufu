using System;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.BossTower.Components
{
	[Serializable]
	public struct LavaCrabHitBlinkComponent : IComponent
	{
		public LavaCrabHitBlinkView View;

		public AudioSource fireSoundSource;

		public ParticleSystem explosionPrepareParticle;

		public ParticleSystem explosionPrepareParticleInner;
	}
}
