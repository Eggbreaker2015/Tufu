using Codebase.Unit;

namespace Codebase.Crystals
{
	public static class CrystalColors
	{
		public static CrystalColor GetForStat(StatType stat)
		{
			switch ((int)stat)
			{
				case 1:
					return (CrystalColor)2;
				case 2:
					return (CrystalColor)2;
				case 3:
					return (CrystalColor)0;
				case 4:
					return (CrystalColor)0;
				case 5:
					return (CrystalColor)0;
				case 6:
					return (CrystalColor)0;
				case 7:
					return (CrystalColor)0;
				case 8:
					return (CrystalColor)0;
				case 9:
					return (CrystalColor)0;
				case 10:
					return (CrystalColor)0;
				case 11:
					return (CrystalColor)0;
				case 12:
					return (CrystalColor)0;
				case 13:
					return (CrystalColor)0;
				case 14:
					return (CrystalColor)1;
				case 15:
					return (CrystalColor)1;
				case 16:
					return (CrystalColor)1;
				case 17:
					return (CrystalColor)1;
				case 18:
					return (CrystalColor)1;
				case 19:
					return (CrystalColor)1;
				case 20:
					return (CrystalColor)1;
				case 21:
					return (CrystalColor)1;
				case 22:
					return (CrystalColor)2;
				case 23:
					return (CrystalColor)1;
				case 24:
					return (CrystalColor)0;
				case 25:
					return (CrystalColor)0;
				case 26:
					return (CrystalColor)1;
				case 27:
					return (CrystalColor)1;
				case 28:
					return (CrystalColor)0;
				case 29:
					return (CrystalColor)0;
				case 30:
					return (CrystalColor)0;
				case 31:
					return (CrystalColor)1;
				case 32:
					return (CrystalColor)2;
				case 33:
					return (CrystalColor)1;
				case 34:
					return (CrystalColor)1;
				default:
					return CrystalColor.Red;
			}
		}
	}
}
