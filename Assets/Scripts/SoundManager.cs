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
        [SerializeField] private AudioClip enemyShootClip;
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

            sfxSource = gameObject.GetComponent<AudioSource>();
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.playOnAwake = false;

            // 오디오 클립이 없을 경우 절차적 신스 오디오 자동 생성
            if (shootClip == null) shootClip = GenerateProceduralPlayerLaserClip();
            if (enemyShootClip == null) enemyShootClip = GenerateProceduralEnemyLaserClip();
            if (explosionClip == null) explosionClip = GenerateProceduralExplosionClip();
            if (hitClip == null) hitClip = GenerateProceduralHitClip();
        }

        public void PlayShootSound()
        {
            if (shootClip == null) shootClip = GenerateProceduralPlayerLaserClip();
            if (sfxSource != null && shootClip != null)
            {
                sfxSource.PlayOneShot(shootClip, 0.75f);
            }
        }

        public void PlayEnemyShootSound()
        {
            if (enemyShootClip == null) enemyShootClip = GenerateProceduralEnemyLaserClip();
            if (sfxSource != null && enemyShootClip != null)
            {
                sfxSource.PlayOneShot(enemyShootClip, 0.55f);
            }
        }

        public void PlayExplosionSound()
        {
            if (explosionClip == null) explosionClip = GenerateProceduralExplosionClip();
            if (sfxSource != null && explosionClip != null)
            {
                sfxSource.PlayOneShot(explosionClip, 0.85f);
            }
        }

        public void PlayHitSound()
        {
            if (hitClip == null) hitClip = GenerateProceduralHitClip();
            if (sfxSource != null && hitClip != null)
            {
                sfxSource.PlayOneShot(hitClip, 0.8f);
            }
            else
            {
                PlayExplosionSound();
            }
        }

        /// <summary>
        /// 8비트 아케이드 스타일 플레이어 레이저 신스음 생성 (High to Low Frequency Sweep)
        /// </summary>
        private AudioClip GenerateProceduralPlayerLaserClip()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                // 950Hz -> 200Hz 주파수 하강 스윕
                float freq = Mathf.Lerp(950f, 200f, t * t);
                float phase = 2f * Mathf.PI * freq * (i / (float)sampleRate);
                // 사각파(Square) + 톱니파(Sawtooth) 블렌딩으로 레트로 아케이드 질감 연출
                float wave = Mathf.Sign(Mathf.Sin(phase)) * 0.6f + (Mathf.Sin(phase * 0.5f)) * 0.4f;
                float envelope = Mathf.Pow(1f - t, 1.5f);
                samples[i] = wave * envelope * 0.65f;
            }

            AudioClip clip = AudioClip.Create("SynthPlayerLaser", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 외계인 펄스 레이저 신스음 생성 (Low Pulsing Laser)
        /// </summary>
        private AudioClip GenerateProceduralEnemyLaserClip()
        {
            int sampleRate = 44100;
            float duration = 0.18f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                // 400Hz -> 120Hz 주파수 스윕
                float freq = Mathf.Lerp(450f, 120f, t);
                float phase = 2f * Mathf.PI * freq * (i / (float)sampleRate);
                float wave = Mathf.Sign(Mathf.Sin(phase));
                float envelope = Mathf.Pow(1f - t, 1.2f);
                samples[i] = wave * envelope * 0.45f;
            }

            AudioClip clip = AudioClip.Create("SynthEnemyLaser", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 레트로 신스 폭발음 생성 (Filtered White Noise Burst)
        /// </summary>
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
                float decay = Mathf.Exp(-t * 7.5f);
                samples[i] = noise * decay * 0.8f;
            }

            AudioClip clip = AudioClip.Create("SynthExplosion", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 피격 타격음 생성
        /// </summary>
        private AudioClip GenerateProceduralHitClip()
        {
            int sampleRate = 44100;
            float duration = 0.12f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float freq = Mathf.Lerp(300f, 60f, t);
                float phase = 2f * Mathf.PI * freq * (i / (float)sampleRate);
                float wave = Mathf.Sin(phase);
                float decay = Mathf.Exp(-t * 15f);
                samples[i] = wave * decay * 0.7f;
            }

            AudioClip clip = AudioClip.Create("SynthHit", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
