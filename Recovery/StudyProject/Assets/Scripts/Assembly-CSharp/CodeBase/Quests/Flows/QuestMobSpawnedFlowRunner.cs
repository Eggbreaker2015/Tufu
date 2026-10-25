using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.Camera;
using Codebase.Core;
using Codebase.Core.Flows;
using Codebase.Enemy;
using Codebase.Player.Models;
using Codebase.Quests.Components;
using Codebase.Vfx;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Scellecs.Morpeh;
using UnityEngine;
using DG.Tweening;
using Codebase.Unit.Components;

namespace Codebase.Quests.Flows
{
	public class QuestMobSpawnedFlowRunner : IFlowRunner<QuestMobSpawnedFlow>, IFlowHandlerBase
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CAnimateCameraToTarget_003Ed__12 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public Transform anchor;
			public Transform target;
			private Vector3 _003CstartPosition_003E5__2;
			private float _003Celapsed_003E5__3;
			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__1;
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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CExecute_003Ed__7 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public QuestMobSpawnedFlowRunner _003C_003E4__this;
			public QuestMobSpawnedFlow flow;
			private Stash<QuestMobTag> _003CmobTagStash_003E5__2;
			private EnemyUnit _003CmobUnit_003E5__3;
			private Entity _003CmobEntity_003E5__4;
			private bool _003CplaySpawnIn_003E5__5;
			private Transform _003Canchor_003E5__6;
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

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CFinishSpawnIn_003Ed__10 : IAsyncStateMachine
		{
			public int _003C_003E1__state;
			public AsyncUniTaskMethodBuilder _003C_003Et__builder;
			public EnemyUnit mobUnit;
			public Entity entity;
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

		private const float FlightDuration = 1f;
		private const float SpawnVfxLifetime = 3f;
		private const float ScaleBounceDuration = 0.55f;
		private const float HoldDuration = 1.95f;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private CameraFollowSystem cameraFollowSystem;
		[Inject]
		private WorldVfxService worldVfxService;
		public async UniTask Execute(QuestMobSpawnedFlow flow)
		{ // Original ELF 0x1438a1c; independent candidate only.
			playerModel.CanMove = false;
			var mobTagStash = World.Default.GetStash<QuestMobTag>();
			var mobUnit = flow.TargetTransform != null ? flow.TargetTransform.GetComponent<EnemyUnit>() : null;
			Entity mobEntity = mobUnit != null ? mobUnit.EntityProvider.Entity : default;
			bool playSpawnIn = mobUnit != null && mobTagStash.Has(mobEntity) && !mobTagStash.Get(mobEntity).SpawnAnimPlayed;
			if (playSpawnIn)
				BeginSpawnIn(mobEntity, mobUnit);
			var anchor = CreateCameraAnchor();
			cameraFollowSystem.Follow(anchor, 0);
			await AnimateCameraToTarget(anchor, flow.TargetTransform);
			anchor.SetParent(flow.TargetTransform);
			anchor.localPosition = Vector3.zero;
			if (playSpawnIn && flow.SpawnVfx != null)
				worldVfxService.Spawn(flow.SpawnVfx, flow.TargetTransform.position, Quaternion.identity, SpawnVfxLifetime);
			if (playSpawnIn)
			{
				await FinishSpawnIn(mobEntity, mobUnit);
				MarkSpawnAnimPlayed(mobTagStash, mobEntity);
			}

			await UniTask.Delay(System.TimeSpan.FromSeconds(HoldDuration));
			cameraFollowSystem.Unfollow();
			UnityEngine.Object.Destroy(anchor.gameObject);
			playerModel.CanMove = true;
		}

		private void MarkSpawnAnimPlayed(Stash<QuestMobTag> stash, Entity entity)
		{ // Original ELF 0x1438adc; independent candidate only.
			stash.Get(entity).SpawnAnimPlayed = true;
		}

		private void BeginSpawnIn(Entity entity, EnemyUnit mobUnit)
		{ // Original ELF 0x1438b40; independent candidate only.
			World.Default.GetStash<UnitSpawnActiveMarker>().Set(entity);
			mobUnit.VisualRoot.DOKill();
			mobUnit.VisualRoot.localScale = Vector3.zero;
		}

		private async UniTask FinishSpawnIn(Entity entity, EnemyUnit mobUnit)
		{ // Original ELF 0x1438c78; independent candidate only.
			mobUnit.VisualRoot.DOKill();
			mobUnit.VisualRoot.localScale = Vector3.zero;
			mobUnit.VisualRoot.DOScale(Vector3.one, ScaleBounceDuration).SetEase(Ease.OutBounce);
			await UniTask.Delay(System.TimeSpan.FromSeconds(ScaleBounceDuration));
			var stash = World.Default.GetStash<UnitSpawnActiveMarker>();
			if (stash.Has(entity))
				stash.Remove(entity);
		}

		private Transform CreateCameraAnchor()
		{ // Original ELF 0x1438d20; independent candidate only.
			var anchor = new GameObject("QuestMobSpawnCameraAnchor").transform;
			anchor.position = playerModel.Unit.transform.position;
			return anchor;
		}

		private async UniTask AnimateCameraToTarget(Transform anchor, Transform target)
		{ // Original ELF 0x1438de8; independent candidate only.
			Vector3 startPosition = anchor.position;
			float elapsed = 0;
			do
			{
				elapsed += Time.deltaTime;
				float inverse = 1f - Mathf.Clamp01(elapsed / FlightDuration);
				float eased = 1f - inverse * inverse;
				anchor.position = Vector3.Lerp(startPosition, target.position, eased);
				await UniTask.Yield();
			}
			while (elapsed < FlightDuration);
			anchor.position = target.position;
		}
	}
}
