using Codebase.Core;
using Codebase.Player.Models;
using Scellecs.Morpeh;

namespace Codebase.Player.Systems
{
	public class PlayerHitsTakenDecaySystem : SystemBase
	{
		public const float OutOfCombatTimeout = 5f;

		[Inject]
		private PlayerModel playerModel;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}
	}
}
