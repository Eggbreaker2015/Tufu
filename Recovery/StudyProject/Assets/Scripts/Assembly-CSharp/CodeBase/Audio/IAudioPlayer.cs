using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CodeBase.Audio
{
	public interface IAudioPlayer
	{
		bool MusicEnabled { get; }

		bool SFXEnabled { get; }

		void PlaySoundOnce(string sound);

		void PlayClipOnce(AudioClip clip, float volumeScale = 1f);

		SoundHandle PlayClipOnceInterruptible(AudioClip clip, float volumeScale = 1f);

		SoundHandle PlaySoundOnceInterruptible(string sound);

		void PlayAtPosition(string sound, Vector3 position, float startDelay = 0f);

		UniTask FadeMusic(bool mute);
	}
}
