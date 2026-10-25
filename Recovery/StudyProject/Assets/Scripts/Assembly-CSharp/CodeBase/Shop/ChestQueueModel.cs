using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CodeBase.Audio;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.Rewards;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UniMob;

namespace Codebase.Shop
{
	public class ChestQueueModel : Model
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CWaitUntilQueueEmpty_003Ed__17 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public ChestQueueModel _003C_003E4__this;
			private UniTask<bool>.Awaiter _003C_003Eu__1;
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
		private PlayerModel playerModel;
		[Inject]
		private IAudioPlayer audioPlayer;
		private readonly Queue<ChestQueueEntry> queue;
		private bool isProcessing;
		private UniTaskCompletionSource<bool> completionSource;
		public bool HasPendingChests
		{
			get
			{
				return queue.Count > 0;
			}
		}

		public bool IsProcessing => this.isProcessing;

		public bool IsQueueActive
		{
			get
			{
				return HasPendingChests || isProcessing;
			}
		}

		public ChestQueueModel(Lifetime lifetime) : base(lifetime)
		{
			queue = new Queue<ChestQueueEntry>();
		}

		public void EnqueueChest(ChestDef chestDef, int count)
		{
			bool wasEmpty = queue.Count == 0;
			queue.Enqueue(new ChestQueueEntry { Type = ChestQueueEntryType.Chest, ChestDef = chestDef, Count = count });
			if (wasEmpty)
				OnQueueBecameNonEmpty();
		}

		public void EnqueueRewards(List<RewardBase> rewards, string chestKey)
		{
			if (rewards == null || rewards.Count == 0)
				return;
			bool wasEmpty = queue.Count == 0;
			queue.Enqueue(new ChestQueueEntry { Type = ChestQueueEntryType.Reward, Rewards = rewards, ChestKey = chestKey });
			if (wasEmpty)
				OnQueueBecameNonEmpty();
		}

		public bool TryDequeue(out ChestQueueEntry entry)
		{
			if (queue.Count == 0)
			{
				entry = default;
				return false;
			}

			entry = queue.Dequeue();
			return true;
		}

		public void SetProcessing(bool processing)
		{
			this.isProcessing = processing;
		}

		public void OnChestProcessingComplete()
		{
			if (queue.Count != 0 || isProcessing)
				return;
			OnQueueBecameEmpty();
			completionSource?.TrySetResult(true);
			completionSource = null;
		}

		public async UniTask WaitUntilQueueEmpty()
		{
			if (!HasPendingChests && !isProcessing)
				return;
			if (completionSource == null)
				completionSource = new UniTaskCompletionSource<bool>();
			await completionSource.Task;
		}

		private void OnQueueBecameNonEmpty()
		{
			playerModel.CanMove = false;
			audioPlayer.FadeMusic(true);
		}

		private void OnQueueBecameEmpty()
		{
			playerModel.CanMove = true;
			audioPlayer.FadeMusic(false);
		}
	}
}
