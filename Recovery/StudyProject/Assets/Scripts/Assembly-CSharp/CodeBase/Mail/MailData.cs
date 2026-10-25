using System;
using System.Collections.Generic;
using Codebase.Services.Time;
using Codebase.Unit;

namespace Codebase.Mail
{
	[Serializable]
	public class MailData
	{
		public string Guid;

		public string MailKey;

		public List<string> Attachment;

		public bool Viewed;

		public bool Claimed;

		public bool Deleted;

		[NonSerialized]
		public GameTime ReceiveTime;

		[NonSerialized]
		public GameTime ExpirationTime;

		public string ReceiveTimeString;

		public string ExpirationTimeString;

		public List<string> TextArgs;

		public int Gems;

		public List<StatModifier> StatRewards;

		public void SyncTimesToStrings()
		{
		}

		public void SyncStringsToTimes()
		{
		}
	}
}
