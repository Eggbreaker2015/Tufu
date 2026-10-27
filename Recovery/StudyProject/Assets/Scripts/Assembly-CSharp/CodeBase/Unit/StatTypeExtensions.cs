namespace Codebase.Unit
{
	public static class StatTypeExtensions
	{
		public static bool IsPercentStat(this StatType type)
		{
			// APK ARM64 0x15cafa8: unsigned range check, followed by the original bit mask.
			return (uint)type < 33 && ((0x1082aaa80UL >> (int)type) & 1UL) != 0;
		}

		public static bool IsSpecificPercentStat(this StatType type)
		{
			// APK 0x15cafc8.
			return (uint)type < 35 && ((0x633800000UL >> (int)type) & 1UL) != 0;
		}

		public static bool IsPercentDisplay(this StatType type)
		{
			// APK 0x15cafe4.
			return (uint)type < 35 && ((0x73baaaa80UL >> (int)type) & 1UL) != 0;
		}

		public static StatType GetIconStatType(this StatType type)
		{
			// APK 0x15cb004 and jump table 0x9226f0. Unlisted types retain their own icon.
			switch (type)
			{
				case StatType.StabDamagePercent: return StatType.StabDamageFlat;
				case StatType.CutDamagePercent: return StatType.CutDamageFlat;
				case StatType.BuckshotDamagePercent: return StatType.BuckshotDamageFlat;
				case StatType.StabDefencePercent: return StatType.StabDefenceFlat;
				case StatType.CutDefencePercent: return StatType.CutDefenceFlat;
				case StatType.BuckshotDefencePercent: return StatType.BuckshotDefenceFlat;
				default: return type;
			}
		}

		public static bool IsSpecificStat(this StatType type)
		{
			// APK 0x15cb054.
			return (uint)type < 35 && ((0x6f3800001UL >> (int)type) & 1UL) != 0;
		}
	}
}
