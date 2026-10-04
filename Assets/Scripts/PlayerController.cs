using System;
using System.Collections;
using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 플레이어 이동, 사격(SFX), 피격(CameraShake), 무적 제어
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("이동 설정")]
        [SerializeField] private float moveSpeed = GameConstants.PLAYER_SPEED;
        [SerializeField] private float minX = -GameConstants.SCREEN_WIDTH_HALF;
        [SerializeField] private float maxX = GameConstants.SCREEN_WIDTH_HALF;

        [Header("발사 설정")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float shootCooldown = GameConstants.PLAYER_SHOOT_COOLDOWN;
        [SerializeField] private int maxConcurrentBullets = 8; // 무기 레벨 확장에 따라 상향

        [Header("라이프 및 무적 설정")]
        [SerializeField] private int maxLives = GameConstants.PLAYER_MAX_LIVES;
        [SerializeField] private float invincibleDuration = GameConstants.PLAYER_INVINCIBLE_DURATION;

        [Header("무기 및 특수기 설정")]
        [SerializeField] private int weaponLevel = 1; // 1: 중앙 단발, 2: 좌우 듀얼, 3: 3방향 확산
        [SerializeField] private float spGauge = 0f;
        [SerializeField] private float maxSpGauge = 100f;
        [SerializeField] private bool hasShield = false;

        private int currentLives;
        private bool isInvincible = false;
        private bool isFiringSpecial = false;
        private float lastShootTime = -10f;
        private SpriteRenderer spriteRenderer;
        private GameObject shieldObject;

        public event Action<int> OnLivesChanged;
        public event Action<int> OnWeaponLevelChanged;
        public event Action<float> OnSpChanged;
        public event Action<bool> OnShieldChanged;
        public event Action OnPlayerDied;

        private void Awake()
        {
            currentLives = maxLives;
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (firePoint == null)
            {
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(transform);
                fp.transform.localPosition = new Vector3(0, 0.5f, 0);
                firePoint = fp.transform;
            }

            CreateShieldVisual();
            OnLivesChanged?.Invoke(currentLives);
            OnWeaponLevelChanged?.Invoke(weaponLevel);
            OnSpChanged?.Invoke(spGauge);
            OnShieldChanged?.Invoke(hasShield);
        }

        public void ResetPlayer()
        {
            currentLives = maxLives;
            weaponLevel = 1;
            spGauge = 0f;
            hasShield = false;
            isInvincible = false;
            isFiringSpecial = false;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            UpdateShieldVisual();
            gameObject.SetActive(true);
            transform.position = new Vector3(0, GameConstants.PLAYER_START_Y, 0);

            OnLivesChanged?.Invoke(currentLives);
            OnWeaponLevelChanged?.Invoke(weaponLevel);
            OnSpChanged?.Invoke(spGauge);
            OnShieldChanged?.Invoke(hasShield);
        }

        private void Update()
        {
            HandleMovement();
            HandleShooting();
            HandleSpecialWeapon();
        }

        private void HandleMovement()
        {
            float horizontalInput = InputHelper.GetHorizontalAxis();
            Vector3 position = transform.position;
            position.x += horizontalInput * moveSpeed * Time.deltaTime;
            position.x = Mathf.Clamp(position.x, minX, maxX);
            transform.position = position;
        }

        private void HandleShooting()
        {
            if (isFiringSpecial) return;

            if (InputHelper.IsShootingHeld() || InputHelper.IsActionPressed())
            {
                if (Time.time >= lastShootTime + shootCooldown)
                {
                    if (Bullet.ActivePlayerBulletCount < maxConcurrentBullets)
                    {
                        Shoot();
                    }
                }
            }
        }

        private void HandleSpecialWeapon()
        {
            if (InputHelper.IsSpecialWeaponPressed())
            {
                if (spGauge >= maxSpGauge && !isFiringSpecial)
                {
                    FireSpecialWeapon();
                }
            }
        }

        private void Shoot()
        {
            lastShootTime = Time.time;
            Vector3 basePos = firePoint != null ? firePoint.position : transform.position + new Vector3(0, 0.45f, 0);

            if (bulletPrefab != null)
            {
                if (weaponLevel == 1)
                {
                    // Lv.1: 정중앙 단발
                    SpawnBullet(basePos, Vector2.up);
                }
                else if (weaponLevel == 2)
                {
                    // Lv.2: 정중앙 기준 좌/우 오프셋 2발 평행 발사
                    SpawnBullet(basePos + new Vector3(-0.25f, 0, 0), Vector2.up);
                    SpawnBullet(basePos + new Vector3(0.25f, 0, 0), Vector2.up);
                }
                else
                {
                    // Lv.3: 정중앙 1발 + 좌/우 각도(±14°) 3방향 확산 발사
                    SpawnBullet(basePos, Vector2.up);
                    SpawnBullet(basePos + new Vector3(-0.2f, 0, 0), (Quaternion.Euler(0, 0, 14f) * Vector2.up));
                    SpawnBullet(basePos + new Vector3(0.2f, 0, 0), (Quaternion.Euler(0, 0, -14f) * Vector2.up));
                }
            }

            // 발사음 재생
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayShootSound();
            }
        }

        private void SpawnBullet(Vector3 pos, Vector2 direction)
        {
            GameObject bObj = Instantiate(bulletPrefab, pos, Quaternion.identity);
            Bullet bulletComp = bObj.GetComponent<Bullet>();
            if (bulletComp != null)
            {
                bulletComp.Initialize(direction, GameConstants.PLAYER_BULLET_SPEED, false);
            }
        }

        public void FireSpecialWeapon()
        {
            StartCoroutine(HyperBeamRoutine());
        }

        private IEnumerator HyperBeamRoutine()
        {
            isFiringSpecial = true;
            spGauge = 0f;
            OnSpChanged?.Invoke(spGauge);

            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.TriggerShake(1.5f, 0.22f);
            }
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBonusSound();
            }

            float duration = 1.4f;
            float interval = 0.08f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                Vector3 basePos = firePoint != null ? firePoint.position : transform.position + new Vector3(0, 0.5f, 0);

                // 관통 하이퍼 빔 발사 (좌/중/우 3줄기 고속 방출)
                SpawnSpecialBeamBullet(basePos + new Vector3(-0.16f, 0, 0));
                SpawnSpecialBeamBullet(basePos);
                SpawnSpecialBeamBullet(basePos + new Vector3(0.16f, 0, 0));

                elapsed += interval;
                yield return new WaitForSeconds(interval);
            }

            isFiringSpecial = false;
        }

        private void SpawnSpecialBeamBullet(Vector3 pos)
        {
            if (bulletPrefab == null) return;
            GameObject bObj = Instantiate(bulletPrefab, pos, Quaternion.identity);
            Bullet bComp = bObj.GetComponent<Bullet>();
            if (bComp != null)
            {
                bComp.Initialize(Vector2.up, GameConstants.PLAYER_BULLET_SPEED * 1.5f, false, true, 2);
            }

            // 빔 연출을 위해 스프라이트 스케일 및 색상 강화
            bObj.transform.localScale = new Vector3(1.6f, 2.2f, 1f);
            SpriteRenderer sr = bObj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.color = new Color(0.3f, 0.9f, 1f, 1f);
            }
        }

        public void UpgradeWeapon()
        {
            weaponLevel = Mathf.Min(3, weaponLevel + 1);
            OnWeaponLevelChanged?.Invoke(weaponLevel);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayComboSound(1.3f);
        }

        public void AddSp(float amount)
        {
            spGauge = Mathf.Clamp(spGauge + amount, 0f, maxSpGauge);
            OnSpChanged?.Invoke(spGauge);
        }

        public void ActivateShield()
        {
            hasShield = true;
            UpdateShieldVisual();
            OnShieldChanged?.Invoke(true);
            if (SoundManager.Instance != null) SoundManager.Instance.PlayComboSound(1.1f);
        }

        private void CreateShieldVisual()
        {
            if (shieldObject == null)
            {
                shieldObject = new GameObject("ShieldVisual");
                shieldObject.transform.SetParent(transform);
                shieldObject.transform.localPosition = Vector3.zero;
                shieldObject.transform.localScale = new Vector3(1.4f, 1.4f, 1f);

                SpriteRenderer sr = shieldObject.AddComponent<SpriteRenderer>();
                // 원형 실드 비주얼 (절차적 또는 기존 스프라이트 활용)
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    sr.sprite = spriteRenderer.sprite;
                }
                sr.color = new Color(0.2f, 0.7f, 1f, 0.45f);
                shieldObject.SetActive(hasShield);
            }
        }

        private void UpdateShieldVisual()
        {
            if (shieldObject != null)
            {
                shieldObject.SetActive(hasShield);
            }
        }

        public void TakeDamage(int damage = 1)
        {
            if (isInvincible || currentLives <= 0) return;

            // 실드 보유 시 1회 완전 방어
            if (hasShield)
            {
                hasShield = false;
                UpdateShieldVisual();
                OnShieldChanged?.Invoke(false);

                if (SoundManager.Instance != null) SoundManager.Instance.PlayHitSound();
                if (CameraShake.Instance != null) CameraShake.Instance.TriggerShake(0.2f, 0.15f);
                StartCoroutine(InvincibilityRoutine());
                return;
            }

            currentLives -= damage;
            OnLivesChanged?.Invoke(currentLives);

            // 카메라 셰이크 연출
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.TriggerShake(0.25f, 0.2f);
            }

            // 피격 사운드
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayHitSound();
            }

            if (currentLives <= 0)
            {
                Die();
            }
            else
            {
                StartCoroutine(InvincibilityRoutine());
            }
        }

        private IEnumerator InvincibilityRoutine()
        {
            isInvincible = true;
            float elapsed = 0f;
            float flashInterval = 0.1f;
            WaitForSeconds waitInterval = new WaitForSeconds(flashInterval);

            while (elapsed < invincibleDuration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                }
                yield return waitInterval;
                elapsed += flashInterval;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
            }
            isInvincible = false;
        }

        private void Die()
        {
            OnPlayerDied?.Invoke();
            gameObject.SetActive(false);
        }

        public int CurrentLives => currentLives;
        public int WeaponLevel => weaponLevel;
        public float SpGauge => spGauge;
        public float MaxSpGauge => maxSpGauge;
        public bool HasShield => hasShield;
        public bool IsInvincible => isInvincible;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void SetGodMode(bool enable)
        {
            isInvincible = enable;
        }

        public void AddLive(int count = 1)
        {
            currentLives = Mathf.Clamp(currentLives + count, 0, 99);
            OnLivesChanged?.Invoke(currentLives);
        }

        public void SetWeaponLevel(int level)
        {
            weaponLevel = Mathf.Clamp(level, 1, 3);
            OnWeaponLevelChanged?.Invoke(weaponLevel);
        }

        public void MaximizeSp()
        {
            spGauge = maxSpGauge;
            OnSpChanged?.Invoke(spGauge);
        }
#endif
    }
}

