using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// BGM 및 SFX 사운드 재생 관리 매니저 (sound_manager.py 대응)
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [Header("오디오 클립")]
        [SerializeField] private AudioClip shootClip;
        [SerializeField] private AudioClip explosionClip;
        [SerializeField] private AudioClip hitClip;

        private AudioSource sfxSource;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            // 폭발음 클립이 없을 경우 절차적 신스 폭발음 생성
            if (explosionClip == null)
            {
                explosionClip = GenerateProceduralExplosionClip();
            }
        }

        public void PlayShootSound()
        {
            if (shootClip != null)
            {
                sfxSource.PlayOneShot(shootClip, 0.7f);
            }
        }

        public void PlayExplosionSound()
        {
            if (explosionClip != null)
            {
                sfxSource.PlayOneShot(explosionClip, 0.85f);
            }
        }

        public void PlayHitSound()
        {
            if (hitClip != null)
            {
                sfxSource.PlayOneShot(hitClip, 0.8f);
            }
            else
            {
                PlayExplosionSound();
            }
        }

        private AudioClip GenerateProceduralExplosionClip()
        {
            int sampleRate = 44100;
            float duration = 0.35f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float noise = (Random.value * 2f - 1f);
                float decay = Mathf.Exp(-t * 8f);
                samples[i] = noise * decay;
            }

            AudioClip clip = AudioClip.Create("SynthExplosion", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
