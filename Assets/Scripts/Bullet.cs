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
        [SerializeField] private bool isPiercing = false;

        private Vector2 moveDirection = Vector2.up;

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

        public void Initialize(Vector2 direction, float bulletSpeed, bool enemy, bool piercing = false, int dmg = 1)
        {
            isEnemyBullet = enemy;
            speed = bulletSpeed;
            isPiercing = piercing;
            damage = dmg;
            moveDirection = direction.normalized;

            // 탄환 이동 방향에 맞춰 회전 정렬
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        private void Update()
        {
            // 이동 방향 (지정된 moveDirection 또는 기본 수직 방향)
            Vector3 dir = (Vector3)moveDirection;
            transform.position += dir * (speed * Time.deltaTime);

            // 화면 밖으로 나가면 파괴 (X축 및 Y축)
            if (transform.position.y > GameConstants.SCREEN_HEIGHT_HALF + 1.2f ||
                transform.position.y < -GameConstants.SCREEN_HEIGHT_HALF - 1.2f ||
                transform.position.x > GameConstants.SCREEN_WIDTH_HALF + 1.5f ||
                transform.position.x < -GameConstants.SCREEN_WIDTH_HALF - 1.5f)
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
                    if (!isPiercing)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }

                // [플레이어 탄환] -> 보너스 UFO 피격
                BonusUfo ufo = other.GetComponent<BonusUfo>();
                if (ufo != null)
                {
                    ufo.TakeDamage(damage);
                    if (!isPiercing)
                    {
                        Destroy(gameObject);
                        return;
                    }
                }

                // [관통 특수 탄환/빔] -> 적 탄환 소멸
                if (isPiercing)
                {
                    Bullet enemyBullet = other.GetComponent<Bullet>();
                    if (enemyBullet != null && enemyBullet.IsEnemyBullet)
                    {
                        Destroy(enemyBullet.gameObject);
                    }
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
            moveDirection = enemyBullet ? Vector2.down : Vector2.up;
        }

        public void SetDirection(Vector2 dir)
        {
            moveDirection = dir.normalized;
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        public bool IsEnemyBullet => isEnemyBullet;
        public bool IsPiercing => isPiercing;
    }
}
