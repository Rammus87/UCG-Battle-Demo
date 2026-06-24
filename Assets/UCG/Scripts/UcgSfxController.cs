using UnityEngine;

namespace UCG
{
    [DisallowMultipleComponent]
    public class UcgSfxController : MonoBehaviour
    {
        [Header("Audio Source")]
        [SerializeField] AudioSource audioSource;
        [Range(0f, 1f)] public float sfxVolume = 1f;

        [Header("Clips")]
        public AudioClip cardTapClip;
        public AudioClip cardPlaceClip;
        public AudioClip upgradeClip;
        public AudioClip judgementWinClip;
        public AudioClip judgementLoseClip;
        public AudioClip tutorialCompleteClip;

        void Awake()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            ConfigureAudioSource();
        }

        public void PlayCardTap()
        {
            PlayClip(cardTapClip);
        }

        public void PlayCardPlace()
        {
            PlayClip(cardPlaceClip);
        }

        public void PlayUpgrade()
        {
            PlayClip(upgradeClip);
        }

        public void PlayJudgementWin()
        {
            PlayClip(judgementWinClip);
        }

        public void PlayJudgementLose()
        {
            PlayClip(judgementLoseClip);
        }

        public void PlayTutorialComplete()
        {
            PlayClip(tutorialCompleteClip);
        }

        void PlayClip(AudioClip clip)
        {
            if (clip == null) return;

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            ConfigureAudioSource();
            audioSource.PlayOneShot(clip, sfxVolume);
        }

        void ConfigureAudioSource()
        {
            if (audioSource == null) return;

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
    }
}
