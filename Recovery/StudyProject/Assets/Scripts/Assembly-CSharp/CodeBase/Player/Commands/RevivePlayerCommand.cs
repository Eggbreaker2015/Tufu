using Codebase.Core.Commands;
using UnityEngine;

namespace Codebase.Player.Commands
{
	public struct RevivePlayerCommand : ICommand
	{
		public string KillerName;

		public bool restoreHealth;

		public Vector3 revivePosition;
	}
}
