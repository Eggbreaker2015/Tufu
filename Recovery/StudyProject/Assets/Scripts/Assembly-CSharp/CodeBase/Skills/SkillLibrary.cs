using System;
using System.Collections.Generic;
using Codebase.Core;
using UnityEngine;

namespace Codebase.Skills
{
	[Serializable]
	public class SkillVfxEntry
	{
		public string Key;

		public ParticleSystem Prefab;

		public float Lifetime = 2f;

		public float Scale = 1f;

		// Runtime projectile (used for missiles when Prefab is empty): a camera-facing textured quad along the flight
		// direction plus a trail, built once by SkillProjectileView. Texture points to +X (right); Scale multiplies the size.
		public Texture2D ProjectileTexture;

		public float ProjectileLength = 1.2f;

		public float ProjectileWidth = 0.36f;

		public Color ProjectileTint = Color.white;

		public float TrailTime = 0.2f;

		public float TrailWidth = 0.2f;

		public Color TrailColor = new Color(0.8f, 0.95f, 1f, 0.6f);
	}

	// Hand-edited YAML assets under Resources/Skills; several files are merged (first key wins, duplicates warn).
	[CreateAssetMenu(fileName = "Skill Library", menuName = "Codebase/Skill Library")]
	public class SkillLibrary : ScriptableObject
	{
		private const string ResourcesPath = "Skills";

		public List<SkillDef> Skills = new List<SkillDef>();

		public List<BuffDef> Buffs = new List<BuffDef>();

		public List<UnitSkillLoadout> Loadouts = new List<UnitSkillLoadout>();

		public List<SkillVfxEntry> Vfx = new List<SkillVfxEntry>();

		// Ground telegraph texture (soft filled circle); the first library that sets one wins.
		public Texture2D TelegraphTexture;

		private static bool loaded;

		private static readonly Dictionary<string, SkillDef> SkillsByKey = new Dictionary<string, SkillDef>();

		private static readonly Dictionary<string, BuffDef> BuffsByKey = new Dictionary<string, BuffDef>();

		private static readonly Dictionary<string, SkillVfxEntry> VfxByKey = new Dictionary<string, SkillVfxEntry>();

		private static readonly Dictionary<string, UnitSkillLoadout>[] LoadoutsByKind = new Dictionary<string, UnitSkillLoadout>[4];

		private static readonly HashSet<string> Warned = new HashSet<string>();

		private static Texture2D telegraphTexture;

		public static bool HasLoadouts
		{
			get
			{
				EnsureLoaded();
				foreach (var map in LoadoutsByKind)
					if (map != null && map.Count > 0)
						return true;
				return false;
			}
		}

		public static Texture2D SharedTelegraphTexture
		{
			get
			{
				EnsureLoaded();
				return telegraphTexture;
			}
		}

		public static SkillDef FindSkill(string key)
		{
			EnsureLoaded();
			if (string.IsNullOrEmpty(key))
				return null;
			if (SkillsByKey.TryGetValue(key, out SkillDef def))
				return def;
			WarnOnce("skill:" + key, "[Skills] Unknown skill '" + key + "'");
			return null;
		}

		public static BuffDef FindBuff(string key)
		{
			EnsureLoaded();
			if (string.IsNullOrEmpty(key))
				return null;
			if (BuffsByKey.TryGetValue(key, out BuffDef def))
				return def;
			WarnOnce("buff:" + key, "[Skills] Unknown buff '" + key + "'");
			return null;
		}

		public static SkillVfxEntry FindVfx(string key)
		{
			EnsureLoaded();
			if (string.IsNullOrEmpty(key))
				return null;
			if (VfxByKey.TryGetValue(key, out SkillVfxEntry entry))
				return entry;
			WarnOnce("vfx:" + key, "[Skills] Unknown vfx '" + key + "'");
			return null;
		}

		public static UnitSkillLoadout FindLoadout(SkillOwnerKind kind, string key)
		{
			EnsureLoaded();
			if (string.IsNullOrEmpty(key))
				return null;
			var map = LoadoutsByKind[(int)kind];
			return map != null && map.TryGetValue(key, out UnitSkillLoadout loadout) ? loadout : null;
		}

		public static void WarnOnce(string id, string message)
		{
			if (Warned.Add(id))
				Debug.LogWarning(message);
		}

		private static void EnsureLoaded()
		{
			if (loaded)
				return;
			loaded = true;
			foreach (SkillLibrary library in Resources.LoadAll<SkillLibrary>(ResourcesPath))
				Merge(library);
			Validate();
		}

		private static void Merge(SkillLibrary library)
		{
			if (library == null)
				return;
			if (telegraphTexture == null)
				telegraphTexture = library.TelegraphTexture;
			if (library.Skills != null)
				foreach (SkillDef def in library.Skills)
					if (def != null && !string.IsNullOrEmpty(def.Key) && !TryAdd(SkillsByKey, def.Key, def))
						Debug.LogWarning("[Skills] Duplicate skill '" + def.Key + "' in " + library.name);
			if (library.Buffs != null)
				foreach (BuffDef def in library.Buffs)
					if (def != null && !string.IsNullOrEmpty(def.Key) && !TryAdd(BuffsByKey, def.Key, def))
						Debug.LogWarning("[Skills] Duplicate buff '" + def.Key + "' in " + library.name);
			if (library.Vfx != null)
				foreach (SkillVfxEntry entry in library.Vfx)
					if (entry != null && !string.IsNullOrEmpty(entry.Key) && !TryAdd(VfxByKey, entry.Key, entry))
						Debug.LogWarning("[Skills] Duplicate vfx '" + entry.Key + "' in " + library.name);
			if (library.Loadouts != null)
				foreach (UnitSkillLoadout loadout in library.Loadouts)
				{
					if (loadout == null || string.IsNullOrEmpty(loadout.Key))
						continue;
					int kind = (int)loadout.OwnerKind;
					if (kind < 0 || kind >= LoadoutsByKind.Length)
						continue;
					var map = LoadoutsByKind[kind] ?? (LoadoutsByKind[kind] = new Dictionary<string, UnitSkillLoadout>());
					if (!TryAdd(map, loadout.Key, loadout))
						Debug.LogWarning("[Skills] Duplicate loadout '" + loadout.OwnerKind + "/" + loadout.Key + "' in " + library.name);
				}
		}

		private static bool TryAdd<T>(Dictionary<string, T> map, string key, T value)
		{
			if (map.ContainsKey(key))
				return false;
			map[key] = value;
			return true;
		}

		// Loadout skill keys and buff keys referenced by effects; unknown keys warn once at load.
		private static void Validate()
		{
			foreach (var map in LoadoutsByKind)
			{
				if (map == null)
					continue;
				foreach (UnitSkillLoadout loadout in map.Values)
					if (loadout.Skills != null)
						foreach (SkillSlotDef slot in loadout.Skills)
							if (slot == null || !SkillsByKey.ContainsKey(slot.SkillKey ?? string.Empty))
								WarnOnce("skill:" + slot?.SkillKey, "[Skills] Loadout '" + loadout.Key + "' references unknown skill '" + slot?.SkillKey + "'");
			}
			foreach (SkillDef def in SkillsByKey.Values)
			{
				if (def.Effects == null)
					continue;
				foreach (ISkillEffect effect in def.Effects)
					if (effect == null)
						WarnOnce("null:" + def.Key, "[Skills] Skill '" + def.Key + "' has a missing effect (renamed class?)");
			}
		}

		[StaticReset]
		private static void ResetStaticState()
		{
			loaded = false;
			SkillsByKey.Clear();
			BuffsByKey.Clear();
			VfxByKey.Clear();
			for (int i = 0; i < LoadoutsByKind.Length; i++)
				LoadoutsByKind[i] = null;
			Warned.Clear();
			telegraphTexture = null;
		}
	}
}
