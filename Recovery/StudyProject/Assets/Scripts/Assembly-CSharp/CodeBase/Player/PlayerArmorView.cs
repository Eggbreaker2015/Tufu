using Codebase.Player.Inventory;
using Codebase.Player.Models;
using Codebase.Utilities;
using UnityEngine;

namespace Codebase.Player
{
	public class PlayerArmorView : MonoBehaviour
	{
		[SerializeField]
		[KeysDropdown(typeof(ItemSlotKeys))]
		private string slotKey;

		[SerializeField]
		private Transform target;

		[Header("Chain death/revive")]
		[SerializeField]
		private float fallDuration;

		[SerializeField]
		private float fallRadius;

		[SerializeField]
		private float bounceHeight;

		[SerializeField]
		private float bounceDuration;

		[SerializeField]
		private float armorPopDuration;

		[SerializeField]
		private float chainStaggerMax;

		[SerializeField]
		private Vector2 stickAngleRange;

		[SerializeField]
		private float stickProbability;

		private GameObject view;

		private Vector3 viewIdleScale;

		private string currentArmorKey;

		private bool deferViewCreation;

		private ItemSlotModel currentSlot;

		private PlayerModel player;

		private void Awake()
		{
		}

		private void Start()
		{
		}

		private void OnDestroy()
		{
		}

		private void HandleDeathPhase(DeathPhase phase, float duration)
		{
		}

		private void PlayDeathSequence(float duration)
		{
		}

		private void AnimateArmorFall(GameObject v, float stagger)
		{
		}

		private void VanishForWarp(float duration)
		{
		}

		private void PlayReviveSequence(float duration)
		{
		}

		private GameObject BuildView()
		{
			return null;
		}

		private void Update()
		{
		}
	}
}
