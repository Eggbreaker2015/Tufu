using Codebase.Core;
using Codebase.Core.UI;
using Codebase.SecretChest;
using Codebase.SecretChest.Commands;
using Codebase.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codebase.UI.SecretChest
{
    public class SecretChestPromptView : View
    {
        [SerializeField] private Button openButton;
        [SerializeField] private GameObject foundLabel;
        [SerializeField] private TMP_Text foundText;
        [Inject] private SecretChestModel secretChestModel;
        private string chestKey;
        private bool consumed;

        protected override void Awake()
        {
            // APK ARM64 0x1631314. Keep the listener across pool reuse.
            base.Awake();
            openButton.onClick.AddListener(HandleOpen);
        }

        public void Setup(string key)
        {
            // APK 0x16313b0.
            chestKey = key;
            consumed = false;
            Initialize();
            Render();
        }

        public void Dispose()
        {
            // APK 0x16313d8.
            consumed = true;
            Cleanup();
        }

        protected override void Render()
        {
            // APK 0x16313ec; retain original localization and global saved count.
            bool opened = secretChestModel.IsOpened(chestKey);
            openButton.gameObject.SetActive(!opened && !consumed);
            openButton.interactable = !consumed;
            foundLabel.SetActive(opened);
            if (opened)
                foundText.text = LocalizationStrings.Get("SECRET_CHEST_FOUND_COUNT")
                    .Replace("{value}", secretChestModel.OpenedCount.ToString())
                    .Replace("{total}", secretChestModel.TotalCount.ToString());
        }

        private void HandleOpen()
        {
            // APK 0x16315b8: consume before dispatch, including repeated clicks in one frame.
            if (consumed || secretChestModel.IsOpened(chestKey)) return;
            consumed = true;
            openButton.gameObject.SetActive(false);
            App.ExecuteCommand(new OpenSecretChestCommand { ChestKey = chestKey });
        }
    }
}
