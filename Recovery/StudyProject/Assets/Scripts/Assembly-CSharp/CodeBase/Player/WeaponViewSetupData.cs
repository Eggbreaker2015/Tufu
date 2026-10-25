using Codebase.Player.Chain;
using Codebase.Player.Inventory;
using Codebase.Player.Models;
using UnityEngine;

namespace Codebase.Player
{
	public struct WeaponViewSetupData
	{
		public ItemSlotModel Model;

		public Transform Target;

		public PlayerChainDeformer ChainDeformer;

		public int SlotIndex;

		public WeaponSlotSide SlotSide;

		public Transform PlayerTransform;

		public PlayerModel PlayerModel;

		public string AnimationKey;

		public Animator Animator;

		public Vector3 PositionOffset;

		public Vector3 RotationOffset;

		public Vector3 ScaleOffset;

		public bool UseWorldIdle;

		public Transform IdleReference;
	}
}
