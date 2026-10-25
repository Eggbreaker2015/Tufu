namespace Codebase.Unit
{
	public static class StatTypeExtensions
	{
		public static bool IsPercentStat(this StatType type)
		{
			return false;
		}

		public static bool IsSpecificPercentStat(this StatType type)
		{
			return false;
		}

		public static bool IsPercentDisplay(this StatType type)
		{
			return false;
		}

		public static StatType GetIconStatType(this StatType type)
		{
			return StatType.MovementSpeed;
		}

		public static bool IsSpecificStat(this StatType type)
		{
			return false;
		}
	}
}
