using Codebase.Utilities;
using System;

namespace Codebase.Unit
{
	[Serializable]
	public class StatModifier
	{
		public string Name;

		public StatType StatType;

		public ModifierType ModifierType;

		public ModifierSource Source;

		public float Value;

		public int Param;

		public string StringParam;

		public float Duration { get; set; }

		public static double GetEventPercentBonusPercent(double value)
		{
			return Math.Sqrt(value) * Math.Sqrt(Math.Min(value, 20.0));
		}

		public static double GetEventPercentMultiplier(double value)
		{
			return GetEventPercentBonusPercent(value) * 0.01 + 1.0;
		}

		public override string ToString()
		{
			// Native ELF VA 0x15ca9f4. Decompiled C spills at terminal string helper; original bounded ARM64 determines all branches.
			uint kind=(uint)ModifierType;
			if(kind<8u && ((1u<<(int)kind)&0x36u)!=0u) return "x "+(Value*100f).ToString()+"%";
			if(kind==7u) return ((double)Value).Format()+" (+"+GetEventPercentBonusPercent(Value).Format()+"%)";
			return ((double)Value).Format() ?? string.Empty;
		}
	}
}
