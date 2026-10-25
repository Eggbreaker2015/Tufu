using Codebase.BossTower;
using Codebase.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CodeBase.Services.Cheats
{
	public class SpawnBossCheatPanel : MonoBehaviour
	{
		[SerializeField]
		private CheatButton buttonPrefab;

		[SerializeField]
		private TMP_InputField optimumTimeField;

		[SerializeField]
		private Toggle toggle;

		[SerializeField]
		private Transform bossListRoot;

		[Inject]
		private BossTowerConfig bossTowerConfig;

		[Inject]
		private BossTowerModel bossTowerModel;

		private float optimunDays;

		private bool isSetup;

		private void Awake()
		{
		}

		public void Open()
		{
		}

		public void Close()
		{
		}

		private void PopulateBossList()
		{
		}

		private void Fight(string bossKey)
		{
		}
	}
}
