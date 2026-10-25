using System.Collections;
using UnityEngine;

namespace CodeBase.Audio
{
    public class AudioSourcePlay : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float delay;
        [SerializeField] private bool playOnEnable;
        [SerializeField] private bool playOnAwake;
        private void Awake()
        {
            // Native 0x14a945c.
            audioSource.playOnAwake = false;
            if (playOnAwake) StartCoroutine(PlayDelayed());
        }
        private void OnEnable()
        {
            // Native 0x14a9514; both enabled flags deliberately schedule independent coroutines.
            audioSource.playOnAwake = false;
            if (playOnEnable) StartCoroutine(PlayDelayed());
        }
        private IEnumerator PlayDelayed()
        {
            // Native 0x14a94c8/14a9594, including a yield even when delay is zero.
            yield return new WaitForSeconds(delay);
            audioSource.Play();
        }
    }
}
