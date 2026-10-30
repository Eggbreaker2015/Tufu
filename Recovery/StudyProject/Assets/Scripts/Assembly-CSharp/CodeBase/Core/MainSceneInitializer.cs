using System;
using System.Diagnostics;
using Codebase.Hub;
using Codebase.Analytics.Events;
using Codebase.Localization;
using Multicast;
using Multicast.Backend;
using UniMob;
using UnityEngine.SceneManagement;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Codebase.ABTests;
using Codebase.Analytics;
using Codebase.Core.UserData;
using Codebase.Integrations;
using Codebase.Integrations.AppMetrica;
using Codebase.UI;
using Codebase.UserConsent;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using UnityEngine;

namespace Codebase.Core
{
	public class MainSceneInitializer : MonoBehaviour
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CInitializePostConsentAsync_003Ed__15 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public MainSceneInitializer _003C_003E4__this;

			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__1;

			private UniTask.Awaiter _003C_003Eu__2;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CLoadMainSceneWithoutActivationAsync_003Ed__14 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public MainSceneInitializer _003C_003E4__this;

			private UniTask.Awaiter _003C_003Eu__1;

			private Cysharp.Threading.Tasks.YieldAwaitable.Awaiter _003C_003Eu__2;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CStart_003Ed__13 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncVoidMethodBuilder _003C_003Et__builder;

			public MainSceneInitializer _003C_003E4__this;

			private BootTimeReporter _003Creporter_003E5__2;

			private GameDataService _003CgameDataService_003E5__3;

			private UniTask<bool>.Awaiter _003C_003Eu__1;

			private UniTask.Awaiter _003C_003Eu__2;

			private void MoveNext()
			{
			}

			void IAsyncStateMachine.MoveNext()
			{
				//ILSpy generated this explicit interface implementation from .override directive in MoveNext
				this.MoveNext();
			}

			[DebuggerHidden]
			private void SetStateMachine(IAsyncStateMachine stateMachine)
			{
			}

			void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
			{
				//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
				this.SetStateMachine(stateMachine);
			}
		}

		[SerializeField]
		private LoadingScreen loadingScreen;

		[SerializeField]
		private SnapshotSelectorView snapshotSelector;

		[SerializeField]
		private ABTestInitializer abTestInitializer;

		[SerializeField]
		private MaxUserConsentService consentService;

		[SerializeField]
		private AdjustInitializer adjustInitializer;

		[SerializeField]
		private FirebaseInitializer firebaseInitializer;

		[SerializeField]
		private AppMetricaTrackingUpdater appMetricaTrackingUpdater;

		private AsyncOperation mainSceneLoad;

		private BootTimeReporter bootReporter;
#if UNITY_EDITOR
        private readonly System.Collections.Generic.List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject>> editorStartupAssets =
            new System.Collections.Generic.List<UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject>>();
#endif

        // Restored from APK ARM64 methods 0x1539928..0x153ac10.
        public static bool ShowSnapshotSelectorOnce
        {
            get => PlayerPrefs.GetInt("ShowSnapshotSelectorOnce", 0) != 0;
            set => PlayerPrefs.SetInt("ShowSnapshotSelectorOnce", value ? 1 : 0);
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            DontDestroyOnLoad(loadingScreen.gameObject);
            loadingScreen.SetProgress(0f);
            GameContext.Set(new AppMetricaAnalyticsWrapper());
            bootReporter = new BootTimeReporter();
            bootReporter.Start();
            GameContext.Set(bootReporter);
        }

        private async void Start()
        {
            var cancellationToken = destroyCancellationToken;
            try { await InitializeAsync(); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        }

        private async UniTask InitializeAsync()
        {
            BootTimeReporter reporter = GameContext.Get<BootTimeReporter>();
            Disposer disposer = Disposer.WhileAppRunning;
            var (url, gameId) = HubBackendBootstrap.GetBackendInfo();
            var gameDataService = new GameDataService(Lifetime.Eternal);
            if (!BackendAuthGuest.HasGuestDeviceIdCreated && gameDataService.TryLoadAnySave(out var existing))
            {
                string guestId = existing.Value.Player.HubGuestId.Value;
                if (!string.IsNullOrEmpty(guestId)) BackendAuthGuest.GuestDeviceId = guestId;
            }
            // Simulation sandbox (stage 5 C0b): skip backend, analytics and integration startup.
            bool skipIntegrations = Codebase.Simulation.Harness.SimulationSandbox.Active && Codebase.Simulation.Harness.SimulationSandbox.Request.Sandbox.SkipIntegrations;
            if (!skipIntegrations) BackendApi.Init(disposer, gameId, url);
            if (!skipIntegrations) await HubBackendBootstrap.TryLogin(GameContext.Get<AppMetricaAnalyticsWrapper>());
            reporter.ReportStep("cloud_save");
            GameContext.Set(gameDataService);
            gameDataService.Initialize();
            // The original callback awaited an empty UniTask method.
            gameDataService.OnAfterSave += _ => UniTask.CompletedTask.Forget();
            await loadingScreen.PlayLaunchSoundAsync();
            reporter.ReportStep("game_data_service");
            string savedGuestId = gameDataService.Data.Player.HubGuestId.Value;
            if (!string.IsNullOrEmpty(savedGuestId) && BackendApi.IsLoggedIn && BackendAuthGuest.GuestDeviceId == savedGuestId)
                gameDataService.Data.Player.HubGuestId.Value = null;
            abTestInitializer.Initialize();
            LocalizationService.Initialize();
            loadingScreen.ShowRandomTip();
            reporter.ReportStep("ab_tests");
            reporter.ReportStep("scene_preloaded_and_consent_started");
            await (skipIntegrations ? LoadMainSceneWithoutActivationAsync() : UniTask.WhenAll(LoadMainSceneWithoutActivationAsync(), consentService.InitializeAndShowUserConsent()));
            reporter.ReportStep("scene_preloaded_and_consent_finished");
            await InitializePostConsentAsync();
        }

        private async UniTask LoadMainSceneWithoutActivationAsync()
        {
            loadingScreen.SetProgress(0f);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5), false, PlayerLoopTiming.Update);
#if UNITY_EDITOR
            await PreloadEditorStartupAssetsAsync();
#endif
            mainSceneLoad = SceneManager.LoadSceneAsync("Main");
            mainSceneLoad.allowSceneActivation = false;
            while (mainSceneLoad.progress < 0.9f)
            {
                loadingScreen.SetProgress(Mathf.Clamp01(mainSceneLoad.progress / 0.9f) * 0.75f);
                await UniTask.Yield(PlayerLoopTiming.Update);
                bootReporter.Tick();
            }
            loadingScreen.SetProgress(0.75f);
#if UNITY_EDITOR
            await RecoveryCompatibility.EditorEnemySpawnTemplates.PrepareAsync(
                progress => loadingScreen.SetProgress(0.75f + progress * 0.06f), destroyCancellationToken);
#endif
        }

        private async UniTask InitializePostConsentAsync()
        {
            loadingScreen.SetProgress(0.82f);
            if (!(Codebase.Simulation.Harness.SimulationSandbox.Active && Codebase.Simulation.Harness.SimulationSandbox.Request.Sandbox.SkipIntegrations))
            {
            appMetricaTrackingUpdater.ApplyConsent();
            if (PlayerPrefs.GetInt("game_loaded_once", 0) == 0)
                GameContext.Get<AppMetricaAnalyticsWrapper>().Send(new BootTimeEvent(0));
            adjustInitializer.InitializeAsync().Forget();
            firebaseInitializer.InitializeAsync().Forget();
            HubUpdateAttributesFlowRunner.CollectDeviceIdentifiers();
            }
            loadingScreen.DisableAudioListener();
            mainSceneLoad.allowSceneActivation = true;
            while (!mainSceneLoad.isDone)
            {
                float value = Mathf.Clamp01((mainSceneLoad.progress - 0.9f) / (1f - 0.9f));
                loadingScreen.SetProgress(Mathf.Max(0f, value) * (1f - 0.88f) + 0.88f);
                await UniTask.Yield(PlayerLoopTiming.Update);
                bootReporter.Tick();
            }
#if UNITY_EDITOR
            var bootstrap = GameContext.Get<global::Codebase.Enemy.Systems.EnemySpawnerBootstrapSystem>();
            while (!bootstrap.IsNearbyBootstrapComplete)
            {
                await UniTask.NextFrame(cancellationToken: destroyCancellationToken);
                bootReporter.Tick();
            }
#endif
            loadingScreen.SetProgress(1f);
            await loadingScreen.DismissAsync();
        }

#if UNITY_EDITOR
        private async UniTask PreloadEditorStartupAssetsAsync()
        {
            // First nearby chest/quest prompts otherwise initialize Addressables
            // and synchronously open the recovered local UI bundle in an ECS frame.
            // Start before the non-activated scene: Unity's async operation queue
            // would be blocked by allowSceneActivation=false if we awaited later.
            var addresses = new[] {
                "Assets/Prefabs/UI/SecretChestButtonView.prefab",
                "Assets/Prefabs/UI/Mines/Quests/QuestPopup.prefab"
            };
            foreach (string address in addresses)
            {
                var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(address);
                editorStartupAssets.Add(handle);
                while (!handle.IsDone)
                    await UniTask.NextFrame(cancellationToken: destroyCancellationToken);
                if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                    throw new InvalidOperationException("Startup UI prefab failed to preload: " + address, handle.OperationException);
            }
        }

        private void OnDestroy()
        {
            foreach (var handle in editorStartupAssets)
                if (handle.IsValid()) UnityEngine.AddressableAssets.Addressables.Release(handle);
            editorStartupAssets.Clear();
        }
#endif
    }
}
