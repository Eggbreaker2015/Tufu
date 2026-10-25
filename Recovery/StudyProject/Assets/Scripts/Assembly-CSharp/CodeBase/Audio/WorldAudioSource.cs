using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

namespace CodeBase.Audio
{
    public class WorldAudioSource : MonoBehaviour
    {
        private AudioSource audioSource;
        public void Initialize(AudioMixerGroup mixerGroup)
        {
            // Original 0x14a766c; despite the type name, the authored runtime explicitly uses blend zero.
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0f;
            audioSource.outputAudioMixerGroup = mixerGroup;
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 50f;
            audioSource.minDistance = 1f;
        }
        public void Play(AudioClip clip, float volumeScale, Action onComplete, float startDelay = 0f)
        {
            // Original 0x14a7a20.
            WaitAndReturn(clip, volumeScale, startDelay, onComplete).Forget();
        }
        private async UniTaskVoid WaitAndReturn(AudioClip clip, float volumeScale, float startDelay, Action onComplete)
        {
            // Original 0x14aade4/14aaec8; original destroy token cancels both scaled-time waits.
            try {
                if (startDelay > 0f) await UniTask.Delay((int)(startDelay * 1000f), cancellationToken: destroyCancellationToken);
                audioSource.clip = clip;
                audioSource.volume = volumeScale;
                audioSource.Play();
                await UniTask.Delay((int)(clip.length * 1000f), cancellationToken: destroyCancellationToken);
                onComplete?.Invoke();
            } catch (OperationCanceledException) { }
        }
    }
}
