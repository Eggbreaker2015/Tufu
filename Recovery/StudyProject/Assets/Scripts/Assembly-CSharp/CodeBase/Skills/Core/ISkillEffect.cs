namespace Codebase.Skills
{
	public interface ISkillEffect
	{
		void Apply(in SkillCastContext context);
	}
}
