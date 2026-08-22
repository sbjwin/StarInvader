using System.Collections;
using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 화면 상단을 횡단하는 고득점 보너스 외계인 모함 (Mystery UFO)
    /// </summary>
    public class BonusUfo : MonoBehaviour
    {
        [Header("UFO 설정")]
        [SerializeField] private float speed = 3.5f;
        [SerializeField] private int maxHp = 1;
        [SerializeField] private GameObject explosionPrefab;

        private int moveDirection = 1; // 1: 좌->우, -1: 우->좌
        private int currentHp;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Coroutine flashCoroutine;

        private void Awake()
        {
            currentHp = maxHp;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        private void Start()
        {
            // 출현 시 사이렌 사운드 시작
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayUfoSound();
            }
        }

        public void Initialize(int direction, float moveSpeed = 3.5f, GameObject explosion = null)
        {
            moveDirection = direction >= 0 ? 1 : -1;
            speed = moveSpeed;
            if (explosion != null) explosionPrefab = explosion;
        }

        private void Update()
        {
            transform.position += Vector3.right * (moveDirection * speed * Time.deltaTime);

            // 화면 경계를 완전히 벗어나면 소멸
            if (Mathf.Abs(transform.position.x) > GameConstants.SCREEN_WIDTH_HALF + 1.5f)
            {
                DestroyUfoWithoutScore();
            }
        }

        public void TakeDamage(int damage = 1)
        {
            currentHp -= damage;
            if (currentHp <= 0)
            {
                Die();
            }
            else
            {
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(HitFlashRoutine());
            }
        }

        private IEnumerator HitFlashRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
                yield return new WaitForSeconds(0.06f);
                spriteRenderer.color = originalColor;
            }
        }

        private void Die()
        {
            // 랜덤 보너스 점수 결정 (500, 800, 1000, 1500점)
            int[] bonusOptions = new int[] { 500, 800, 1000, 1500 };
            int bonusScore = bonusOptions[Random.Range(0, bonusOptions.Length)];

            // 점수 및 콤보 반영
            if (InGameController.Instance != null)
            {
                InGameController.Instance.AddScore(bonusScore);
                InGameController.Instance.ShowBonusScorePopup(transform.position, bonusScore);
            }

            // 폭발 이펙트 생성
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }

            // 전용 팡파레 SFX
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopUfoSound();
                SoundManager.Instance.PlayBonusSound();
                SoundManager.Instance.PlayExplosionSound();
            }

            Destroy(gameObject);
        }

        private void DestroyUfoWithoutScore()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopUfoSound();
            }
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopUfoSound();
            }
        }
    }
}
