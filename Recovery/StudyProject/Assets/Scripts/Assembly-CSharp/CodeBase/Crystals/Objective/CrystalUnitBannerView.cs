using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.Crystals.Objective
{
	public class CrystalUnitBannerView : MonoBehaviour
	{
		private const string HintLocKey = "CRYSTAL_OBJECTIVE_HINT";

		private const string RespawnGeodesLocKey = "CRYSTAL_OBJECTIVE_RESPAWN_GEODES_HINT";

		private const string RespawnRewardLocKey = "CRYSTAL_OBJECTIVE_RESPAWN_REWARD_HINT";

		[SerializeField]
		private GameObject hintState;

		[SerializeField]
		private GameObject respawnState;

		[SerializeField]
		private TMP_Text hintText;

		[SerializeField]
		private TMP_Text respawnGeodesText;

		[SerializeField]
		private TMP_Text respawnRewardText;

		[SerializeField]
		private Slider respawnProgressSlider;

		[SerializeField]
		private TMP_Text respawnProgressText;

		private CrystalModel crystalModel;

		public void Initialize()
		{
		}

		public void RenderState()
		{
		}
	}
}
