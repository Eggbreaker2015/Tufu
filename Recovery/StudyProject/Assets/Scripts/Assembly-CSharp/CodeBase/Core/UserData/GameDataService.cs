using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using Codebase.Utilities;
using UnityEngine;
using Debug = UnityEngine.Debug;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.CompilerServices;
using Multicast.UserData;
using UniMob;
using UnityEngine.Pool;

namespace Codebase.Core.UserData
{
	public sealed class GameDataService : ILifetimeScope
	{
		[StructLayout((LayoutKind)3)]
		[CompilerGenerated]
		private struct _003CSaveToFileInternalInBackgroundAsync_003Ed__35 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskMethodBuilder _003C_003Et__builder;

			public GameDataService _003C_003E4__this;

			private CancellationToken _003Ctoken_003E5__2;

			private PooledObject<ArrayBufferWriter<byte>> _003C__003E5__3;

			private ArrayBufferWriter<byte> _003Cbuffer_003E5__4;

			private bool _003Csaved_003E5__5;

			private object _003C_003E7__wrap5;

			private int _003C_003E7__wrap6;

			private SwitchToThreadPoolAwaitable.Awaiter _003C_003Eu__1;

			private SwitchToMainThreadAwaitable.Awaiter _003C_003Eu__2;

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
		private struct _003CStartAutoSave_003Ed__26 : IAsyncStateMachine
		{
			public int _003C_003E1__state;

			public AsyncUniTaskVoidMethodBuilder _003C_003Et__builder;

			public GameDataService _003C_003E4__this;

			private CancellationToken _003CcancellationToken_003E5__2;

			private UniTask.Awaiter _003C_003Eu__1;

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

        public const string SAVE_FILE_NAME = "game_data.bin";
        public const string TEMP_FILE_NAME = "game_data_temp.bin";
        public const string BACKUP_FILE_NAME = "game_data_backup.bin";
        public const string MIGRATION_COMPLETE_KEY = "ud_migration_complete_v1";
        private const float AUTO_SAVE_INTERVAL_SECONDS = 5f;
        private readonly string savePath;
        private readonly string tempPath;
        private readonly string backupPath;
        // Compatibility: serialize the complete temp-write/promotion transaction across writers.
        private static readonly object saveFileLock = new object();
        private CancellationTokenSource saveTcs;
        private UdRoot<GameUserData> root;
        private Action m_OnBeforeSave;
        private Action<GameDataSaveReason> m_OnAfterSave;
        public Lifetime Lifetime { get; }
        public GameUserData Data => root.Value;
        public bool IsMigrated => IsMigrationComplete();
        public event Action OnBeforeSave
        {
            add
            {
                Action oldValue, newValue;
                do { oldValue = m_OnBeforeSave; newValue = (Action)Delegate.Combine(oldValue, value); }
                while (!ReferenceEquals(Interlocked.CompareExchange(ref m_OnBeforeSave, newValue, oldValue), oldValue));
            }
            remove
            {
                Action oldValue, newValue;
                do { oldValue = m_OnBeforeSave; newValue = (Action)Delegate.Remove(oldValue, value); }
                while (!ReferenceEquals(Interlocked.CompareExchange(ref m_OnBeforeSave, newValue, oldValue), oldValue));
            }
        }
        public event Action<GameDataSaveReason> OnAfterSave
        {
            add
            {
                Action<GameDataSaveReason> oldValue, newValue;
                do { oldValue = m_OnAfterSave; newValue = (Action<GameDataSaveReason>)Delegate.Combine(oldValue, value); }
                while (!ReferenceEquals(Interlocked.CompareExchange(ref m_OnAfterSave, newValue, oldValue), oldValue));
            }
            remove
            {
                Action<GameDataSaveReason> oldValue, newValue;
                do { oldValue = m_OnAfterSave; newValue = (Action<GameDataSaveReason>)Delegate.Remove(oldValue, value); }
                while (!ReferenceEquals(Interlocked.CompareExchange(ref m_OnAfterSave, newValue, oldValue), oldValue));
            }
        }

        public GameDataService(Lifetime lifetime)
        {
            Lifetime = lifetime;
            savePath = Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
            tempPath = Path.Combine(Application.persistentDataPath, TEMP_FILE_NAME);
            backupPath = Path.Combine(Application.persistentDataPath, BACKUP_FILE_NAME);
        }
        public void Initialize()
        {
            UdRoot.RegisterResolver(Generated.UserDataResolver.Instance);
            LoadOrCreate();
            if (!IsMigrationComplete()) MigrateFromLegacy();
            StartAutoSave().Forget();
        }
        public void OverrideSaveFile(byte[] bytes)
        {
            CancelBackgroundSave();
            lock (saveFileLock)
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
                File.WriteAllBytes(tempPath, bytes);
                if (File.Exists(backupPath)) File.Delete(backupPath);
                if (File.Exists(savePath)) File.Move(savePath, backupPath);
                File.Move(tempPath, savePath);
            }
        }
        private async UniTaskVoid StartAutoSave()
        {
            var cancellationToken = Lifetime.ToCancellationToken();
            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(AUTO_SAVE_INTERVAL_SECONDS), false, PlayerLoopTiming.Update, cancellationToken);
                m_OnBeforeSave?.Invoke();
                await SaveToFileInternalInBackgroundAsync();
            }
        }
        public void ForceSave()
        {
            m_OnBeforeSave?.Invoke();
            SaveToFileInternal(GameDataSaveReason.Forced);
        }
        public bool TryReadSaveBytes(out byte[] bytes)
        {
            bytes = null;
            if (!File.Exists(savePath)) return false;
            bytes = File.ReadAllBytes(savePath);
            return bytes.Length != 0;
        }
        private void LoadOrCreate()
        {
            foreach (var candidate in new[] { (label: "Main", path: savePath), (label: "Temp", path: tempPath), (label: "Backup", path: backupPath) })
            {
                if (TryLoadFromFile(candidate.path, out root, out var exception))
                {
                    Debug.Log("[GameDataService] Loaded from " + candidate.label + " successfully");
                    SaveToFileInternal(GameDataSaveReason.Auto);
                    CreateBackup();
                    return;
                }
                if (exception != null) Debug.LogError("[GameDataService] Failed to load from " + candidate.label + ": " + exception.Message);
            }
            CreateNew();
        }
        public bool TryLoadAnySave(out UdRoot<GameUserData> result)
        {
            return TryLoadFromFile(savePath, out result, out _) ||
                   TryLoadFromFile(tempPath, out result, out _) ||
                   TryLoadFromFile(backupPath, out result, out _);
        }
        private bool TryLoadFromFile(string path, out UdRoot<GameUserData> result, out Exception exception)
        {
            result = null;
            exception = null;
            try
            {
                if (!File.Exists(path)) return false;
                var bytes = File.ReadAllBytes(path);
                if (bytes.Length == 0)
                {
                    exception = new InvalidDataException("File is empty");
                    return false;
                }
                Debug.Log(string.Format("[GameDataService] Loading {0} bytes from {1}", bytes.Length, path));
                result = UdRoot.FromMemory(args => new GameUserData(args), bytes);
                Debug.Log("[GameDataService] Load successful");
                return true;
            }
            catch (Exception error) { exception = error; return false; }
        }
        private void CreateNew()
        {
            root = UdRoot.Create(args => new GameUserData(args));
            Debug.Log("[GameDataService] Created new game data");
        }
        public void SerializeData(ArrayBufferWriter<byte> buffer) => UdRoot.Serialize(root, buffer);
        private void SaveToFileInternal(GameDataSaveReason reason)
        {
            try
            {
                CancelBackgroundSave();
                lock (saveFileLock)
                {
                    using (UdRoot.ArrayBufferWriterPool.Get(out var buffer))
                    {
                        SerializeData(buffer);
                        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                        {
                            stream.Write(buffer.WrittenSpan);
                            stream.Flush(true);
                        }
                    }
                    if (File.Exists(savePath)) File.Delete(savePath);
                    File.Move(tempPath, savePath);
                }
                m_OnAfterSave?.Invoke(reason);
            }
            catch (Exception exception) { Debug.LogError("[GameDataService] Error saving: " + exception.Message); }
        }
        private async UniTask SaveToFileInternalInBackgroundAsync()
        {
            CancelBackgroundSave();
            saveTcs = new CancellationTokenSource();
            var token = saveTcs.Token;
            using (UdRoot.ArrayBufferWriterPool.Get(out var buffer))
            {
                var saved = false;
                try
                {
                    SerializeData(buffer);
                    await UniTask.SwitchToThreadPool();
                    lock (saveFileLock)
                    {
                        if (token.IsCancellationRequested) return;
                        using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                        {
                            stream.Write(buffer.WrittenSpan);
                            stream.Flush(true);
                        }
                        if (token.IsCancellationRequested) return;
                        if (File.Exists(savePath)) File.Delete(savePath);
                        File.Move(tempPath, savePath);
                        saved = true;
                    }
                }
                catch (Exception exception) { Debug.LogError("[GameDataService] Error saving: " + exception.Message); }
                finally { await UniTask.SwitchToMainThread(); }
                if (saved) m_OnAfterSave?.Invoke(GameDataSaveReason.Auto);
            }
        }
        private void CancelBackgroundSave()
        {
            if (saveTcs == null) return;
            saveTcs.Cancel();
            saveTcs.Dispose();
            saveTcs = null;
        }
        private void CreateBackup()
        {
            if (!File.Exists(savePath)) return;
            if (File.Exists(backupPath)) File.Delete(backupPath);
            File.Copy(savePath, backupPath);
        }
        private bool IsMigrationComplete() => PlayerPrefs.GetInt(MIGRATION_COMPLETE_KEY, 0) == 1 || File.Exists(savePath);
        private void MarkMigrationComplete()
        {
            PlayerPrefs.SetInt(MIGRATION_COMPLETE_KEY, 1);
            PlayerPrefs.Save();
        }
        private void MigrateFromLegacy()
        {
            try
            {
                Debug.Log("[GameDataService] Starting migration from PlayerPrefs...");
                new LegacyDataMigrator().Migrate(Data);
                ForceSave();
                MarkMigrationComplete();
                Debug.Log("[GameDataService] Migration completed successfully");
            }
            catch (Exception exception) { Debug.LogError("[GameDataService] Migration failed: " + exception.Message + "\n" + exception.StackTrace); }
        }
    }
}
