using System;
using System.IO;
using System.Linq;
using Codebase.Core;
using Codebase.Economy;
using Codebase.Enemy;
using Codebase.Rpg;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RpgGrowthTools
{
	[MenuItem("Tools/RPG/Add Level and 1000 Gold")]
	public static void AddLevelAndGold()
	{
		if (!Application.isPlaying || !RpgModeConfig.Enabled || !GameContext.Has<RpgProgressModel>()) return;
		var progress = GameContext.Get<RpgProgressModel>();
		progress.AddXp(Math.Max(0, progress.XpToNext - progress.Xp));
		GameContext.Get<CurrencyModel>().Add(CurrencyKeys.RpgGold, 1000);
	}

	[MenuItem("Tools/RPG/Recruit Island 1-2 Heroes")]
	public static void RecruitHeroes()
	{
		if (!Application.isPlaying || !RpgModeConfig.Enabled || !GameContext.Has<PlayerModel>() || !GameContext.Has<ItemsProvider>()) return;
		PlayerModel player = GameContext.Get<PlayerModel>();
		ItemsProvider provider = GameContext.Get<ItemsProvider>();
		foreach (string key in new[] { "pick", "mailbox", "fork", "knife", "minisaw", "pike" })
			if (!player.Items.Any(item => item.Config.Key == key)) player.CreateItem(provider.Get(key), 1);
	}

	[MenuItem("Tools/RPG/Add 10 Hero Shards")]
	public static void AddHeroShards()
	{
		if (!Application.isPlaying || !RpgModeConfig.Enabled || !GameContext.Has<PlayerModel>()) return;
		foreach (ItemModel item in GameContext.Get<PlayerModel>().Items)
			if (item.Config.SlotType == ItemSlotType.Weapon && Codebase.Pet.PetEconomy.FindSpecies(item.Config) != null) item.Copies += 10;
	}

	[MenuItem("Tools/RPG/Dump Spawner Levels")]
	public static void DumpSpawnerLevels()
	{
		const string scenePath = "Assets/Scenes/Main.unity";
		if (!UnityEngine.SceneManagement.SceneManager.GetActiveScene().path.Equals(scenePath, StringComparison.Ordinal))
			EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
		var lines = UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None)
			.Where(spawner => spawner.Config.LocationNumber == 1 || spawner.Config.LocationNumber == 2)
			.OrderBy(spawner => spawner.Config.LocationNumber).ThenBy(spawner => spawner.Config.Key)
			.Select(spawner => Format(spawner.Config)).ToArray();
		string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Docs/RpgMode/SpawnerLevels.txt"));
		File.WriteAllLines(path, lines);
		foreach (string line in lines) Debug.Log(line);
		Debug.Log("RPG spawner levels written to " + path);
	}
	private static string Format(EnemySpawnerConfig config)
	{
		int level = RpgEnemyLevel.Compute(config);
		return string.Format(System.Globalization.CultureInfo.InvariantCulture,
			"{0} | island={1} | kind={2} | rel={3:0.###} | level={4} | cooldown={5:0.##} | xp={6:0.##} | gold={7} | prefab={8}",
			config.Key, config.LocationNumber, RpgSpawnerRules.Kind(config), config.RelativeOptimumTime, level,
			RpgSpawnerRules.GetCooldownSeconds(config), RpgNumbers.Xp(level) * RpgSpawnerRules.XpMultiplier(config),
			RpgNumbers.GrantedGold(level, config), config.Prefab != null ? config.Prefab.name : "(none)");
	}
}
