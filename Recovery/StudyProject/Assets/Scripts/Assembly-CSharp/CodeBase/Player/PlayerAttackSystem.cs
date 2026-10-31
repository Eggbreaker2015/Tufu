using Codebase.Player.Inventory;
using UnityEngine;

namespace Codebase.Player
{
	public class PlayerAttackSystem : MonoBehaviour
	{
		[SerializeField]
		private GameObject hitVfx;

		public GameObject HitVfx => hitVfx;

		public static float GetAnimationCastPoint(ItemModel weapon) => weapon.Config.AnimationCastPoint;
	}
}
