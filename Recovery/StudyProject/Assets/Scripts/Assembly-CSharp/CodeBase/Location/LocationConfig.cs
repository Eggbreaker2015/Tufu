using System;
using Codebase.Enemy;
using UnityEngine;

namespace Codebase.Location
{
	[Serializable]
	public class LocationConfig
	{
		public string Name;

		public int Number;

		public WeaponType PreferredWeaponType;

		public WeaponType PreferredArmorType;

		public AudioClip NewMusic;
	}
}
