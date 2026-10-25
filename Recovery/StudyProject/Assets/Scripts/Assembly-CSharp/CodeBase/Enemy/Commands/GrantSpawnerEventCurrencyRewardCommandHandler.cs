using Codebase.Events.Flows;
using Cysharp.Threading.Tasks;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.Events;
using Codebase.Events.Analytics;
using Codebase.Events.Models;
using Codebase.UI;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerEventCurrencyRewardCommandHandler : ICommandHandler<GrantSpawnerEventCurrencyRewardCommand>
	{
		[Inject]
		private EventsRepository eventsRepository;

		[Inject]
		private EventStyleConfig eventStyle;

		[Inject]
		private EventCurrencyRewardContainer eventCurrencyRewardContainer;

		[Inject]
		private EventAnalyticsModel eventAnalyticsModel;

		[Inject]
		private IAudioPlayer audioPlayer;

		public void Execute(GrantSpawnerEventCurrencyRewardCommand command)
		{
            var currentEvent = eventsRepository.CurrentEvent;
            if (currentEvent == null) return;
            var definition = command.Spawner.Config.EventDef;
            if (definition?.Key != currentEvent.Key) return;
            if (definition.UnitType == EventUnitType.MiniBoss)
            {
                int previous = currentEvent.Progress;
                currentEvent.AddProgress(1);
                if (currentEvent.Key == "crystal_event_roster")
                {
                    bool advanced = currentEvent.TryAdvanceRosterLeague(eventsRepository.EventPlan);
                    App.InitiateFlow(new RosterMiniBossKilledFlow { ProgressIndex = previous, DidAdvanceLeague = advanced }).Forget();
                }
                else App.InitiateFlow(new EventMiniBossKilledFlow { ProgressIndex = previous }).Forget();
            }
            if (definition.UnitType == EventUnitType.Boss)
            {
                var difficulty = currentEvent.Difficulty;
                currentEvent.AddProgress(currentEvent.ProgressMax);
                App.InitiateFlow(new EventBossChestRewardFlow { Difficulty = difficulty }).Forget();
            }
            if (definition.UnitType == EventUnitType.Mob) eventAnalyticsModel.IncrementEventMobKilled(currentEvent.Key);
            var reward = command.Unit.EventUnitReward;
            if (reward == null || reward.CurrencyAmount <= 0f) return;
            currentEvent.AddCurrency(reward.CurrencyKey, reward.CurrencyAmount);
            var style = eventStyle.GetCurrencyStyle(reward.CurrencyKey);
            string color = FIMSpace.FColorMethods.ColorToHex(style.Color, true);
            eventCurrencyRewardContainer.Create(string.Format("<color={0}>+{1}<sprite name={2}>", color, reward.CurrencyAmount, style.Sprite.name));
            audioPlayer.PlaySoundOnce("devour_event");
        }
	}
}
