using Codebase.Core;
using Codebase.Core.UI;
using Codebase.LevelPass;
using Codebase.Rewards;
using Codebase.UI.Events;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.LevelPass
{
	public class LevelPassStageView : View
	{
		[Header("Progress Bar")]
		[SerializeField]
		private Image barFilled;

		[SerializeField]
		private Image barEmpty;

		[SerializeField]
		private Image barNotReached;

		[Header("Diamond Marker")]
		[SerializeField]
		private Image diamond;

		[SerializeField]
		private Image diamondOverlay;

		[SerializeField]
		private Sprite diamondFilledSprite;

		[SerializeField]
		private Sprite diamondEmptySprite;

		[Header("Info")]
		[SerializeField]
		private TMP_Text stageNumberText;

		[SerializeField]
		private TMP_Text previousStageNumberText;

		[SerializeField]
		private TMP_Text titleText;

		[SerializeField]
		private TMP_Text countText;

		[SerializeField]
		private Image progressBar;

		[Header("Rewards")]
		[SerializeField]
		private RewardView freeRewardView;

		[SerializeField]
		private Button freeRewardButton;

		[SerializeField]
		private RewardView paidRewardView1;

		[SerializeField]
		private RewardView paidRewardView2;

		[SerializeField]
		private Button paidRewardButton;

		[SerializeField]
		private Image paidLock1;

		[Header("Paid Reward Background")]
		[SerializeField]
		private Image paidRewardBgImage;

		[SerializeField]
		private GameObject paidRewardGlow;

		[SerializeField]
		private Sprite defaultRewardBgSprite;

		[SerializeField]
		private Sprite goldRewardBgSprite;

		[Header("Completion")]
		[SerializeField]
		private GameObject completedStage;

		[SerializeField]
		private GameObject completedText;

		[SerializeField]
		private GameObject rewardsContainer;

		[Header("Reward Backgrounds")]
		[SerializeField]
		private GameObject freeRewardClaimBg;

		[SerializeField]
		private GameObject paidRewardClaimBg;

		[Header("Islands")]
		[SerializeField]
		private Transform islandsContainer;

		[SerializeField]
		private LevelPassIslandTextView islandLabelPrefab;

		[Inject]
		private LevelPassModel levelPassModel;

		[Inject]
		private UINavigator navigator;

		private LevelPassTierData data;

		public void Setup(LevelPassTierData tierData)
		{
		}

		protected override void Render()
		{
		}

		private void RenderIslands()
		{
		}

		private void RenderRewards()
		{
		}

		private void SetupRewardView(RewardView view, RewardBase reward, bool isClaimed)
		{
		}

		private void SetupRewardBg(Image bgImage, RewardBase reward, bool isClaimed)
		{
		}

		private static bool IsGoldReward(RewardBase reward)
		{
			return false;
		}

		private void OnFreeRewardClicked()
		{
		}

		private void OnPaidRewardsClicked()
		{
		}

		private void OpenPurchaseView()
		{
		}
	}
}
