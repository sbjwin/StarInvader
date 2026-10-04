using System;
using UnityEngine;

namespace StarInvader
{
    public enum EnemyType
    {
        Top,
        Mid,
        Bottom
    }

    public enum EnemyState
    {
        InFormation, // 편대 대열 내 위치
        Diving,      // 플레이어를 향해 급강하 돌진 중
        Docking      // 화면 상단에서 슬롯으로 안착 중
    }

    /// <summary>
    /// 개별 적 기체 컴포넌트 (편대 슬롯 도킹, 급강하 돌진, 피격/폭발 및 아이템 드롭 연동)
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [Header("적 속성")]
        [SerializeField] private EnemyType enemyType = EnemyType.Bottom;
        [SerializeField] private int scoreValue = GameConstants.SCORE_PER_ENEMY;
        [SerializeField] private int maxHp = 1;
        [SerializeField] private GameObject explosionPrefab;

        private int currentHp;
        private SpriteRenderer spriteRenderer;
        private Color originalColor;
        private Coroutine flashCoroutine;

        // 상태 및 슬롯 제어
        private EnemyState currentState = EnemyState.InFormation;
        private EnemyFleet parentFleet;
        private int slotIndex = -1;
        private Vector3 localSlotPosition;

        // 돌진 제어
        private Vector3 diveStartPos;
        private Vector3 diveTargetPos;
        private float diveProgress = 0f;
        private float diveDuration = 2.2f;
        private bool hasShotDuringDive = false;

        // 도킹 제어
        private Vector3 dockStartPos;
        private float dockProgress = 0f;
        private float dockDuration = 1.2f;

        public event Action<Enemy> OnDestroyed;

        private void Awake()
        {
            currentHp = maxHp;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
        }

        public void Setup(EnemyType type, int score = GameConstants.SCORE_PER_ENEMY, GameObject explosion = null)
        {
            enemyType = type;
            scoreValue = score;
            if (explosion != null) explosionPrefab = explosion;
        }

        public void AssignSlot(EnemyFleet fleet, int slotIdx, Vector3 localPos)
        {
            parentFleet = fleet;
            slotIndex = slotIdx;
            localSlotPosition = localPos;
            currentState = EnemyState.InFormation;
        }

        public void StartDive(Vector3 targetPos, float duration = 2.2f)
        {
            currentState = EnemyState.Diving;
            diveStartPos = transform.position;
            diveTargetPos = targetPos;
            diveDuration = duration;
            diveProgress = 0f;
            hasShotDuringDive = false;
        }

        public void StartDocking(Vector3 spawnWorldPos, float duration = 1.2f)
        {
            currentState = EnemyState.Docking;
            dockStartPos = spawnWorldPos;
            transform.position = spawnWorldPos;
            dockDuration = duration;
            dockProgress = 0f;
        }

        private void Update()
        {
            switch (currentState)
            {
                case EnemyState.InFormation:
                    // 편대 이동은 EnemyFleet에서 일괄 관리
                    break;

                case EnemyState.Diving:
                    UpdateDiveMovement();
                    break;

                case EnemyState.Docking:
                    UpdateDockingMovement();
                    break;
            }
        }

        private void UpdateDiveMovement()
        {
            diveProgress += Time.deltaTime / diveDuration;

            // 베지어 곡선 기반 급강하 S자 돌진 궤적
            float t = diveProgress;
            // X축은 약간의 흔들림을 주며 타겟 X로 수렴, Y축은 아래로 강하
            float curveX = Mathf.Sin(t * Mathf.PI * 1.5f) * 1.2f;
            Vector3 currentPos = Vector3.Lerp(diveStartPos, diveTargetPos + Vector3.down * 4.0f, t);
            currentPos.x += curveX;
            transform.position = currentPos;

            // 강하 중간 지점(약 35%)에서 플레이어를 향해 조준 사격 1발
            if (!hasShotDuringDive && t >= 0.35f)
            {
                hasShotDuringDive = true;
                if (parentFleet != null)
                {
                    parentFleet.FireAimedBullet(transform.position, diveTargetPos);
                }
            }

            // 화면 하단 완전히 벗어남
            if (transform.position.y < -GameConstants.SCREEN_HEIGHT_HALF - 1.2f)
            {
                if (parentFleet != null)
                {
                    parentFleet.NotifyDiverEscaped(this, slotIndex);
                }
                Destroy(gameObject);
            }
        }

        private void UpdateDockingMovement()
        {
            dockProgress += Time.deltaTime / dockDuration;
            float t = Mathf.Clamp01(dockProgress);
            // 부드러운 스무스스텝 안착
            t = t * t * (3f - 2f * t);

            if (parentFleet != null)
            {
                Vector3 targetWorldPos = parentFleet.GetSlotWorldPosition(slotIndex);
                transform.position = Vector3.Lerp(dockStartPos, targetWorldPos, t);
            }

            if (dockProgress >= 1f)
            {
                currentState = EnemyState.InFormation;
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

        private System.Collections.IEnumerator HitFlashRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
                yield return new WaitForSeconds(0.06f);
                spriteRenderer.color = originalColor;
            }
        }

        private void Die()
        {
            // 폭발 이펙트 생성
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }

            // 폭발 사운드 재생
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayExplosionSound();
            }

            // 점수 가산 (돌진 중 격파 시 보너스 2배)
            int finalScore = (currentState == EnemyState.Diving) ? scoreValue * 2 : scoreValue;
            if (InGameController.Instance != null)
            {
                InGameController.Instance.AddScore(finalScore);
                if (currentState == EnemyState.Diving)
                {
                    InGameController.Instance.ShowBonusScorePopup(transform.position, finalScore);
                }
            }

            // 아이템 드롭 (돌진 적 격파 시 100% 드롭, 일반 적 15% 드롭)
            if (parentFleet != null)
            {
                bool shouldDrop = (currentState == EnemyState.Diving) || (UnityEngine.Random.value < 0.15f);
                if (shouldDrop)
                {
                    parentFleet.SpawnItemDrop(transform.position);
                }
            }

            OnDestroyed?.Invoke(this);

            if (parentFleet != null)
            {
                parentFleet.NotifyEnemyKilled(this, slotIndex);
            }

            Destroy(gameObject);
        }

        public EnemyType Type => enemyType;
        public int ScoreValue => scoreValue;
        public EnemyState State => currentState;
        public int SlotIndex => slotIndex;
        public Vector3 LocalSlotPosition => localSlotPosition;
    }
}
