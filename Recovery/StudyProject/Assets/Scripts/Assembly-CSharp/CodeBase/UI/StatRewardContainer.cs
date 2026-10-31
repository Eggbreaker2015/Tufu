using TMPro;
using System.Collections.Generic;
using Codebase.Core;
using Codebase.Simulation.Systems;
using Codebase.Utilities.ObjectPool;
using UnityEngine;

namespace Codebase.UI
{
	public class StatRewardContainer : MonoBehaviour
	{
		[SerializeField]
		private StatRewardView prefab;

		[SerializeField]
		private float verticalSpacing = 60f;

		[SerializeField]
		private int maxActiveRewards = 6;

		[Inject]
		private SimulationSystem simulationSystem;

		private readonly List<StatRewardView> activeRewards = new List<StatRewardView>();

		private readonly Queue<(string value, bool isVip)> pending = new Queue<(string value, bool isVip)>();

		private readonly ObjectPool<StatRewardView> pool = new ObjectPool<StatRewardView>();

		private void Awake()
		{
            global::Codebase.Core.GameContext.Set<StatRewardContainer>(this);
        }

		public void Create(string value, bool isVip = false)
		{
			// Native ELF VA 0x15fd198; native inline SpawnReward body checked in bounded ARM64.
			if (simulationSystem.UiDisabled) return;
			if (activeRewards.Count < maxActiveRewards) SpawnReward(value, isVip);
			else pending.Enqueue((value, isVip));
		}

		private void Reposition()
		{
			// Native ELF VA 0x15fd5a0.
			for (int i = 0; i < activeRewards.Count; i++) activeRewards[i].TargetPosition = new Vector2(0f, verticalSpacing * i);
		}

		private void SpawnReward(string value, bool isVip)
		{
			// Native ELF VA 0x15fd29c. Four gradient colors verified from FP registers and ARM64 stack args.
			StatRewardView view = pool.Spawn(prefab, Vector3.zero, Quaternion.identity, transform);
			view.gameObject.SetActive(true);
			view.Text.text = value;
			Color top = new Color(1f, 0.96f, 0f, 1f);
			Color bottom = new Color(1f, 0.55f, 0f, 1f);
			view.Text.colorGradient = new VertexGradient(top, top, bottom, bottom);
			view.Text.enableVertexGradient = isVip;
			view.OnAnimationEnd = () =>
			{
			    activeRewards.Remove(view);
			    Reposition();
			    pool.Despawn(view);
			    TrySpawnFromQueue();
			};
			if ((value.EndsWith("经验") || value.EndsWith("金币") || value.EndsWith("碎片") || value.EndsWith("嘲讽")))
			{
				TMP_FontAsset originalFont = view.Text.font;
				Material originalMaterial = view.Text.fontSharedMaterial;
				PetStatsText.ApplyCjkFont(view.Text);
				var onAnimationEnd = view.OnAnimationEnd;
				view.OnAnimationEnd = () =>
				{
					view.Text.font = originalFont;
					view.Text.fontSharedMaterial = originalMaterial;
					onAnimationEnd();
				};
			}
			activeRewards.Add(view);
			Reposition();
		}

		private void TrySpawnFromQueue()
		{
			// Native ELF VA 0x15fd650.
			while (activeRewards.Count < maxActiveRewards && pending.Count > 0)
			{
			    var next = pending.Dequeue();
			    SpawnReward(next.value, next.isVip);
			}
		}
	}
}
