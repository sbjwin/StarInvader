using UnityEngine;

namespace StarInvader
{
    public enum ItemType
    {
        Powerup,  // [P] 주무기 업그레이드
        Special,  // [SP] 특수무기 게이지 충전
        Shield,   // [S] 에너지 실드
        Gem       // [★] 보너스 젬 (1000점)
    }

    /// <summary>
    /// 적 격파 시 드롭되는 아이템 (P/SP/Shield/Gem) 및 습득 효과
    /// </summary>
    public class PowerupItem : MonoBehaviour
    {
        [SerializeField] private ItemType itemType = ItemType.Powerup;
        [SerializeField] private float fallSpeed = 2.2f;

        private SpriteRenderer spriteRenderer;
        private float lifeTimer = 0f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(ItemType type)
        {
            itemType = type;
            ConfigureVisual();
        }

        private void ConfigureVisual()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) return;

            switch (itemType)
            {
                case ItemType.Powerup:
                    spriteRenderer.color = new Color(1f, 0.2f, 0.2f); // 붉은색 [P]
                    break;
                case ItemType.Special:
                    spriteRenderer.color = new Color(0.2f, 0.8f, 1f); // 청록색 [SP]
                    break;
                case ItemType.Shield:
                    spriteRenderer.color = new Color(0.3f, 1f, 0.4f); // 녹색 [S]
                    break;
                case ItemType.Gem:
                    spriteRenderer.color = new Color(1f, 0.85f, 0.1f); // 황금색 [GEM]
                    break;
            }
        }

        private void Update()
        {
            // 천천히 아래로 하강
            transform.position += Vector3.down * (fallSpeed * Time.deltaTime);

            // 좌우 살랑거리는 궤적
            lifeTimer += Time.deltaTime * 4f;
            transform.position += Vector3.right * (Mathf.Sin(lifeTimer) * 0.015f);

            // 화면 하단 벗어나면 소멸
            if (transform.position.y < -GameConstants.SCREEN_HEIGHT_HALF - 1.0f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                ApplyEffect(player);
                Destroy(gameObject);
            }
        }

        private void ApplyEffect(PlayerController player)
        {
            if (InGameController.Instance == null) return;

            switch (itemType)
            {
                case ItemType.Powerup:
                    player.UpgradeWeapon();
                    InGameController.Instance.ShowBonusScorePopup(transform.position, 500);
                    break;

                case ItemType.Special:
                    player.AddSp(50f);
                    InGameController.Instance.ShowBonusScorePopup(transform.position, 500);
                    break;

                case ItemType.Shield:
                    player.ActivateShield();
                    InGameController.Instance.ShowBonusScorePopup(transform.position, 500);
                    break;

                case ItemType.Gem:
                    InGameController.Instance.AddScore(1000);
                    InGameController.Instance.ShowBonusScorePopup(transform.position, 1000);
                    break;
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBonusSound();
            }
        }

        public ItemType Type => itemType;
    }
}
