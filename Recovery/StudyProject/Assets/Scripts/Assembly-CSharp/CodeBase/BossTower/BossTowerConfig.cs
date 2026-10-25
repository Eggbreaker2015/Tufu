using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;

namespace Codebase.BossTower
{
	[CreateAssetMenu(menuName = "Devour/BossTowerConfig")]
	public class BossTowerConfig : ScriptableObject
	{
		public int BaseTickets;
		public float ArmorBossOptimumTimeOffsetMinutes = 300f;
		public List<BossDef> Bosses;
		public int DailyRaidsMaxCount;
		public AudioClip ArenaMusic;
		public AudioClip FightMusic;
		[FormerlySerializedAs("bossItemDrops")]
		public List<BossDropItemData> bossItemWeaponDrops;
		public List<BossDropItemData> bossItemArmorDrops;
		public ParticleSystem BossSpawnVfxPrefab;
		public IReadOnlyList<BossDef> All
		{
			get
			{
				return Bosses;
			}
		}

		public BossDef Get(string key)
		{
			return Bosses.FirstOrDefault(boss => boss.Key == key);
		}

		public bool TryGet(string key, out BossDef boss)
		{
			boss = Get(key);
			return boss != null;
		}
	}
}
