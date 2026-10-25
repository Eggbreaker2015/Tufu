using System;
using System.Collections.Generic;
using Codebase.In_apps;
using Codebase.Rewards;
using UnityEngine;

namespace Codebase.Events
{
	[Serializable]
	public class EventBoosterDef
	{
		public string Key;

		public EventBoosterType Type;

		public Sprite FirstIcon;

		public Sprite SecondIcon;

		public int BaseGemCost;

		public int GemCostIncrement;

		public int SpawnCount;

		public ProductKey ProductKey;

		public Sprite TokenSprite;

		[SerializeReference]
		public List<RewardBase> Rewards;

		public int MaxPurchases;

		public bool IsUnlimited;

		private bool IsGemBooster => false;

		private bool IsIAPBooster => false;
	}
}
