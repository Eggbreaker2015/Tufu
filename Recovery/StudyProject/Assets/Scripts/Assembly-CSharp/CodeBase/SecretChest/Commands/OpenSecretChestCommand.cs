using Codebase.Core.Commands;

namespace Codebase.SecretChest.Commands
{
	public struct OpenSecretChestCommand : ICommand
	{
		public string ChestKey;
	}
}
