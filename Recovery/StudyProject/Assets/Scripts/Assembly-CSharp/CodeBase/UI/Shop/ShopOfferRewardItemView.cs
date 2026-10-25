using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.Shop
{
	public class ShopOfferRewardItemView : MonoBehaviour
	{
		private const float DescriptionHeightMultiplier = 1.3f;

		[SerializeField]
		private GameObject rewardRoot;

		[SerializeField]
		private Image icon;

		[SerializeField]
		private GameObject soulsContainer;

		[SerializeField]
		private TMP_Text label;

		[SerializeField]
		private GameObject descriptionRoot;

		[SerializeField]
		private TMP_Text descriptionLabel;

		private RectTransform rectTransform;

		private float baseHeight;

		private void Awake()
		{
            // Native 0x1629bbc.
            rectTransform = (RectTransform)transform;
            baseHeight = rectTransform.rect.height;
		}

		public void Setup(ShopOfferBonusData data)
		{
            // Native 0x1628f5c.
            bool reward = data.Type == ShopOfferBonusType.Reward;
            rewardRoot.SetActive(reward); descriptionRoot.SetActive(!reward);
            float height;
            if (reward) {
                soulsContainer.SetActive(data.ShowSoulsShowcase);
                icon.enabled = !data.ShowSoulsShowcase;
                if (!data.ShowSoulsShowcase) icon.sprite = data.Icon;
                label.text = data.Text;
                height = baseHeight;
            } else {
                descriptionLabel.text = data.Text;
                height = baseHeight * DescriptionHeightMultiplier;
            }
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
		}
	}
}
