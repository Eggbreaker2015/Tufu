using System;
using System.Threading;
using Codebase.BossTower;
using Codebase.Core;
using Codebase.Location;
using Codebase.Player.Models;
using Cysharp.Threading.Tasks;
using UniMob;
using UnityEngine;

namespace CodeBase.Audio
{
    public class MusicPlayer : LifetimeMonoBehaviour
    {
        [SerializeField] private AudioSource musicSource;
        private const float FadeDuration = 4f;
        private AudioClip currentClip;
        private CancellationTokenSource fadeCts;
        private bool destroying;
        private CancellationTokenSource muteOverrideCts;
        [Inject] private PlayerModel playerModel;
        [Inject] private IAudioPlayer audioPlayer;
        [Inject] private BossTowerModel bossTowerModel;
        private AudioClip targetClip;
        // Original ctor 0x14a9afc initializes both consecutive floats to one.
        private float musicTransitionMultiplier = 1f;
        private float musicVolumeMultiplier = 1f;
        protected override void Start()
        {
            // 0x14a96a4, reaction 0x14a9b0c.
            base.Start();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            Atom.Reaction(Lifetime, () => { var location = playerModel.CurrentLocation; var bossMusic = bossTowerModel.CurrentBossMusic; OnMusicSourceChanged(); });
        }
        private void OnMusicSourceChanged()
        {
            // 0x14a9774: the current boss clip takes priority; instant is a genuine BossTowerModel flag.
            var clip = bossTowerModel.CurrentBossMusic;
            targetClip = clip != null ? clip : SelectMusicClip(playerModel.CurrentLocation);
            if (targetClip == currentClip) return;
            if (bossTowerModel.BossMusicInstant) ChangeMusicInstant(); else ChangeMusic().Forget();
        }
        private void ChangeMusicInstant()
        {
            // Original0x14a98a4: instant latest clip at full transition multiplier.
            if (destroying || musicSource == null) return;
            CancelAndDispose(ref fadeCts);
            currentClip = targetClip;
            musicTransitionMultiplier = 1f;
            musicSource.clip = targetClip;
            musicSource.Play();
            UpdateMusicVolume();
        }
        private AudioClip SelectMusicClip(LocationConfig location) { return location.NewMusic; } // 0x14a9890
        private async UniTaskVoid ChangeMusic()
        {
            // Original four-second fade out/latest target/four-second fade in remains unchanged.
            if (destroying || musicSource == null) return;
            CancelAndDispose(ref fadeCts);
            var request = new CancellationTokenSource();
            fadeCts = request;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(request.Token, destroyCancellationToken);
            CancellationToken token = linked.Token;
            try
            {
                await FadeVolume(0f, FadeDuration, token);
                token.ThrowIfCancellationRequested();
                if (musicSource == null) return;
                currentClip = targetClip;
                musicSource.clip = targetClip;
                musicSource.Play();
                await FadeVolume(1f, FadeDuration, token);
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (ReferenceEquals(fadeCts, request)) fadeCts = null;
                request.Dispose();
            }
        }
        private void UpdateMusicVolume() {
            // An AudioSource can be destroyed before this component's OnDestroy during scene teardown.
            if (!destroying && musicSource != null)
                musicSource.volume = musicVolumeMultiplier * musicTransitionMultiplier;
        } // 0x14a99f4
        private async UniTask FadeVolume(float target, float duration = FadeDuration, CancellationToken token = default)
        {
            // Preserve original scaled deltaTime and Update-timing yields.
            float startMultiplier = musicTransitionMultiplier;
            float t = 0f;
            while (t < duration)
            {
                token.ThrowIfCancellationRequested();
                if (destroying || musicSource == null) return;
                t += Time.deltaTime;
                musicTransitionMultiplier = Mathf.Lerp(startMultiplier, target, t / duration);
                UpdateMusicVolume();
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            token.ThrowIfCancellationRequested();
            if (destroying || musicSource == null) return;
            musicTransitionMultiplier = target;
            UpdateMusicVolume();
        }
        public float GetMusicSourceVolume() {
            return !destroying && musicSource != null ? musicSource.volume : 0f;
        } // 0x14a9ae4
        public async UniTask FadeMusicVolume(float target, float duration, CancellationToken token = default)
        {
            // Keep independent override cancellation linked to the caller; also stop at component destruction.
            if (destroying || musicSource == null) return;
            CancelAndDispose(ref muteOverrideCts);
            var request = new CancellationTokenSource();
            muteOverrideCts = request;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, request.Token, destroyCancellationToken);
            CancellationToken linkedToken = linked.Token;
            try
            {
                float startMultiplier = musicVolumeMultiplier;
                float t = 0f;
                while (t < duration)
                {
                    linkedToken.ThrowIfCancellationRequested();
                    if (destroying || musicSource == null) return;
                    t += Time.deltaTime;
                    musicVolumeMultiplier = Mathf.Lerp(startMultiplier, target, t / duration);
                    UpdateMusicVolume();
                    await UniTask.Yield(PlayerLoopTiming.Update, linkedToken);
                }
                linkedToken.ThrowIfCancellationRequested();
                if (destroying || musicSource == null) return;
                musicVolumeMultiplier = target;
                UpdateMusicVolume();
            }
            finally
            {
                if (ReferenceEquals(muteOverrideCts, request)) muteOverrideCts = null;
                request.Dispose();
            }
        }
        // Local lifecycle compatibility: cancel before LifetimeMonoBehaviour disposes reactions.
        // Detach the field before Cancel because cancellation can resume an awaiting method synchronously.
        private static void CancelAndDispose(ref CancellationTokenSource source)
        {
            var previous = source;
            source = null;
            if (previous == null) return;
            try { previous.Cancel(); }
            finally { previous.Dispose(); }
        }

        protected override void OnDestroy()
        {
            destroying = true;
            try
            {
                CancelAndDispose(ref fadeCts);
                CancelAndDispose(ref muteOverrideCts);
            }
            finally { base.OnDestroy(); }
        }
    }
}
