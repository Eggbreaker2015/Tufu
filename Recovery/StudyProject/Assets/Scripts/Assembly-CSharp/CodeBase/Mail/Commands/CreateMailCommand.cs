using System.Collections.Generic;
using Codebase.Core.Commands;
using Codebase.Rewards;
using Codebase.Services.Time;
using Codebase.Unit;

namespace Codebase.Mail.Commands
{
	public struct CreateMailCommand : ICommand
	{
		public string MailKey;

		public string TemplateKey;

		public string Description;

		public GameTime ReceiveTime;

		public GameTime ExpirationTime;

		public List<string> TextArgs;

		public int Gems;

		public int InAppValue;

		public int BoosterTokens;

		public string BoosterTokenProductId;

		public List<StatModifier> StatRewards;

		public List<string> Rewards;

		public List<RewardBase> RewardObjects;

		public bool ArchiveOnClaim;
	}
}
