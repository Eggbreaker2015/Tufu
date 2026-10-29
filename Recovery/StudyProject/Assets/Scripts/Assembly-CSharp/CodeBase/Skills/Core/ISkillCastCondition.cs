namespace Codebase.Skills
{
	public interface ISkillCastCondition
	{
		bool Check(in SkillCastContext context);
	}
}
