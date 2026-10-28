using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Codebase.Core
{
	// Loads GameObjects by their original Addressables key.
	// Player builds use Addressables as the original did. In the editor the recovered bundles (built for
	// StandaloneWindows64) render broken on macOS/Metal: magenta UI, white-square particles. So the editor loads
	// the project's own copy of each asset, mapped in Recovery/Evidence/addressables-recovered-asset-map.json
	// (serialized trees match the originals), and falls back to the bundle only for unmapped keys.
	public static class RecoveredAddressables
	{
		public static GameObject Load(string key)
		{
#if UNITY_EDITOR
			GameObject local = LoadProjectCopy(key);
			if (local != null) return local;
#endif
			return Addressables.LoadAssetAsync<GameObject>(key).WaitForCompletion();
		}

		// Same as Load, but keeps the original asynchronous shape: the callback may run later (bundle) or at once (editor copy).
		public static void LoadAsync(string key, Action<GameObject> onLoaded)
		{
#if UNITY_EDITOR
			GameObject local = LoadProjectCopy(key);
			if (local != null)
			{
				onLoaded(local);
				return;
			}
#endif
			Addressables.LoadAssetAsync<GameObject>(key).Completed += handle => onLoaded(handle.Result);
		}

#if UNITY_EDITOR
		private const string MapPath = "../../Evidence/addressables-recovered-asset-map.json";

		private static Dictionary<string, string> projectPaths;

		[Serializable]
		private class MapFile
		{
			public List<MapEntry> mapping;
		}

		[Serializable]
		private class MapEntry
		{
			public string exportedPath;

			public string[] keys;
		}

		public static bool HasProjectCopy(string key)
		{
			return GetProjectPaths().ContainsKey(key);
		}

		public static GameObject LoadProjectCopy(string key)
		{
			if (key == null || !GetProjectPaths().TryGetValue(key, out string path)) return null;
			GameObject asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (asset == null) Debug.LogWarning("RecoveredAddressables: mapped asset not found at " + path + " for key " + key);
			return asset;
		}

		private static Dictionary<string, string> GetProjectPaths()
		{
			if (projectPaths != null) return projectPaths;
			projectPaths = new Dictionary<string, string>();
			string file = Path.GetFullPath(Path.Combine(Application.dataPath, MapPath));
			if (!File.Exists(file))
			{
				Debug.LogWarning("RecoveredAddressables: no asset map at " + file + "; loading from the recovered bundles");
				return projectPaths;
			}
			MapFile map = JsonUtility.FromJson<MapFile>(File.ReadAllText(file));
			if (map?.mapping == null) return projectPaths;
			foreach (MapEntry entry in map.mapping)
			{
				if (string.IsNullOrEmpty(entry.exportedPath) || entry.keys == null) continue;
				foreach (string key in entry.keys)
					projectPaths[key] = entry.exportedPath;
			}
			return projectPaths;
		}

#endif
	}
}
