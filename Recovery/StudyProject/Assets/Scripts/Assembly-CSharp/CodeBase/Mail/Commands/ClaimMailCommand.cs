using System;
using Codebase.Core.Commands;

namespace Codebase.Mail.Commands
{
	public struct ClaimMailCommand : ICommand
	{
		public MailModel Mail;

		public Action OnComplete;
	}
}
