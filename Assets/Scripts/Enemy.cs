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
        private float diveDuration = 2.4f;
        private bool hasShotDuringDive = false;
        private float loopDirection = 1f;

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

        public void Setup(EnemyType type, int hp = 1, int score = GameConstants.SCORE_PER_ENEMY, GameObject explosion = null)
        {
            enemyType = type;
            maxHp = hp;
            currentHp = hp;
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

        public void StartDive(Vector3 targetPos, float duration = 2.4f)
        {
            currentState = EnemyState.Diving;
            diveStartPos = transform.position;
            diveTargetPos = targetPos;
            diveDuration = duration;
            diveProgress = 0f;
            hasShotDuringDive = false;
            loopDirection = (UnityEngine.Random.value > 0.5f) ? 1f : -1f;
        }

        public void StartDocking(Vector3 spawnWorldPos, float duration = 1.4f)
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
            float t = Mathf.Clamp01(diveProgress);
            Vector3 nextPos;

            // 1단계 (t: 0.0 ~ 0.30): 갤러그 스타일 상공 원형(360도 루프) 휙 선회 기동
            if (t <= 0.30f)
            {
                float loopT = t / 0.30f;
                float loopAngle = loopT * Mathf.PI * 2.0f; // 0 ~ 360도
                float radius = 0.85f;
                Vector3 loopCenter = diveStartPos + new Vector3(loopDirection * radius, 0.35f, 0);

                float px = loopCenter.x - Mathf.Cos(loopAngle) * (loopDirection * radius);
                float py = loopCenter.y + Mathf.Sin(loopAngle) * radius;
                nextPos = new Vector3(px, py, 0);
            }
            else // 2단계 (t: 0.30 ~ 1.0): 플레이어를 향한 가속 급강하 궤적
            {
                float diveT = (t - 0.30f) / 0.70f;
                Vector3 toPos = diveTargetPos + Vector3.down * 4.5f;
                nextPos = Vector3.Lerp(diveStartPos, toPos, diveT);
                nextPos.x += Mathf.Sin(diveT * Mathf.PI) * (loopDirection * 1.5f);
            }

            // 기체 이동 방향을 바라보도록 회전 연출
            Vector3 moveDelta = nextPos - transform.position;
            if (moveDelta.sqrMagnitude > 0.0001f)
            {
                float rotAngle = Mathf.Atan2(moveDelta.y, moveDelta.x) * Mathf.Rad2Deg + 90f;
                transform.rotation = Quaternion.Euler(0, 0, rotAngle);
            }
            transform.position = nextPos;

            // 강하 중(루프 직후 약 45%) 플레이어를 향해 조준 사격 1발
            if (!hasShotDuringDive && t >= 0.45f)
            {
                hasShotDuringDive = true;
                if (parentFleet != null)
                {
                    parentFleet.FireAimedBullet(transform.position, diveTargetPos);
                }
            }

            // 화면 하단 완전히 벗어남
            if (transform.position.y < -GameConstants.SCREEN_HEIGHT_HALF - 0.8f)
            {
                // 생존 시: 상단에서 재진입하여 원래 편대 슬롯으로 복귀 (갤러그 루프)
                if (currentHp > 0 && parentFleet != null)
                {
                    Vector3 slotTarget = parentFleet.GetSlotWorldPosition(slotIndex);
                    Vector3 reEnterPos = new Vector3(slotTarget.x, GameConstants.SCREEN_HEIGHT_HALF + 1.2f, 0);
                    transform.rotation = Quaternion.identity;
                    StartDocking(reEnterPos, 1.4f);
                }
                else
                {
                    if (parentFleet != null)
                    {
                        parentFleet.NotifyDiverEscaped(this, slotIndex);
                    }
                    Destroy(gameObject);
                }
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

            // 원래 각도(0도)로 부드럽게 정렬
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.identity, t);

            if (dockProgress >= 1f)
            {
                transform.rotation = Quaternion.identity;
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

            // 아이템 드롭 (돌진 적 격파 시 100% 드롭, 일반 적 25% 드롭)
            if (parentFleet != null)
            {
                bool shouldDrop = (currentState == EnemyState.Diving) || (UnityEngine.Random.value < 0.25f);
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
