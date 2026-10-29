using System;
using System.Collections.Generic;
using UnityEngine;

namespace Codebase.Skills
{
	[Serializable]
	public class SkillAutoCast
	{
		public int Priority;

		public SkillPick Pick;

		[SerializeReference]
		public List<ISkillCastCondition> UseWhen = new List<ISkillCastCondition>();
	}
}
