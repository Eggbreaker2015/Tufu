using System.Collections.Generic;
using Codebase.Core;
using UniMob;
using UniMob.Core;

namespace Codebase.Mail
{
	public class MailsRepository : ModelRepository<MailModel, MailRepoUd>
	{
		private ComputedAtom<List<MailModel>> __Values;

		private ComputedAtom<List<MailModel>> __ActiveValuesByReceiveTime;

		private ComputedAtom<List<MailModel>> __ArchivedValues;

		private ComputedAtom<bool> __HasAnyInteractable;

		private ComputedAtom<bool> __HasAnyUnread;

		public List<MailModel> Values => null;

		public List<MailModel> ActiveValuesByReceiveTime => null;

		public List<MailModel> ArchivedValues => null;

		public bool HasAnyInteractable => false;

		public bool HasAnyUnread => false;

		public MailsRepository(Lifetime lifetime)
			: base(default(Lifetime))
		{
		}

		public void Populate()
		{
		}

		public MailModel GetByKey(string mailKey)
		{
			return null;
		}

		public MailModel GetOrCreate(string mailKey)
		{
			return null;
		}

		public bool RemoveByKey(string mailKey)
		{
			return false;
		}
	}
}
