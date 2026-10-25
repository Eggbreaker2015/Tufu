using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Quests
{
	[CreateAssetMenu(menuName = "Devour/QuestConfig")]
	public class QuestConfig : ScriptableObject
	{
		public List<QuestGiverDef> Givers = new List<QuestGiverDef>();
		public List<QuestChainDef> Chains = new List<QuestChainDef>();
		[NonSerialized]
		private readonly Dictionary<string, QuestGiverDef> giverByKey = new Dictionary<string, QuestGiverDef>();
		[NonSerialized]
		private readonly Dictionary<string, QuestGiverDef> giverBySideChainKey = new Dictionary<string, QuestGiverDef>();
		[NonSerialized]
		private readonly Dictionary<string, QuestGiverDef> giverByHubChainKey = new Dictionary<string, QuestGiverDef>();
		[NonSerialized]
		private readonly Dictionary<string, QuestChainDef> chainByKey = new Dictionary<string, QuestChainDef>();
		[NonSerialized]
		private readonly Dictionary<string, QuestChainDef> chainByStepKey = new Dictionary<string, QuestChainDef>();
		[NonSerialized]
		private readonly Dictionary<string, QuestStepDef> stepByKey = new Dictionary<string, QuestStepDef>();
		[NonSerialized]
		private readonly Dictionary<string, QuestChainDef> hubChainByTokenKey = new Dictionary<string, QuestChainDef>();
		[NonSerialized]
		private readonly Dictionary<string, QuestChainDef> hubChainBySourceChainKey = new Dictionary<string, QuestChainDef>();
		[NonSerialized]
		private readonly Dictionary<QuestChainDef, QuestChainDef> previousChainByChain = new Dictionary<QuestChainDef, QuestChainDef>();
		[NonSerialized]
		private readonly Dictionary<string, QuestStepDef> nextStepByKey = new Dictionary<string, QuestStepDef>();
		[NonSerialized]
		private QuestGiverDef giverWithNullKey;
		[NonSerialized]
		private QuestGiverDef giverWithNullSideChainKey;
		[NonSerialized]
		private QuestGiverDef giverWithNullHubChainKey;
		[NonSerialized]
		private QuestChainDef chainWithNullKey;
		[NonSerialized]
		private QuestChainDef chainWithNullStepKey;
		[NonSerialized]
		private QuestStepDef stepWithNullKey;
		[NonSerialized]
		private QuestChainDef hubChainWithNullTokenKey;
		[NonSerialized]
		private QuestStepDef nextStepForNullKey;
		[NonSerialized]
		private bool hasNextStepForNullKey;
		private void OnEnable()
		{
			RebuildIndexes();
		}

		private void OnValidate()
		{
			RebuildIndexes();
		}

		public QuestGiverDef GetGiver(string giverKey)
		{
			return giverKey == null ? giverWithNullKey : giverByKey.GetValueOrDefault(giverKey);
		}

		public QuestGiverDef GetGiverBySideChain(string sideChainKey)
		{ // Original ELF 0x1422454; independent candidate only.
			return sideChainKey == null ? giverWithNullSideChainKey : giverBySideChainKey.GetValueOrDefault(sideChainKey);
		}

		public QuestGiverDef GetGiverByHubChain(string hubChainKey)
		{
			return hubChainKey == null ? giverWithNullHubChainKey : giverByHubChainKey.GetValueOrDefault(hubChainKey);
		}

		public QuestGiverDef GetGiverOfChain(QuestChainDef chain)
		{
			return chain == null ? null : GetGiver(chain.QuestGiverKey);
		}

		public QuestGiverDef GetGiverOfStep(string stepKey)
		{ // Original ELF 0x14224d8; independent candidate only.
			return GetGiverOfChain(GetChainOfStep(stepKey));
		}

		public QuestStepDef GetStep(string stepKey)
		{ // Original ELF 0x1422500; independent candidate only.
			return stepKey == null ? stepWithNullKey : stepByKey.GetValueOrDefault(stepKey);
		}

		public QuestChainDef GetChainOfStep(string stepKey)
		{
			return stepKey == null ? chainWithNullStepKey : chainByStepKey.GetValueOrDefault(stepKey);
		}

		public QuestChainDef GetChain(string chainKey)
		{
			return chainKey == null ? chainWithNullKey : chainByKey.GetValueOrDefault(chainKey);
		}

		public QuestChainDef GetHubChainForToken(string tokenKey)
		{ // Original ELF 0x1422570; independent candidate only.
			return tokenKey == null ? hubChainWithNullTokenKey : hubChainByTokenKey.GetValueOrDefault(tokenKey);
		}

		public QuestChainDef GetPreviousChain(QuestChainDef chain)
		{
			return chain == null ? null : previousChainByChain.GetValueOrDefault(chain);
		}

		public QuestChainDef GetUnlockSourceChain(QuestChainDef chain)
		{
			// Original ELF 0x14225e0.
			return chain == null || string.IsNullOrEmpty(chain.UnlocksAfterChainKey) ? null : GetChain(chain.UnlocksAfterChainKey);
		}

		public QuestChainDef GetHubChainForTokenSource(string sourceChainKey)
		{
			// Original ELF 0x1422628.
			return string.IsNullOrEmpty(sourceChainKey) ? null : hubChainBySourceChainKey.GetValueOrDefault(sourceChainKey);
		}

		public QuestStepDef GetNextStep(string stepKey)
		{ // Original ELF 0x14226a4; independent candidate only.
			return stepKey == null ? nextStepForNullKey : nextStepByKey.GetValueOrDefault(stepKey);
		}

		private void RebuildIndexes()
		{
			// Original ELF0x1421c10: first duplicate wins, including dedicated null-key slots.
			giverByKey.Clear();
			giverBySideChainKey.Clear();
			giverByHubChainKey.Clear();
			chainByKey.Clear();
			chainByStepKey.Clear();
			stepByKey.Clear();
			hubChainByTokenKey.Clear();
			hubChainBySourceChainKey.Clear();
			previousChainByChain.Clear();
			nextStepByKey.Clear();
			giverWithNullKey = null;
			giverWithNullSideChainKey = null;
			giverWithNullHubChainKey = null;
			chainWithNullKey = null;
			chainWithNullStepKey = null;
			stepWithNullKey = null;
			hubChainWithNullTokenKey = null;
			nextStepForNullKey = null;
			hasNextStepForNullKey = false;
			foreach (var giver in Givers)
			{
				AddFirst(giverByKey, giver.GiverKey, giver, ref giverWithNullKey);
				AddFirst(giverBySideChainKey, giver.SideChainKey, giver, ref giverWithNullSideChainKey);
				AddFirst(giverByHubChainKey, giver.HubChainKey, giver, ref giverWithNullHubChainKey);
			}

			for (int ci = 0; ci < Chains.Count; ci++)
			{
				var chain = Chains[ci];
				AddFirst(chainByKey, chain.ChainKey, chain, ref chainWithNullKey);
				previousChainByChain.TryAdd(chain, ci > 0 ? Chains[ci - 1] : null);
				for (int si = 0; si < chain.Steps.Count; si++)
				{
					var step = chain.Steps[si];
					AddFirst(chainByStepKey, step.StepKey, chain, ref chainWithNullStepKey);
					AddFirst(stepByKey, step.StepKey, step, ref stepWithNullKey);
					var next = si < chain.Steps.Count - 1 ? chain.Steps[si + 1] : null;
					if (step.StepKey == null)
					{
						if (!hasNextStepForNullKey)
						{
							nextStepForNullKey = next;
							hasNextStepForNullKey = true;
						}
					}
					else
						nextStepByKey.TryAdd(step.StepKey, next);
				}

				if (chain.Role == QuestChainRole.Hub)
					foreach (var slot in chain.HubTokenSlots)
					{
						AddFirst(hubChainByTokenKey, slot.TokenKey, chain, ref hubChainWithNullTokenKey);
						if (!string.IsNullOrEmpty(slot.SourceChainKey))
							hubChainBySourceChainKey.TryAdd(slot.SourceChainKey, chain);
					}
			}
		}

		private static void AddFirst<T>(Dictionary<string, T> index, string key, T value, ref T nullKeyValue)
			where T : class
		{
			if (key == null)
			{
				if (nullKeyValue == null)
					nullKeyValue = value;
			}
			else
				index.TryAdd(key, value);
		}
	}
}
