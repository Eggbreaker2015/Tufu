using Codebase.VIP.Features;
using Codebase.Utilities;
using System.Linq;
using CodeBase.Audio;
using Codebase.Cashback;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Services.Cheats;
using Codebase.UI;
using Codebase.Unit;
using Codebase.VIP;
using JetBrains.Annotations;
using Scellecs.Morpeh;

namespace Codebase.Player.Systems
{
	public class PlayerRewardSystem : SystemBase
	{
		[Inject]
		private PlayerModel playerModel;

		[Inject]
		private CheatsModel cheatsModel;

		[Inject]
		private StatRewardContainer statRewardContainer;

		[Inject]
		private IconStyleConfig iconStyleConfig;

		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private VipModel vipModel;

		[Inject]
		private CashbackModel cashbackModel;

		public override void OnAwake()
		{
		}

		public override void OnUpdate(float deltaTime)
		{
		}

		[PublicAPI]
		public void GrantStatReward(StatModifier modifier, double displayOverride = -1.0, int displayMultiplier = 1)
		{
			return;
		}
	}
}
