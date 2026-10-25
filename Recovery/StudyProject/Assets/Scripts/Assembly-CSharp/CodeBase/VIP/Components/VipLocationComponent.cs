using System;
using Scellecs.Morpeh;
using UnityEngine;

namespace Codebase.VIP.Components
{
	[Serializable]
	public struct VipLocationComponent : IComponent
	{
		public GameObject Door;

		public int LocationNumber;

		public Transform NotificationPoint;

		public Transform TeleportPoint;
	}
}
