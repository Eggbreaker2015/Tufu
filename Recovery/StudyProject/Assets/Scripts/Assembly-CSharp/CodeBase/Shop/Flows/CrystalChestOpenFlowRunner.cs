using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Core.UI;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using System.Collections.Generic;
using Codebase.Crystals;
using Codebase.Crystals.Commands;
using Codebase.UI.Crystals;

namespace Codebase.Shop.Flows
{
	public class CrystalChestOpenFlowRunner : IFlowRunner<CrystalChestOpenFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__3 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public CrystalChestOpenFlowRunner _003C_003E4__this;
			public CrystalChestOpenFlow flow;
			private UniTask.Awaiter _003C_003Eu__1;
			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[Inject]
		private ChestsModel chestsModel;
		[Inject]
		private UINavigator navigator;
		[Inject]
		private IAudioPlayer audioPlayer;
		public async UniTask Execute(CrystalChestOpenFlow flow)
		{
			var crystals = new List<CrystalItemModel>();
			for (int i = 0; i < flow.ChestCount; i++)
				crystals.Add(App.ExecuteCommand<GenerateCrystalCommand, CrystalItemModel>(new GenerateCrystalCommand { ForcedRarity = chestsModel.RollCrystalRarity(), SkipGeodeSpend = true, BlacksmithingBonus = 3, EnchantLevel = chestsModel.CrystalChestEnchantLevel }));
			audioPlayer.PlaySoundOnce("chest_buy");
			var view = navigator.Open<AmuletCrystalsBlacksmithView>("Assets/Prefabs/UI/Amulet/Crystals/AmuletCrystalsBlacksmithView.prefab", true);
			await view.BeginExternalReveal(crystals);
		}
	}
}
