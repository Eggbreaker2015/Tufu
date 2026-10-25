using System;
using CodeBase.Audio;
using Codebase.Core;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using Codebase.Enemy;
using Codebase.Player.Chain;
using Codebase.Utilities.ObjectPool;
using UnityEngine;

namespace Codebase.Player
{
	[DefaultExecutionOrder(-10)]
	public class PlayerDevour : MonoBehaviour
	{
		[SerializeField]
		private DevourSphere devourPrefab;

		[SerializeField]
		private GameObject devourTentacle;

		[SerializeField]
		private Material devourMaterial;

		[SerializeField]
		private float floatingTime;

		[SerializeField]
		private float catchingTime;

		[Header("Visual Config")]
		[SerializeField]
		private float sphereStartScale = 0.5f;

		[SerializeField]
		private float sphereFloatScale = 2f;

		[SerializeField]
		private float floatHeight = 3f;

		[SerializeField]
		private float visualShrinkScale = 0.5f;

		[SerializeField]
		private float visualRaiseHeight = 3f;

		[SerializeField]
		private float tentacleHeadScaleX = 1.2f;

		[SerializeField]
		private float tentacleHeadScaleZ = 1.2f;

		[SerializeField]
		private float tentacleLengthDiv = 1.7f;

		[Header("Hook Aim")]
		[SerializeField]
		private ChainAxisDirection hookTopAxis = ChainAxisDirection.Left;

		[SerializeField]
		private float hookOutSlack = 1.35f;

		[SerializeField]
		private float hookStraightenTime = 0.12f;

		[SerializeField]
		private float hookTargetHeightOffset = 1f;

		private readonly List<DevourAnimation> animations = new List<DevourAnimation>();

		private readonly ObjectPool<DevourSphere> devouringPool = new ObjectPool<DevourSphere>();

		private const float FinishTimeout = 0.5f;

		private GameObject hookRoot;

		private void Awake()
		{
// Native ELF VA 0x1442b94. The original static death event is the only registration input.
            EnemySpawner.OnDied += RegisterEnemy;
            hookRoot = new GameObject("DevourHookRoot");
            hookRoot.transform.position = transform.position;
            hookRoot.transform.localScale = transform.lossyScale;
		}

		private void OnDestroy()
		{
// Native ELF VA 0x1442d78.
            EnemySpawner.OnDied -= RegisterEnemy;
            for (int i = animations.Count - 1; i >= 0; i--) CleanupAnimation(animations[i]);
            animations.Clear();
            Destroy(hookRoot);
		}

		private void LateUpdate()
		{
// Native ELF VA 0x1443094; hook root stays in world space and follows only position.
            hookRoot.transform.position = transform.position;
		}

		private void RegisterEnemy(EnemyUnit enemy)
		{
// Native ELF VA 0x14430e8; creeper providers deliberately do not enter this animation path.
            if (enemy == null || enemy.TryGetComponent<CreeperMobProvider>(out _)) return;
            animations.Add(CreateAnimation(enemy));
		}

		private void Update()
		{
// Native ELF VA 0x1443484. Completion or the original timeout delivers the real devour callback.
            hookRoot.transform.position = transform.position;
            float deltaTime = Time.deltaTime;
            for (int i = animations.Count - 1; i >= 0; i--)
            {
                DevourAnimation state = animations[i];
                if (state.State == DevourState.Finished || state.Elapsed >= floatingTime + catchingTime + FinishTimeout)
                {
                    state.Enemy.OnDevoured?.Invoke(state.Enemy);
                    CleanupAnimation(state);
                    animations.RemoveAt(i);
                    continue;
                }
                if (state.State == DevourState.Pending && state.Elapsed < floatingTime)
                {
                    BeginFloating(state);
                    state.State = DevourState.Floating;
                }
                if (state.Elapsed >= floatingTime && state.State == DevourState.Floating)
                {
                    BeginCatching(state);
                    state.State = DevourState.Catching;
                }
                state.Elapsed += deltaTime;
            }
		}

		private DevourAnimation CreateAnimation(EnemyUnit enemy)
		{
// Native ELF VA 0x14431c4; all visuals use the original serialized prefab references.
            DevourSphere sphere = devouringPool.Spawn(devourPrefab, enemy.transform.position);
            sphere.transform.localScale = Vector3.one * sphereStartScale;
            GameObject tentacle = Instantiate(devourTentacle, hookRoot.transform);
            Transform head = tentacle.transform.GetChild(0);
            PlayerChainDeformer deformer = tentacle.GetComponent<PlayerChainDeformer>();
            deformer.SetEndAnchor(head);
            head.gameObject.SetActive(false);
            tentacle.SetActive(false);
            return new DevourAnimation
            {
                Enemy = enemy, Sphere = sphere, TentacleRoot = tentacle, TentacleHead = head,
                Particles = tentacle.transform.GetChild(1), Deformer = deformer,
                State = DevourState.Pending, Elapsed = 0f, OriginalMaterial = enemy.GetOriginalMaterial()
            };
		}

		private void BeginFloating(DevourAnimation s)
		{
// Native ELF VA 0x1443660. Bounded instructions retain vector components lost by decompiler output.
            s.Enemy.SetDeathState(true);
            Transform sphere = s.Sphere.transform;
            Transform visual = s.Enemy.VisualRoot;
            sphere.DOMoveY(sphere.position.y + floatHeight, floatingTime).SetEase(Ease.OutQuad);
            sphere.DOScale(Vector3.one * sphereFloatScale * s.Enemy.DevourScaleMultiplier, floatingTime).SetEase(Ease.OutCubic);
            visual.DOScale(Vector3.one * visualShrinkScale, floatingTime).SetEase(Ease.OutCubic);
            visual.DOLocalMoveY(visualRaiseHeight, floatingTime).SetEase(Ease.OutCubic);
            float x = UnityEngine.Random.Range(-30f, 30f);
            float z = UnityEngine.Random.Range(-30f, 30f);
            visual.DOLocalRotate(new Vector3(x, visual.localEulerAngles.y, z), floatingTime).SetEase(Ease.OutCubic);
		}

		private void BeginCatching(DevourAnimation s)
		{
// Native ELF VA 0x14438b8 and bounded closure bodies 0x14441f8..0x1444ed8.
            s.TentacleRoot.SetActive(true);
            float halfDuration = catchingTime * 0.5f;
            Transform visual = s.Enemy.VisualRoot;
            Vector3 outward = (visual.position - transform.position).normalized;
            visual.DOMove(visual.position + outward * 1.2f, 0.1f).SetEase(Ease.OutQuad);
            GameContext.Get<IAudioPlayer>().PlaySoundOnce("hook");
            Transform head = s.TentacleHead;
            float mouthHeight = head.position.y - hookRoot.transform.position.y;
            Vector3 MouthPoint() => hookRoot.transform.position + Vector3.up * mouthHeight;
            Vector3 target = s.Sphere.transform.position + Vector3.up * hookTargetHeightOffset;
            Vector3 topAxis = PlayerChainDeformer.AxisToVector(hookTopAxis);
            Quaternion roll = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), topAxis);
            Vector3 direction = target - head.position;
            if (direction.sqrMagnitude > 0.0001f) head.rotation = Quaternion.FromToRotation(topAxis, direction.normalized) * roll;
            s.Deformer.SetReachSlack(hookOutSlack);
            s.Deformer.SetDanceOverride(true);
            Vector3 headScale = head.localScale;
            head.localScale = Vector3.zero;
            head.gameObject.SetActive(true);
            head.DOScale(headScale, halfDuration * 0.05f);
            float straightenDuration = Mathf.Min(halfDuration * 0.5f, hookStraightenTime);
            float pullDuration = halfDuration - straightenDuration;
            head.DOMove(target, halfDuration).SetEase(Ease.OutQuint).OnComplete(() =>
            {
                s.Deformer.SetDanceOverride(false);
                Vector3 sphereOffset = s.Sphere.transform.position - head.position;
                Vector3 enemyOffset = s.Enemy.VisualRoot.position - head.position;
                float totalDuration = straightenDuration + pullDuration;
                Vector3 fixedStart = head.position;
                Vector3 pullStart = fixedStart;
                Sequence sequence = DOTween.Sequence().SetLink(s.TentacleRoot);
                sequence.Append(DOVirtual.Float(hookOutSlack, 1f, straightenDuration, value => s.Deformer.SetReachSlack(value)).SetEase(Ease.OutQuad));
                sequence.AppendCallback(() => pullStart = head.position);
                sequence.Append(DOVirtual.Float(0f, 1f, pullDuration, value => head.position = Vector3.Lerp(pullStart, MouthPoint(), value)).SetEase(Ease.InCubic));
                sequence.Insert(totalDuration * 0.95f, head.DOScale(Vector3.zero, totalDuration * 0.05f));
                sequence.OnUpdate(() =>
                {
                    float elapsed = sequence.Elapsed(true);
                    if (elapsed < straightenDuration) head.position = fixedStart;
                    Vector3 toMouth = MouthPoint() - head.position;
                    if (toMouth.sqrMagnitude > 0.0001f) head.rotation = Quaternion.FromToRotation(topAxis, -toMouth.normalized) * roll;
                    float progress = Mathf.Clamp01(sequence.Elapsed(true) / totalDuration);
                    float remainingOffset = straightenDuration > 0f ? Mathf.Clamp01(1f - sequence.Elapsed(true) / straightenDuration) : 0f;
                    s.Sphere.transform.position = head.position + sphereOffset * remainingOffset;
                    s.Enemy.VisualRoot.position = head.position + enemyOffset * remainingOffset;
                    float shrink = progress < 0.95f ? 1f : (progress - 0.95f) / -0.05f + 1f;
                    s.Sphere.transform.localScale = Vector3.Lerp(s.CatchStartSphereScale, s.CatchStartSphereScale * 0.5f, progress) * shrink;
                    s.Enemy.VisualRoot.localScale = Vector3.Lerp(s.CatchStartEnemyScale, s.CatchStartEnemyScale * 0.5f, progress) * shrink;
                });
                sequence.OnComplete(() => s.State = DevourState.Finished);
                s.Particles.parent = null;
                s.Particles.LookAt(s.Sphere.transform.position);
                s.Particles.gameObject.SetActive(true);
                Destroy(s.Particles.gameObject, 5f);
            });
            // Original closure calls Delay and discards it, then returns CompletedTask.
            // Do not turn this unused native task into a fabricated completion/reward trigger.
            UniTask.Create(() =>
            {
                UniTask.Delay(TimeSpan.FromSeconds(halfDuration - 0.1f), false, PlayerLoopTiming.Update);
                return UniTask.CompletedTask;
            });
            s.CatchStartSphereScale = s.Sphere.transform.localScale;
            s.CatchStartEnemyScale = s.Enemy.VisualRoot.localScale;
		}

		private void CleanupAnimation(DevourAnimation s)
		{
// Native ELF VA 0x1442f9c. Pool and linked tentacle ownership follow the original path.
            if (s.Sphere != null) devouringPool.Despawn(s.Sphere);
            if (s.TentacleRoot != null) Destroy(s.TentacleRoot);
		}
	}
}
