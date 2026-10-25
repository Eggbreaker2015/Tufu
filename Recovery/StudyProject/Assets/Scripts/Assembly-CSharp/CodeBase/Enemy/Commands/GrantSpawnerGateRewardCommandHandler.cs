using Codebase.Gates;
using Cysharp.Threading.Tasks;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Commands;
using Codebase.UI;

namespace Codebase.Enemy.Commands
{
	public struct GrantSpawnerGateRewardCommandHandler : ICommandHandler<GrantSpawnerGateRewardCommand>
	{
		[Inject]
		private IAudioPlayer audioPlayer;

		[Inject]
		private UiModel uiModel;

		public void Execute(GrantSpawnerGateRewardCommand command)
		{
            var gates = new System.Collections.Generic.List<Gate>();
            foreach (Gate gate in command.Spawner.Config.Gates)
                if (gate != null && !gate.IsUnlocked) gates.Add(gate);
            if (gates.Count == 0) return;
            audioPlayer.PlaySoundOnce("unlock");
            uiModel.IsGateOpeningShowing = true;
            App.InitiateFlow(new SequentialGateOpeningFlow { Gates = gates }).Forget();
        }
	}
}
