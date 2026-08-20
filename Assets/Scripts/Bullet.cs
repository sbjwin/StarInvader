using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 플레이어 및 적 탄환 기본 동작 및 충돌 스크립트 (bullet.py 대응)
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        [Header("탄환 속성")]
        [SerializeField] private bool isEnemyBullet = false;
        [SerializeField] private float speed = GameConstants.PLAYER_BULLET_SPEED;
        [SerializeField] private int damage = 1;

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
            // 플레이어 탄환이 적과 충돌했을 때
            if (!isEnemyBullet)
            {
                Enemy enemy = other.GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.TakeDamage(damage);
                    Destroy(gameObject); // 탄환 소멸
                }
            }
        }

        public void SetSpeed(float newSpeed)
        {
            speed = newSpeed;
        }

        public bool IsEnemyBullet => isEnemyBullet;
    }
}
