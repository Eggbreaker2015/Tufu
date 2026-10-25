using System.Collections.Generic;
using SimpleAssets.Common.Character;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleAssets.Common.Preview
{
	public class CharacterLoader : MonoBehaviour
	{
		[SerializeField]
		private List<GameObject> Prefabs;

		[SerializeField]
		private Toggle VFXToggle;

		public int CharacterIndex { get; private set; }

		public Animator CharacterAnimator { get; private set; }

		public Transform CharacterTranform { get; private set; }

		public CharacterVFX CharacterVFX { get; private set; }

		private void Awake()
		{
		}

		private void OnDestroy()
		{
		}

		private void Initialize()
		{
		}

		public void LoadCharacterAt(int index)
		{
		}

		private void VFXValueChanged(bool val)
		{
		}
	}
}
