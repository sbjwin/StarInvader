using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 플레이어 및 적 탄환 기본 동작 및 충돌 판정 (bullet.py 대응)
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        [Header("탄환 속성")]
        [SerializeField] private bool isEnemyBullet = false;
        [SerializeField] private float speed = GameConstants.PLAYER_BULLET_SPEED;
        [SerializeField] private int damage = 1;

        public static int ActivePlayerBulletCount { get; private set; } = 0;

        private void Awake()
        {
            if (!isEnemyBullet)
            {
                ActivePlayerBulletCount++;
            }
        }

        private void OnDestroy()
        {
            if (!isEnemyBullet)
            {
                ActivePlayerBulletCount = Mathf.Max(0, ActivePlayerBulletCount - 1);
            }
        }

        private void Update()
        {
            // 이동 방향 (플레이어: 위쪽 +Y, 적: 아래쪽 -Y)
            Vector3 direction = isEnemyBullet ? Vector3.down : Vector3.up;
            transform.position += direction * (speed * Time.deltaTime);

            // 화면 밖으로 나가면 파괴
            if (transform.position.y > GameConstants.SCREEN_HEIGHT_HALF + 1.0f ||
                transform.position.y < -GameConstants.SCREEN_HEIGHT_HALF - 1.0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isEnemyBullet)
            {
                // [플레이어 탄환] -> 일반 적 피격
                Enemy enemy = other.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    Destroy(gameObject);
                    return;
                }

                // [플레이어 탄환] -> 보너스 UFO 피격
                BonusUfo ufo = other.GetComponent<BonusUfo>();
                if (ufo != null)
                {
                    ufo.TakeDamage(damage);
                    Destroy(gameObject);
                    return;
                }
            }
            else
            {
                // [적 탄환] -> 플레이어 피격
                PlayerController player = other.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(damage);
                    Destroy(gameObject);
                }
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        public void SetEnemyBullet(bool enemyBullet)
        {
            isEnemyBullet = enemyBullet;
        }

        public bool IsEnemyBullet => isEnemyBullet;
    }
}
