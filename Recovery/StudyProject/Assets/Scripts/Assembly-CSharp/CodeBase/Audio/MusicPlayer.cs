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
            // 0x14a98a4.
            fadeCts?.Cancel();
            fadeCts = new CancellationTokenSource();
            currentClip = targetClip;
            musicTransitionMultiplier = 1f;
            musicSource.clip = targetClip;
            musicSource.Play();
            UpdateMusicVolume();
        }
        private AudioClip SelectMusicClip(LocationConfig location) { return location.NewMusic; } // 0x14a9890
        private async UniTaskVoid ChangeMusic()
        {
            // 0x14a9964/14a9b44: full four-second fade out, then latest target, then four-second fade in.
            fadeCts?.Cancel();
            fadeCts = new CancellationTokenSource();
            CancellationToken token = fadeCts.Token;
            try {
                await FadeVolume(0f, FadeDuration, token);
                currentClip = targetClip;
                musicSource.clip = targetClip;
                musicSource.Play();
                await FadeVolume(1f, FadeDuration, token);
            } catch (OperationCanceledException) { }
        }
        private void UpdateMusicVolume() { musicSource.volume = musicVolumeMultiplier * musicTransitionMultiplier; } // 0x14a99f4
        private async UniTask FadeVolume(float target, float duration = FadeDuration, CancellationToken token = default)
        {
            // 0x14a9a18/14aa7a4, original scaled deltaTime and Update-timing yields.
            float startMultiplier = musicTransitionMultiplier;
            float t = 0f;
            while (t < duration) {
                token.ThrowIfCancellationRequested();
                t += Time.deltaTime;
                musicTransitionMultiplier = Mathf.Lerp(startMultiplier, target, t / duration);
                UpdateMusicVolume();
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
            musicTransitionMultiplier = target;
            UpdateMusicVolume();
        }
        public float GetMusicSourceVolume() { return musicSource.volume; } // 0x14a9ae4
        public async UniTask FadeMusicVolume(float target, float duration, CancellationToken token = default)
        {
            // 0x14a8e1c/14aa228: independent override cancellation is linked to the caller.
            muteOverrideCts?.Cancel();
            muteOverrideCts = new CancellationTokenSource();
            CancellationToken linkedToken = CancellationTokenSource.CreateLinkedTokenSource(token, muteOverrideCts.Token).Token;
            float startMultiplier = musicVolumeMultiplier;
            float t = 0f;
            while (t < duration) {
                linkedToken.ThrowIfCancellationRequested();
                t += Time.deltaTime;
                musicVolumeMultiplier = Mathf.Lerp(startMultiplier, target, t / duration);
                UpdateMusicVolume();
                await UniTask.Yield(PlayerLoopTiming.Update, linkedToken);
            }
            musicVolumeMultiplier = target;
            UpdateMusicVolume();
        }
    }
}
