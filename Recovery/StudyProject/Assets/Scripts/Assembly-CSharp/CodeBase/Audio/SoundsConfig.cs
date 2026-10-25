using System;
using System.Collections.Generic;
using UnityEngine;

namespace CodeBase.Audio
{
	[CreateAssetMenu(fileName = "Sounds Config", menuName = "Configs/Sounds Config")]
	public class SoundsConfig : ScriptableObject
	{
		[Serializable]
		public class SoundEntry
		{
			public string Key;

			public AudioClip[] Clips;

			[Range(0f, 3f)]
			public float Volume = 1f;
		}

		[SerializeField]
		private List<SoundEntry> entries = new List<SoundEntry>();

		public bool TryGetValue(string key, out SoundEntry data)
		{
			for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Key == key) { data = entries[i]; return true; }
            }
            data = null;
            return false;
		}
	}
}
