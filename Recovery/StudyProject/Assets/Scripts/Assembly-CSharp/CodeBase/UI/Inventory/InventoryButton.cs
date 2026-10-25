using UniMob;
using Codebase.Core;
using Codebase.Core.UI;
using Codebase.Player.Models;
using UnityEngine;

namespace Codebase.UI.Inventory
{
	public class InventoryButton : View
	{
		[SerializeField]
		private GameObject notification;

		[Inject]
		private PlayerModel playerModel;

		private float timer;

		protected override void Start()
		{
			// Native ELF VA 0x164c338.
			base.Start();
			Atom.Reaction(Lifetime, Render);
		}

		private void LateUpdate()
		{
			// Native ELF VA 0x164c3d4.
			timer -= Time.deltaTime;
			if (timer > 0f) return;
			timer = 1f;
			Render();
		}

		protected override void Render()
		{
			// Native ELF VA 0x164c428.
			notification.SetActive(playerModel.HasInventoryNotification);
		}
	}
}
