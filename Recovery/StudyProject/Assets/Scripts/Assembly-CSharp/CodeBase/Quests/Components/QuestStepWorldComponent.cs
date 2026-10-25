using System;
using Codebase.Utilities;
using Scellecs.Morpeh;
using UnityEngine;
using UnityEngine.Playables;

namespace Codebase.Quests.Components
{
	[Serializable]
	public struct QuestStepWorldComponent : IComponent
	{
		[KeysDropdown(typeof(QuestKeys))]
		public string StepKey;

		public PlayableDirector Cutscene;

		public GameObject CamerasRoot;

		public GameObject[] ObjectsToEnable;

		public GameObject[] ObjectsToDisable;

		public GameObject[] ObjectsToHideDuringCutscene;

		public bool HasScriptedSequence;

		public Transform DialogueCameraTarget;

		public GameObject[] ScriptedCharacters;

		public Renderer[] StoneTransitionRenderers;

		public Animator StoneTransitionAnimator;

		public ParticleSystem[] ScriptedParticles;

		public QuestCutsceneSound[] CutsceneSounds;
	}
}
