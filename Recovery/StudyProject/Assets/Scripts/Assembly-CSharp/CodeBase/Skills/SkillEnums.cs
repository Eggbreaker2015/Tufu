using System;

namespace Codebase.Skills
{
	public enum BlinkMode
	{
		BehindTarget = 0,
		ToPoint = 1,
		AwayFromTarget = 2
	}

	public enum SkillTargetType
	{
		NoTarget = 0,
		Unit = 1,
		Point = 2,
		Passive = 3
	}

	// Relation bits (Enemy/Ally/Self) are resolved relative to the caster's side; kind bits (Hero/Pet/Boss), when any is set, narrow the match.
	[Flags]
	public enum SkillTargetFilter
	{
		None = 0,
		Enemy = 1,
		Ally = 2,
		Self = 4,
		Hero = 8,
		Pet = 16,
		Boss = 32,
		Alive = 64
	}

	public enum TargetSelectMode
	{
		Self = 0,
		Primary = 1,
		CircleCaster = 2,
		CirclePoint = 3,
		Cone = 4,
		Line = 5
	}

	public enum SkillPick
	{
		CurrentTarget = 0,
		NearestHostile = 1,
		LowestHpAlly = 2,
		Self = 3,
		DensestHostile = 4,
		// The hero when alive and in Range, else CurrentTarget.
		Hero = 5
	}

	public enum TelegraphShape
	{
		None = 0,
		Circle = 1,
		Cone = 2,
		Line = 3
	}

	public enum BuffStackMode
	{
		Refresh = 0,
		Stack = 1,
		Independent = 2,
		KeepLonger = 3
	}

	public enum SkillOwnerKind
	{
		Enemy = 0,
		Boss = 1,
		Pet = 2,
		Hero = 3
	}
}
