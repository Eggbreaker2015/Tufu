using System;
using System.Collections.Generic;
using System.Threading;
using Codebase.Analytics;
using Codebase.Core;
using Cysharp.Threading.Tasks;
using UniMob;
using UniMob.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace CodeBase.Audio
{
    public class AudioPlayer : LifetimeMonoBehaviour, IAudioPlayer
    {
        [SerializeField] private SoundsConfig soundsConfig;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioMixer mixer;
        [SerializeField] private MusicPlayer musicPlayer;
        [Inject] private UserModel userModel;
        private bool soundFxEnabled;
        private bool soundMusicEnabled;
        private const string SOUND_FX_ENABLED_PREF = "sound_volume";
        private const string SOUND_MUSIC_ENABLED_PREF = "music_volume";
        private const int audioSourcePoolSize = 10;
        private const int worldAudioPoolSize = 8;
        private List<AudioSource> audioSourcePool;
        private Queue<AudioSource> availableAudioSources;
        private Queue<WorldAudioSource> availableWorldAudioSources;
        // Original ctor 0x14a8444.
        private readonly Dictionary<string, float> lastPlayedTimes = new Dictionary<string, float>();
        private const float minDelayBetweenSounds = .1f;
        private MutableAtom<int> version;
        private CancellationTokenSource musicFadeCts;
        private ComputedAtom<float> __MusicVolume;
        private ComputedAtom<float> __SoundVolume;
        private ComputedAtom<bool> __SFXEnabled;
        private ComputedAtom<bool> __MusicEnabled;

        public float MusicVolume
        {
            get {
                // Native 0x14a6af4.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__MusicVolume == null) __MusicVolume = CodeGenAtom.CreatePooled(this, null, () => MusicVolume, false);
                return __MusicVolume.DirectEvaluate() ? userModel.MusicVolume.Value : __MusicVolume.Value;
            }
        }
        public float SoundVolume
        {
            get {
                // Native 0x14a6c50.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__SoundVolume == null) __SoundVolume = CodeGenAtom.CreatePooled(this, null, () => SoundVolume, false);
                return __SoundVolume.DirectEvaluate() ? userModel.SoundVolume.Value : __SoundVolume.Value;
            }
        }
        public bool SFXEnabled
        {
            get {
                // Native 0x14a6dac.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__SFXEnabled == null) __SFXEnabled = CodeGenAtom.CreatePooled(this, null, () => SFXEnabled, false);
                return __SFXEnabled.DirectEvaluate() ? SoundVolume > 0f : __SFXEnabled.Value;
            }
        }
        public bool MusicEnabled
        {
            get {
                // Native 0x14a6ed8.
                LifetimeScopeExtension.ThrowIfDisposed(this);
                if (__MusicEnabled == null) __MusicEnabled = CodeGenAtom.CreatePooled(this, null, () => MusicEnabled, false);
                return __MusicEnabled.DirectEvaluate() ? MusicVolume > 0f : __MusicEnabled.Value;
            }
        }
        private void Awake() { GameContext.Set<IAudioPlayer>(this); } // 0x14a7004
        protected override void Start()
        {
            // 0x14a7074, original reaction delegates 0x14a84cc/14a853c.
            base.Start();
            version = Atom.Value(Lifetime, 0);
            Atom.Reaction(Lifetime, () => SetMusicVolume(userModel.MusicVolume.Value));
            Atom.Reaction(Lifetime, () => SetSfxVolume(userModel.SoundVolume.Value));
            InitPool();
            InitWorldAudioPool();
        }
        private void SetSfxVolume(float value)
        {
            // 0x14a74a0; ASM retains fmaxnm(value, original float32 .0001) before log10f.
            mixer.SetFloat(SOUND_FX_ENABLED_PREF, Mathf.Log10(float.IsNaN(value) ? .0001f : Mathf.Max(value, .0001f)) * 20f);
        }
        private void SetMusicVolume(float value)
        {
            // 0x14a751c, same original decibel conversion.
            mixer.SetFloat(SOUND_MUSIC_ENABLED_PREF, Mathf.Log10(float.IsNaN(value) ? .0001f : Mathf.Max(value, .0001f)) * 20f);
        }
        private void InitPool()
        {
            // 0x14a71dc: copy the authored source properties, retaining ten fixed interruptible slots.
            audioSourcePool = new List<AudioSource>();
            availableAudioSources = new Queue<AudioSource>();
            for (int i = 0; i < audioSourcePoolSize; i++) {
                var source = gameObject.AddComponent<AudioSource>();
                source.volume = audioSource.volume;
                source.pitch = audioSource.pitch;
                source.spatialBlend = audioSource.spatialBlend;
                source.playOnAwake = false;
                source.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
                audioSourcePool.Add(source);
                availableAudioSources.Enqueue(source);
            }
        }
        private void InitWorldAudioPool()
        {
            // 0x14a73dc: world sources may subsequently grow beyond the initial eight.
            availableWorldAudioSources = new Queue<WorldAudioSource>();
            for (int i = 0; i < worldAudioPoolSize; i++) availableWorldAudioSources.Enqueue(CreateWorldAudioSource());
        }
        private WorldAudioSource CreateWorldAudioSource()
        {
            // 0x14a7598: deliberately root-level GameObjects, as in the original.
            var go = new GameObject("WorldAudioSource");
            var source = go.AddComponent<WorldAudioSource>();
            go.SetActive(false);
            source.Initialize(audioSource.outputAudioMixerGroup);
            return source;
        }
        public void PlayAtPosition(string sound, Vector3 position, float startDelay = 0f)
        {
            // 0x14a7768, return closure 0x14a85ac; world sounds do not use the per-key throttle.
            if (!soundsConfig.TryGetValue(sound, out var entry)) { Debug.LogError("Unknown sound " + sound + "!"); return; }
            var source = availableWorldAudioSources.Count > 0 ? availableWorldAudioSources.Dequeue() : CreateWorldAudioSource();
            AudioClip clip = entry.Clips[UnityEngine.Random.Range(0, entry.Clips.Length)];
            source.transform.position = position;
            source.gameObject.SetActive(true);
            source.Play(clip, entry.Volume, () => ReturnWorldAudioToPool(source), startDelay);
        }
        private void ReturnWorldAudioToPool(WorldAudioSource source)
        {
            // 0x14a7a44.
            source.gameObject.SetActive(false);
            availableWorldAudioSources.Enqueue(source);
        }
        public void PlaySoundOnce(string sound)
        {
            // 0x14a7abc: scaled-time per-key minimum interval is exactly .1f.
            if (!soundsConfig.TryGetValue(sound, out var entry)) { Debug.LogError("Unknown sound " + sound + "!"); return; }
            if (lastPlayedTimes.TryGetValue(sound, out float last) && Time.time - last < minDelayBetweenSounds) return;
            PlayClipOnce(entry.Clips[UnityEngine.Random.Range(0, entry.Clips.Length)], entry.Volume);
            lastPlayedTimes[sound] = Time.time;
        }
        public void PlayClipOnce(AudioClip clip, float volumeScale = 1f)
        {
            // 0x14a7c70: the source volume is intentionally multiplied again by PlayOneShot.
            if (clip == null) return;
            audioSource.PlayOneShot(clip, audioSource.volume * volumeScale);
        }
        public SoundHandle PlayClipOnceInterruptible(AudioClip clip, float volumeScale = 1f)
        {
            // 0x14a7d28, release closure 0x14a85c8. Handles release slots on Stop, not natural completion.
            if (clip == null) return null;
            if (availableAudioSources.Count == 0) { Debug.LogWarning("No available AudioSource in pool!"); return null; }
            var source = availableAudioSources.Dequeue();
            var cts = new CancellationTokenSource();
            PlayClipAsync(source, clip, volumeScale, cts.Token).Forget();
            return new SoundHandle(source, cts, () => availableAudioSources.Enqueue(source));
        }
        public SoundHandle PlaySoundOnceInterruptible(string sound)
        {
            // 0x14a80a0/14a8624: config lookup precedes pool exhaustion and random selection.
            if (!soundsConfig.TryGetValue(sound, out var entry)) { Debug.LogError("Unknown sound " + sound + "!"); return null; }
            if (availableAudioSources.Count == 0) { Debug.LogWarning("No available AudioSource in pool!"); return null; }
            var clip = entry.Clips[UnityEngine.Random.Range(0, entry.Clips.Length)];
            var source = availableAudioSources.Dequeue();
            var cts = new CancellationTokenSource();
            PlayClipAsync(source, clip, entry.Volume, cts.Token).Forget();
            return new SoundHandle(source, cts, () => availableAudioSources.Enqueue(source));
        }
        private async UniTaskVoid PlayClipAsync(AudioSource audioSource, AudioClip clip, float volumeScale, CancellationToken cancellationToken)
        {
            // 0x14a7f70 + MoveNext 0x14a8ef4; cancellation is swallowed but finally always stops/clears.
            audioSource.PlayOneShot(clip, audioSource.volume * volumeScale);
            try { await UniTask.Delay((int)(clip.length * 1000f), cancellationToken: cancellationToken); }
            catch (OperationCanceledException) { }
            finally { if (audioSource.isPlaying) audioSource.Stop(); audioSource.clip = null; }
        }
        public async UniTask FadeMusic(bool mute)
        {
            // 0x14a8394 + MoveNext 0x14a8680: mute .5s / restore 1.5s, cancel the previous request.
            if (musicPlayer == null) { Debug.LogWarning("MusicPlayer not found on scene!"); return; }
            musicFadeCts?.Cancel();
            musicFadeCts = new CancellationTokenSource();
            try { await musicPlayer.FadeMusicVolume(mute ? 0f : 1f, mute ? .5f : 1.5f, musicFadeCts.Token); }
            catch (OperationCanceledException) { }
        }
    }
}
