using BrunoMikoski.AnimationSequencer;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Player.Models;
using Codebase.UI.World;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Codebase.Gates
{
	public class Portal : MonoBehaviour
	{
		[Inject]
		private DynamicWorldCanvas worldCanvas;
		[Inject]
		private PlayerModel playerModel;
		[Inject]
		private BossTowerModel bossTowerModel;
		[SerializeField]
		private PortalWorldView portalWorldViewPrefab;
		[SerializeField]
		private Transform point;
		[SerializeField]
		private AnimationSequencerController unlockAnimation;
		[SerializeField]
		private GameObject unlockedStateObject;
		[SerializeField]
		private GameObject hasTicketsEffect;
		[SerializeField]
		private GameObject[] stones;
		[SerializeField]
		public ParticleSystem Trail;
		[SerializeField]
		public ParticleSystem Explosion;
		public Transform playerSpawnPosition;
		private Vector3[] stonePositions;
		private PortalWorldView worldView;
		private int stonesPrevious;
		public int GemsTotal
		{
			get
			{
				return stones.Length;
			}
		}

		private void Awake()
		{
			global::Codebase.Core.GameContext.Set(this);
			stonePositions = new Vector3[stones.Length];
			for (int i = 0; i < stones.Length; i++)
				stonePositions[i] = stones[i].transform.position;
		}

		private void Start()
		{
			UpdateStones();
		}

		private void Update()
		{
			if (worldView == null && playerModel.PortalGemsCollected >= 0)
				CreateWorldView();
			if (stonesPrevious != playerModel.PortalGemsCollected)
			{
				stonesPrevious = playerModel.PortalGemsCollected;
				UpdateStones();
			}

			bool hasTickets = bossTowerModel.HasEnoughTickets(1) && playerModel.IsUnlockAnimationShown;
			if (hasTickets != hasTicketsEffect.activeSelf)
				hasTicketsEffect.SetActive(hasTickets);
			unlockedStateObject.SetActive(playerModel.IsUnlockAnimationShown);
		}

		private void CreateWorldView()
		{
			worldView = worldCanvas.CreateNew(portalWorldViewPrefab);
			Injector.InjectInto(worldView);
			worldView.Initialize(stones.Length);
			worldCanvas.AddTarget(point, worldView.GetComponent<RectTransform>());
		}

		private void UpdateStones()
		{
			for (int i = 0; i < stones.Length; i++)
				stones[i].SetActive(i < playerModel.PortalGemsCollected);
		}

		public UniTask PlayUnlockAnimation()
		{
			var completion = new UniTaskCompletionSource();
			unlockAnimation.Play(() => completion.TrySetResult());
			return completion.Task;
		}

		public void CleanupUnlockAnimation()
		{
			unlockAnimation.ResetToInitialState();
			unlockAnimation.SetProgress(0f, false);
		}

		public GameObject GetStone(int index)
		{
			stones[index].SetActive(true);
			Trail.transform.SetParent(stones[index].transform);
			Trail.transform.localPosition = Vector3.zero;
			Trail.Play();
			return stones[index];
		}

		public Vector3 GetStonePosition(int index)
		{
			return stonePositions[index];
		}

		public void PlayExplosion(Vector3 target)
		{
			Explosion.transform.position = target;
			Explosion.Stop();
			Explosion.Clear();
			Explosion.Play();
		}

		public void DisableTrail()
		{
			Trail.Stop();
		}
	}
}
