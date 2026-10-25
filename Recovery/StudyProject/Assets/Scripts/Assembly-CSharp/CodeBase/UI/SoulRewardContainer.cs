using System.Collections.Generic;
using BrunoMikoski.AnimationSequencer;
using Codebase.Amulet;
using Codebase.Core;
using Codebase.Simulation.Systems;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI
{
	public class SoulRewardContainer : MonoBehaviour
	{
		private struct PendingReward
		{
			public string SoulKey;

			public double Amount;

			public bool Doubled;
		}

		[SerializeField]
		private Image icon;

		[SerializeField]
		private TMP_Text countText;

		[SerializeField]
		private TMP_Text multiplierText;

		[SerializeField]
		private AnimationSequencerController[] animations;

		[SerializeField]
		private AnimationSequencerController doubledAnimation;

		[Inject]
		private SimulationSystem simulationSystem;

		[Inject]
		private SoulStyleConfig soulStyleConfig;

		private static readonly Color DoubledColor = new Color(1f, 0.902f, 0.259f, 1f);

		private readonly List<PendingReward> pending = new List<PendingReward>();

		private AnimationSequencerController currentAnimation;

		private bool isPlaying;

		private bool initialized;

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set(this);
            icon.enabled = false;
            countText.enabled = false;
            multiplierText.enabled = false;
        }

		public void Create(string soulKey, double amount, bool doubled = false)
		{
			// Native ELF VA 0x15fae8c.
			if (simulationSystem.UiDisabled) return;
			pending.Add(new PendingReward { SoulKey = soulKey, Amount = amount, Doubled = doubled });
			if (!isPlaying) PlayNext();
		}

		public void OnAnimationFinished()
		{
			// Native ELF VA 0x15fb2a8.
			currentAnimation.ClearPlayingSequence();
			currentAnimation.ResetToInitialState();
			isPlaying = false;
			if (pending.Count > 0) PlayNext();
			else
			{
			    icon.enabled = false;
			    countText.enabled = false;
			    multiplierText.enabled = false;
			    initialized = false;
			}
		}

		private void PlayNext()
		{
			// Native ELF VA 0x15faf64: aggregate every pending entry with the first key, traversing backward.
			if (pending.Count == 0) return;
			string key = pending[0].SoulKey;
			double amount = 0.0;
			bool doubled = false;
			for (int i = pending.Count - 1; i >= 0; i--)
			{
			    if (pending[i].SoulKey != key) continue;
			    amount += pending[i].Amount;
			    doubled |= pending[i].Doubled;
			    pending.RemoveAt(i);
			}
			if (!initialized)
			{
			    initialized = true;
			    icon.enabled = true;
			    countText.enabled = true;
			}
			icon.sprite = soulStyleConfig.GetIcon(key);
			countText.text = string.Format("{0:0.#}", amount);
			countText.color = doubled ? DoubledColor : Color.white;
			multiplierText.enabled = doubled;
			multiplierText.text = "x2";
			currentAnimation = doubled ? doubledAnimation : animations[GetAnimationIndex(key)];
			currentAnimation.Play();
			isPlaying = true;
		}

		private static int GetAnimationIndex(string soulKey)
		{
			// Native ELF VA 0x15fb370; exact keys resolved by original ELF relocations.
			if (soulKey == "common") return 0;
			if (soulKey == "large") return 1;
			if (soulKey == "elite") return 2;
			return 3;
		}
	}
}
