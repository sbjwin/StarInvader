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
        [SerializeField] private AudioClip ufoClip;
        [SerializeField] private AudioClip bonusClip;
        [SerializeField] private AudioClip comboClip;

        private AudioSource sfxSource;
        private AudioSource ufoSource;

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
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            // UFO 전용 루프 오디오 소스
            ufoSource = gameObject.AddComponent<AudioSource>();
            ufoSource.playOnAwake = false;
            ufoSource.loop = true;

            // 오디오 클립이 없을 경우 절차적 신스 오디오 자동 생성
            if (shootClip == null) shootClip = GenerateProceduralPlayerLaserClip();
            if (enemyShootClip == null) enemyShootClip = GenerateProceduralEnemyLaserClip();
            if (explosionClip == null) explosionClip = GenerateProceduralExplosionClip();
            if (hitClip == null) hitClip = GenerateProceduralHitClip();
            if (ufoClip == null) ufoClip = GenerateProceduralUfoClip();
            if (bonusClip == null) bonusClip = GenerateProceduralBonusClip();
            if (comboClip == null) comboClip = GenerateProceduralComboClip();
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

        public void PlayUfoSound()
        {
            if (ufoClip == null) ufoClip = GenerateProceduralUfoClip();
            if (ufoSource != null && ufoClip != null && !ufoSource.isPlaying)
            {
                ufoSource.clip = ufoClip;
                ufoSource.volume = 0.45f;
                ufoSource.Play();
            }
        }

        public void StopUfoSound()
        {
            if (ufoSource != null && ufoSource.isPlaying)
            {
                ufoSource.Stop();
            }
        }

        public void PlayBonusSound()
        {
            if (bonusClip == null) bonusClip = GenerateProceduralBonusClip();
            if (sfxSource != null && bonusClip != null)
            {
                sfxSource.PlayOneShot(bonusClip, 0.95f);
            }
        }

        public void PlayComboSound(float pitch = 1.0f)
        {
            if (comboClip == null) comboClip = GenerateProceduralComboClip();
            if (sfxSource != null && comboClip != null)
            {
                sfxSource.pitch = Mathf.Clamp(pitch, 0.8f, 2.0f);
                sfxSource.PlayOneShot(comboClip, 0.7f);
                sfxSource.pitch = 1.0f;
            }
        }

        private AudioClip GenerateProceduralPlayerLaserClip()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float freq = Mathf.Lerp(950f, 200f, t * t);
                float phase = 2f * Mathf.PI * freq * (i / (float)sampleRate);
                float wave = Mathf.Sign(Mathf.Sin(phase)) * 0.6f + (Mathf.Sin(phase * 0.5f)) * 0.4f;
                float envelope = Mathf.Pow(1f - t, 1.5f);
                samples[i] = wave * envelope * 0.65f;
            }

            AudioClip clip = AudioClip.Create("SynthPlayerLaser", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateProceduralEnemyLaserClip()
        {
            int sampleRate = 44100;
            float duration = 0.18f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
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

        /// <summary>
        /// 외계인 모함(UFO) 워블 사이렌 루프 사운드
        /// </summary>
        private AudioClip GenerateProceduralUfoClip()
        {
            int sampleRate = 44100;
            float duration = 0.4f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                // 350Hz ~ 550Hz 정현파 진동
                float freq = 450f + Mathf.Sin(2f * Mathf.PI * 5f * t) * 100f;
                float phase = 2f * Mathf.PI * freq * (i / (float)sampleRate);
                float wave = Mathf.Sign(Mathf.Sin(phase)) * 0.4f + Mathf.Sin(phase) * 0.3f;
                samples[i] = wave * 0.5f;
            }

            AudioClip clip = AudioClip.Create("SynthUfoSiren", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 보너스 UFO 격추 시 아르페지오 팡파레 사운드
        /// </summary>
        private AudioClip GenerateProceduralBonusClip()
        {
            int sampleRate = 44100;
            float duration = 0.45f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // 도(523Hz) - 미(659Hz) - 솔(784Hz) - 도(1046Hz) 아르페지오
            float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f };
            int noteLength = totalSamples / notes.Length;

            for (int i = 0; i < totalSamples; i++)
            {
                int noteIndex = Mathf.Min(i / noteLength, notes.Length - 1);
                float freq = notes[noteIndex];
                float tInNote = (float)(i % noteLength) / noteLength;
                float phase = 2f * Mathf.PI * freq * (i / (float)sampleRate);
                float wave = Mathf.Sin(phase) + Mathf.Sin(phase * 2f) * 0.3f;
                float env = Mathf.Pow(1f - tInNote, 1.2f);
                samples[i] = wave * env * 0.6f;
            }

            AudioClip clip = AudioClip.Create("SynthBonusFanfare", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// 콤보 획득 핑 사운드
        /// </summary>
        private AudioClip GenerateProceduralComboClip()
        {
            int sampleRate = 44100;
            float duration = 0.15f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                float freq = Mathf.Lerp(600f, 1200f, t);
                float phase = 2f * Mathf.PI * freq * (i / (float)sampleRate);
                float wave = Mathf.Sin(phase);
                float env = Mathf.Pow(1f - t, 2f);
                samples[i] = wave * env * 0.7f;
            }

            AudioClip clip = AudioClip.Create("SynthCombo", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
