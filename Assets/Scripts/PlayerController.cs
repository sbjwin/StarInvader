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
        [SerializeField] private int maxConcurrentBullets = GameConstants.PLAYER_MAX_BULLETS;

        [Header("라이프 및 무적 설정")]
        [SerializeField] private int maxLives = GameConstants.PLAYER_MAX_LIVES;
        [SerializeField] private float invincibleDuration = GameConstants.PLAYER_INVINCIBLE_DURATION;

        private int currentLives;
        private bool isInvincible = false;
        private float lastShootTime = -10f;
        private SpriteRenderer spriteRenderer;

        public event Action<int> OnLivesChanged;
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

            OnLivesChanged?.Invoke(currentLives);
        }

        public void ResetPlayer()
        {
            currentLives = maxLives;
            isInvincible = false;
            if (spriteRenderer != null) spriteRenderer.enabled = true;
            gameObject.SetActive(true);
            transform.position = new Vector3(0, GameConstants.PLAYER_START_Y, 0);
            OnLivesChanged?.Invoke(currentLives);
        }

        private void Update()
        {
            HandleMovement();
            HandleShooting();
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
            if (InputHelper.IsShootingHeld() || InputHelper.IsActionPressed())
            {
                if (Time.time >= lastShootTime + shootCooldown)
                {
                    int activeBulletCount = 0;
                    Bullet[] existingBullets = FindObjectsByType<Bullet>(FindObjectsSortMode.None);
                    foreach (var b in existingBullets)
                    {
                        if (b != null && !b.IsEnemyBullet) activeBulletCount++;
                    }

                    if (activeBulletCount < maxConcurrentBullets)
                    {
                        Shoot();
                    }
                }
            }
        }

        private void Shoot()
        {
            lastShootTime = Time.time;
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + new Vector3(0, 0.45f, 0);

            if (bulletPrefab != null)
            {
                Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            }

            // 발사음 재생
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayShootSound();
            }
        }

        public void TakeDamage(int damage = 1)
        {
            if (isInvincible || currentLives <= 0) return;

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

            while (elapsed < invincibleDuration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = !spriteRenderer.enabled;
                }
                yield return new WaitForSeconds(flashInterval);
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
        public bool IsInvincible => isInvincible;
    }
}
