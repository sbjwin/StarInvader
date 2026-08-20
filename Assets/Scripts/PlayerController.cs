using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 플레이어 이동 및 기본 사격 컨트롤러 (player.py 대응)
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

        private float lastShootTime = -10f;

        private void Start()
        {
            // 발사 위치가 지정되지 않았으면 플레이어 위치 기준으로 자동 생성
            if (firePoint == null)
            {
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(transform);
                fp.transform.localPosition = new Vector3(0, 0.5f, 0);
                firePoint = fp.transform;
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleShooting();
        }

        private void HandleMovement()
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal"); // A/D 또는 좌우 화살표
            Vector3 position = transform.position;
            position.x += horizontalInput * moveSpeed * Time.deltaTime;

            // 좌우 이동 범위 제한 (Clamp)
            position.x = Mathf.Clamp(position.x, minX, maxX);
            transform.position = position;
        }

        private void HandleShooting()
        {
            // 스페이스바 또는 기본 사격 키 입력 확인
            if (Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1"))
            {
                if (Time.time >= lastShootTime + shootCooldown)
                {
                    // 현재 활성화된 플레이어 탄환 수 확인
                    int activeBulletCount = 0;
                    Bullet[] existingBullets = FindObjectsByType<Bullet>(FindObjectsSortMode.None);
                    foreach (var b in existingBullets)
                    {
                        if (!b.IsEnemyBullet) activeBulletCount++;
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

            if (bulletPrefab != null)
            {
                Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            }
            else
            {
                // 프리팹이 없을 경우 기본 Quad로 임시 발사
                GameObject defaultBullet = GameObject.CreatePrimitive(PrimitiveType.Quad);
                defaultBullet.name = "PlayerBullet";
                defaultBullet.transform.position = firePoint.position;
                defaultBullet.transform.localScale = new Vector3(0.15f, 0.4f, 1f);
                
                Collider col = defaultBullet.GetComponent<Collider>();
                if (col != null) Destroy(col);

                defaultBullet.AddComponent<BoxCollider2D>();
                defaultBullet.AddComponent<Bullet>();
            }
        }
    }
}
