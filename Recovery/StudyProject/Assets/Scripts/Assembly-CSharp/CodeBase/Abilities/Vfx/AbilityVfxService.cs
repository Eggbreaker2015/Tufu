using Codebase.Utilities.ObjectPool;
using Codebase.Abilities.Vfx.Components;
using Scellecs.Morpeh;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using Codebase.Unit;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Codebase.Abilities.Vfx
{
	public class AbilityVfxService
	{
		private readonly Dictionary<string, ParticleSystem> loadedPrefabs = new Dictionary<string, ParticleSystem>();

		private readonly Dictionary<string, AsyncOperationHandle<GameObject>> handles = new Dictionary<string, AsyncOperationHandle<GameObject>>();

		public void Spawn(AbilityVfxData data, UnitCore unit)
		{
			// Native ELF VA 0x15e42f8.
			if (unit == null) return;
			Spawn(data, unit.VisualRoot != null ? unit.VisualRoot : unit.transform);
		}

		public ParticleSystem SpawnPersistent(AbilityVfxData data, UnitCore unit)
		{
			// Native ELF VA 0x15e4410.
			return SpawnPersistent(data, unit, false);
		}

		public ParticleSystem SpawnPersistent(AbilityVfxData data, UnitCore unit, bool parent)
		{
			// Native ELF VA 0x15e4438.
			if (unit == null) return null;
			return SpawnPersistent(data, unit.VisualRoot != null ? unit.VisualRoot : unit.transform, parent);
		}

		public ParticleSystem SpawnPersistent(AbilityVfxData data, Transform target)
		{
			// Native ELF VA 0x15e4814.
			return SpawnPersistent(data, target, false);
		}

		public ParticleSystem SpawnPersistent(AbilityVfxData data, Transform target, bool parent)
		{
			// Native ELF VA 0x15e4528.
			if (string.IsNullOrEmpty(data.Key) || target == null) return null;
			ParticleSystem prefab = GetOrLoadPrefab(data.Key);
			if (prefab == null) return null;
			ParticleSystem instance = ObjectPoolUtility<ParticleSystem>.Spawn(prefab, target.position + Vector3.up * data.HeightOffset);
			if (parent)
			{
			    instance.transform.SetParent(target, true);
			    instance.transform.localPosition = Vector3.up * data.HeightOffset;
			}
			instance.transform.rotation = Quaternion.Euler(data.RotationX, data.RotationY, data.RotationZ);
			instance.transform.localScale = prefab.transform.localScale;
			instance.Clear(true);
			instance.Play(true);
			return instance;
		}

		public void DespawnPersistent(ParticleSystem instance)
		{
			// Native ELF VA 0x15e4a44; bounded ARM64 only, decompiler had unrelated tail spill.
			if (instance == null) return;
			instance.transform.SetParent(null, true);
			ObjectPoolUtility<ParticleSystem>.Despawn(instance);
		}

		public void Spawn(AbilityVfxData data, Transform target)
		{
			// Native ELF VA 0x15e43e8.
			Spawn(data, target, data.RotationY);
		}

		public void Spawn(AbilityVfxData data, Transform target, float rotationY)
		{
			// Native ELF VA 0x15e4b1c.
			Spawn(data, target, rotationY, Vector3.zero);
		}

		public void Spawn(AbilityVfxData data, Transform target, float rotationY, Vector3 positionOffset)
		{
			// Native ELF VA 0x15e4ba0.
			if (string.IsNullOrEmpty(data.Key) || target == null) return;
			ParticleSystem prefab = GetOrLoadPrefab(data.Key);
			if (prefab == null) return;
			ParticleSystem instance = ObjectPoolUtility<ParticleSystem>.Spawn(prefab, target.position + Vector3.up * data.HeightOffset + positionOffset);
			instance.transform.rotation = Quaternion.Euler(data.RotationX, rotationY, data.RotationZ);
			instance.transform.localScale = prefab.transform.localScale;
			instance.Clear(true);
			instance.Play(true);
			World world = World.Default;
			world.GetStash<AbilityVfxComponent>().Set(world.CreateEntity(), new AbilityVfxComponent { ParticleSystem = instance, Target = target, Duration = data.Duration, HeightOffset = data.HeightOffset, PositionOffset = positionOffset });
		}

		public ParticleSystem SpawnAt(AbilityVfxData data, Vector3 position, float scaleMultiplier)
		{
			// Native ELF VA 0x15e4f54.
			return SpawnAt(data, position, Quaternion.Euler(data.RotationX, data.RotationY, data.RotationZ), scaleMultiplier);
		}

		public ParticleSystem SpawnAt(AbilityVfxData data, Vector3 position, Quaternion rotation, float scaleMultiplier = 1f)
		{
			// Native ELF VA 0x15e5000; native fixed-position path applies no HeightOffset.
			if (string.IsNullOrEmpty(data.Key)) return null;
			ParticleSystem prefab = GetOrLoadPrefab(data.Key);
			if (prefab == null) return null;
			ParticleSystem instance = ObjectPoolUtility<ParticleSystem>.Spawn(prefab, position);
			instance.transform.rotation = rotation;
			instance.transform.localScale = prefab.transform.localScale * scaleMultiplier;
			instance.Clear(true);
			instance.Play(true);
			World world = World.Default;
			world.GetStash<AbilityVfxComponent>().Set(world.CreateEntity(), new AbilityVfxComponent { ParticleSystem = instance, Target = null, Duration = data.Duration });
			return instance;
		}

		private ParticleSystem GetOrLoadPrefab(string key)
		{
			// Native ELF VA 0x15e483c; cache owns successful original Addressables handles.
			if (loadedPrefabs.TryGetValue(key, out ParticleSystem prefab)) return prefab;
#if UNITY_EDITOR
			prefab = LoadProjectPrefab(key);
			if (prefab != null)
			{
				loadedPrefabs[key] = prefab;
				return prefab;
			}
#endif
			AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(key);
			GameObject obj = handle.WaitForCompletion();
			if (obj == null) return null;
			prefab = obj.GetComponent<ParticleSystem>();
			if (prefab == null) return null;
			handles[key] = handle;
			loadedPrefabs[key] = prefab;
			return prefab;
		}

#if UNITY_EDITOR
		// Editor only: the recovered bundles are built for StandaloneWindows64 and their particle materials and
		// textures render as white squares in the macOS (Metal) editor. Load the project's own copy of each prefab
		// instead (Recovery/Evidence/addressables-recovered-asset-map.json; serialized trees match the originals).
		private const string ProjectFxRoot = "Assets/Models/AssetsV2/ButcherMiningCave/FX/Prefabs/";

		private static readonly Dictionary<string, string> projectPrefabs = new Dictionary<string, string>
		{
			{ "Stunned.prefab", "Assets/JMO Assets/Epic Toon FX/Prefabs/Combat/Brawling/Stun/StunnedCirclingStars.prefab" },
			{ "DeathEvasion.prefab", ProjectFxRoot + "DeathEvasion.prefab" },
			{ "HealBig.prefab", ProjectFxRoot + "HealTrigger1.prefab" },
			{ "HealSmall.prefab", ProjectFxRoot + "HealTriggerSmall.prefab" },
			{ "ApplyMark.prefab", ProjectFxRoot + "Mark.prefab" },
			{ "MegaLightning.prefab", ProjectFxRoot + "MegaLightning.prefab" },
			{ "RageLevelUp1.prefab", ProjectFxRoot + "Rage_Stage_1.prefab" },
			{ "RageLevelUp2.prefab", ProjectFxRoot + "Rage_Stage_2.prefab" },
			{ "RageLevelUp3.prefab", ProjectFxRoot + "Rage_Stage_3.prefab" },
			{ "ReflectionStrike.prefab", ProjectFxRoot + "ReflectionStrike.prefab" },
			{ "ShockLightning.prefab", ProjectFxRoot + "ShockLightning.prefab" },
			{ "ApplyShock.prefab", ProjectFxRoot + "Shocked.prefab" },
			{ "SpeedupTrigger.prefab", ProjectFxRoot + "SpeedupTrigger.prefab" },
			{ "SuperCrit_Blue.prefab", ProjectFxRoot + "SuperCrit_Blue.prefab" },
			{ "SwordCrit.prefab", ProjectFxRoot + "SwordCrit_Slime.prefab" },
			{ "SwordHit.prefab", ProjectFxRoot + "SwordHit_Slime.prefab" },
		};

		private static ParticleSystem LoadProjectPrefab(string key)
		{
			if (!projectPrefabs.TryGetValue(System.IO.Path.GetFileName(key), out string path)) return null;
			GameObject obj = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
			return obj != null ? obj.GetComponent<ParticleSystem>() : null;
		}
#endif
	}
}
