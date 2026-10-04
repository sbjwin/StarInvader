using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarInvader
{
    public class FormationSlot
    {
        public int slotIndex;
        public Vector3 localOffset;
        public EnemyType type;
        public Enemy currentEnemy;
        public bool isReservedForReinforcement;

        public FormationSlot(int index, Vector3 offset, EnemyType enemyType)
        {
            slotIndex = index;
            localOffset = offset;
            type = enemyType;
            currentEnemy = null;
            isReservedForReinforcement = false;
        }
    }

    /// <summary>
    /// 적 편대 제어기 (스테이지별 다채로운 포메이션, 팔자/부채꼴 탄막, 급강하 돌진 및 상단 증원 도킹 관리)
    /// </summary>
    public class EnemyFleet : MonoBehaviour
    {
        [Header("프리팹 설정")]
        [SerializeField] private GameObject topEnemyPrefab;
        [SerializeField] private GameObject midEnemyPrefab;
        [SerializeField] private GameObject bottomEnemyPrefab;
        [SerializeField] private GameObject enemyBulletPrefab;

        [Header("이동 설정")]
        [SerializeField] private float baseSpeed = GameConstants.ENEMY_BASE_SPEED_X;
        [SerializeField] private float maxSpeed = 3.6f;
        [SerializeField] private float dropDistance = GameConstants.ENEMY_DROP_DISTANCE;
        [SerializeField] private float boundaryX = GameConstants.SCREEN_WIDTH_HALF - 0.35f;
        [SerializeField] private float invasionYLimit = GameConstants.INVASION_Y_LIMIT;

        [Header("사격 설정")]
        [SerializeField] private float shootIntervalMin = 0.8f;
        [SerializeField] private float shootIntervalMax = 2.0f;

        [Header("돌진(Dive) 설정")]
        [SerializeField] private float diveIntervalMin = 4.0f;
        [SerializeField] private float diveIntervalMax = 7.0f;

        // 슬롯 및 적기 리스트
        private List<FormationSlot> formationSlots = new List<FormationSlot>();
        private List<Enemy> activeEnemies = new List<Enemy>();
        private int totalInitialEnemies = 0;
        private int remainingReinforcements = 0; // 스테이지당 상단 증원 가능 횟수
        private int moveDirection = 1;
        private float currentSpeed;
        private float nextShootTime = 0f;
        private float nextDiveTime = 0f;
        private Vector3 fleetAnchorPosition;

        private void Start()
        {
            fleetAnchorPosition = transform.position;
            SpawnFleet();
            ScheduleNextShot();
            ScheduleNextDive();
        }

        private float motionTimer = 0f;
        private int currentStage = 1;

        public void SpawnFleet()
        {
            ClearFleet();

            currentStage = (GameDataManager.Instance != null) ? GameDataManager.Instance.CurrentStage : 1;
            int stage = currentStage;
            motionTimer = 0f;

            float dynamicStartY = Mathf.Max(2.0f, GameConstants.ENEMY_START_Y - (stage - 1) * 0.25f);
            float dynamicBaseSpeed = Mathf.Min(2.4f, baseSpeed + (stage - 1) * 0.10f);

            // [밸런스 완화] 사격 주기: Stage 1은 2.2~3.5초로 여유롭게, 고레벨로 갈수록 점진적 단축
            shootIntervalMin = Mathf.Max(0.8f, 2.2f - (stage - 1) * 0.35f);
            shootIntervalMax = Mathf.Max(1.4f, 3.5f - (stage - 1) * 0.45f);

            // [밸런스 완화] 상단 증원 쿼터: Stage 1은 0기(증원 없음), Stage 2는 2기, Stage 3+는 4~6기
            if (stage == 1) remainingReinforcements = 0;
            else if (stage == 2) remainingReinforcements = 2;
            else remainingReinforcements = Mathf.Min(6, 4 + (stage - 3) * 2);

            // [밸런스 완화] 급강하 돌진 주기: Stage 1은 완전 금지, Stage 2는 10~15초로 드물게
            if (stage == 1)
            {
                diveIntervalMin = 9999f;
                diveIntervalMax = 9999f;
            }
            else if (stage == 2)
            {
                diveIntervalMin = 10.0f;
                diveIntervalMax = 15.0f;
            }
            else
            {
                diveIntervalMin = Mathf.Max(4.5f, 6.5f - (stage - 3) * 0.6f);
                diveIntervalMax = Mathf.Max(7.5f, 10.0f - (stage - 3) * 0.8f);
            }

            fleetAnchorPosition = new Vector3(0, dynamicStartY, 0);
            transform.position = fleetAnchorPosition;

            // 스테이지별 포메이션 레이아웃 획득
            string[] layout = GetFormationLayoutForStage(stage);
            BuildFormationSlotsFromLayout(layout);

            // 각 슬롯에 적기 인스턴스화
            foreach (var slot in formationSlots)
            {
                SpawnEnemyInSlot(slot);
            }

            totalInitialEnemies = activeEnemies.Count + remainingReinforcements;
            currentSpeed = dynamicBaseSpeed;
            moveDirection = 1;
        }

        private string[] GetFormationLayoutForStage(int stage)
        {
            int patternIndex = (stage - 1) % 4;
            switch (patternIndex)
            {
                case 0: // Stage 1: V자 화살촉 대형 (Arrowhead, 17기)
                    return new string[]
                    {
                        "...T...",
                        "..M.M..",
                        ".M.M.M.",
                        "B.B.B.B",
                        "B.....B"
                    };

                case 1: // Stage 2: 다이아몬드 마름모 대형 (Diamond, 20기)
                    return new string[]
                    {
                        "...T...",
                        "..T.T..",
                        ".M...M.",
                        "M.B.B.M",
                        ".B...B.",
                        "..B.B.."
                    };

                case 2: // Stage 3: W자 듀얼 윙 날개 대형 (Dual Wings, 22기)
                    return new string[]
                    {
                        "T.....T",
                        "M.T.T.M",
                        "M.M.M.M",
                        "B.B.B.B",
                        ".B...B."
                    };

                case 3: // Stage 4+: 요새 크로스 대형 (Fortress, 26기)
                default:
                    return new string[]
                    {
                        ".T.T.T.",
                        "M.T.T.M",
                        "M.M.M.M",
                        "B.M.M.B",
                        "B.B.B.B"
                    };
            }
        }

        private void BuildFormationSlotsFromLayout(string[] layout)
        {
            formationSlots.Clear();

            int rows = layout.Length;
            int cols = layout[0].Length;
            float spacingX = 0.8f;
            float spacingY = 0.65f;

            float totalWidth = (cols - 1) * spacingX;
            float startX = -totalWidth / 2f;

            int slotCounter = 0;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    char ch = layout[r][c];
                    if (ch == '.') continue;

                    EnemyType type = (ch == 'T') ? EnemyType.Top : ((ch == 'M') ? EnemyType.Mid : EnemyType.Bottom);
                    Vector3 localPos = new Vector3(startX + (c * spacingX), -(r * spacingY), 0);

                    FormationSlot slot = new FormationSlot(slotCounter++, localPos, type);
                    formationSlots.Add(slot);
                }
            }
        }

        private void SpawnEnemyInSlot(FormationSlot slot)
        {
            GameObject prefab = GetPrefabForType(slot.type);
            Vector3 worldPos = transform.position + slot.localOffset;
            GameObject enemyObj = null;

            if (prefab != null)
            {
                enemyObj = Instantiate(prefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                enemyObj = CreateFallbackEnemy(slot.type, worldPos);
            }

            Enemy enemyComp = enemyObj.GetComponent<Enemy>();
            if (enemyComp == null) enemyComp = enemyObj.AddComponent<Enemy>();

            enemyComp.Setup(slot.type);
            enemyComp.AssignSlot(this, slot.slotIndex, slot.localOffset);
            enemyComp.OnDestroyed += HandleEnemyDestroyed;

            slot.currentEnemy = enemyComp;
            slot.isReservedForReinforcement = false;
            activeEnemies.Add(enemyComp);
        }

        private void CalculateCurrentMotion(out Vector3 motionOffset, out float pulseScale)
        {
            int motionPattern = (currentStage - 1) % 4;
            float motionOffsetX = 0f;
            float motionOffsetY = 0f;
            pulseScale = 1.0f;

            switch (motionPattern)
            {
                case 0: // Stage 1: 은은한 호흡 부유 (Gentle Sway)
                    motionOffsetY = Mathf.Sin(motionTimer * 1.8f) * 0.18f;
                    break;

                case 1: // Stage 2: 롤러코스터 사인파 웨이브 (Sine Wave Fluctuation)
                    motionOffsetY = Mathf.Sin(motionTimer * 2.8f) * 0.45f;
                    break;

                case 2: // Stage 3: 8자(∞) 입체 선회 (Figure-8 Orbit)
                    motionOffsetX = Mathf.Sin(motionTimer * 1.6f) * 0.55f;
                    motionOffsetY = Mathf.Sin(motionTimer * 3.2f) * 0.35f;
                    break;

                case 3: // Stage 4+: 호흡 팽창 & 변칙 기동 (Breathing Pulse)
                    motionOffsetY = Mathf.Sin(motionTimer * 2.2f) * 0.28f;
                    pulseScale = 1.0f + Mathf.Sin(motionTimer * 2.6f) * 0.18f;
                    break;
            }

            motionOffset = new Vector3(motionOffsetX, motionOffsetY, 0);
        }

        private void Update()
        {
            if (activeEnemies.Count == 0 && remainingReinforcements <= 0) return;

            motionTimer += Time.deltaTime;
            CalculateCurrentMotion(out Vector3 motionOffset, out float pulseScale);

            // 1. 편대 앵커 이동
            float deltaX = moveDirection * currentSpeed * Time.deltaTime;
            fleetAnchorPosition.x += deltaX;
            transform.position = fleetAnchorPosition + motionOffset;

            bool hitBoundary = false;
            bool reachedInvasionLimit = false;

            // 2. 대열 내 적기 위치 동기화 및 경계 판정
            foreach (var slot in formationSlots)
            {
                if (slot.currentEnemy != null && slot.currentEnemy.State == EnemyState.InFormation)
                {
                    Vector3 worldPos = fleetAnchorPosition + (slot.localOffset * pulseScale) + motionOffset;
                    slot.currentEnemy.transform.position = worldPos;

                    if (moveDirection > 0 && worldPos.x >= boundaryX) hitBoundary = true;
                    else if (moveDirection < 0 && worldPos.x <= -boundaryX) hitBoundary = true;

                    if (worldPos.y <= invasionYLimit) reachedInvasionLimit = true;
                }
            }

            // 3. 경계 도달 시 방향 반전 및 하강
            if (hitBoundary)
            {
                moveDirection *= -1;
                fleetAnchorPosition.y -= dropDistance;
                transform.position = fleetAnchorPosition + motionOffset;
            }

            // 4. 침략 한계선 돌파 판정
            if (reachedInvasionLimit)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null && player.gameObject.activeSelf)
                {
                    Debug.LogWarning("[외계인 침략 성공] 적 편대가 방어선을 돌파했습니다!");
                    player.TakeDamage(999);
                }
            }

            // 5. 적 탄막 사격
            if (Time.time >= nextShootTime)
            {
                ShootPatternEnemyBullet();
                ScheduleNextShot();
            }

            // 6. 급강하 돌진(Dive Attack) 트리거
            if (Time.time >= nextDiveTime)
            {
                TriggerDiveAttack();
                ScheduleNextDive();
            }
        }

        private void ShootPatternEnemyBullet()
        {
            if (activeEnemies.Count == 0 || enemyBulletPrefab == null) return;

            // 대열 내에 있는 적기 중 무작위 1기 선정
            List<Enemy> formationEnemies = new List<Enemy>();
            foreach (var e in activeEnemies)
            {
                if (e != null && e.State == EnemyState.InFormation)
                {
                    formationEnemies.Add(e);
                }
            }

            if (formationEnemies.Count == 0) return;
            Enemy shooter = formationEnemies[Random.Range(0, formationEnemies.Count)];
            if (shooter == null) return;

            Vector3 spawnPos = shooter.transform.position + Vector3.down * 0.35f;

            // [밸런스 완화] 탄환 속도: Stage 1은 3.8f로 여유롭게, 레벨마다 0.7f씩 상승 (최대 5.8f)
            float dynamicBulletSpeed = Mathf.Min(5.8f, 3.8f + (currentStage - 1) * 0.7f);

            // [밸런스 완화] 스테이지별 탄막 패턴 단계적 해금
            if (currentStage == 1)
            {
                // Stage 1: 100% 무조건 느린 수직 1발 단발만 발사 (초보자 안심 적응)
                FireBullet(spawnPos, Vector2.down, dynamicBulletSpeed);
            }
            else if (currentStage == 2)
            {
                // Stage 2: 기본 수직 단발 위주 + 가끔(25%) Mid 기체만 2발 팔자탄 발사
                if (shooter.Type == EnemyType.Mid && Random.value < 0.25f)
                {
                    FireBullet(spawnPos + new Vector3(-0.1f, 0, 0), Quaternion.Euler(0, 0, 16f) * Vector2.down, dynamicBulletSpeed);
                    FireBullet(spawnPos + new Vector3(0.1f, 0, 0), Quaternion.Euler(0, 0, -16f) * Vector2.down, dynamicBulletSpeed);
                }
                else
                {
                    FireBullet(spawnPos, Vector2.down, dynamicBulletSpeed);
                }
            }
            else
            {
                // Stage 3 이상: 본격적인 2발 팔자 및 3발 부채꼴 확산 탄막 전개
                switch (shooter.Type)
                {
                    case EnemyType.Bottom:
                        FireBullet(spawnPos, Vector2.down, dynamicBulletSpeed);
                        break;

                    case EnemyType.Mid:
                        FireBullet(spawnPos + new Vector3(-0.1f, 0, 0), Quaternion.Euler(0, 0, 18f) * Vector2.down, dynamicBulletSpeed);
                        FireBullet(spawnPos + new Vector3(0.1f, 0, 0), Quaternion.Euler(0, 0, -18f) * Vector2.down, dynamicBulletSpeed);
                        break;

                    case EnemyType.Top:
                        FireBullet(spawnPos, Vector2.down, dynamicBulletSpeed);
                        FireBullet(spawnPos, Quaternion.Euler(0, 0, 24f) * Vector2.down, dynamicBulletSpeed);
                        FireBullet(spawnPos, Quaternion.Euler(0, 0, -24f) * Vector2.down, dynamicBulletSpeed);
                        break;
                }
            }

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayEnemyShootSound();
            }
        }

        private void FireBullet(Vector3 pos, Vector2 direction, float bulletSpeed)
        {
            if (enemyBulletPrefab == null) return;
            GameObject bObj = Instantiate(enemyBulletPrefab, pos, Quaternion.identity);
            Bullet bulletComp = bObj.GetComponent<Bullet>();
            if (bulletComp != null)
            {
                bulletComp.Initialize(direction, bulletSpeed, true);
            }
        }

        public void FireAimedBullet(Vector3 fromPos, Vector3 targetPos)
        {
            if (enemyBulletPrefab == null) return;
            float dynamicBulletSpeed = Mathf.Min(5.8f, 3.8f + (currentStage - 1) * 0.7f) * 1.1f;
            Vector2 aimDir = (targetPos - fromPos).normalized;
            GameObject bObj = Instantiate(enemyBulletPrefab, fromPos, Quaternion.identity);
            Bullet bulletComp = bObj.GetComponent<Bullet>();
            if (bulletComp != null)
            {
                bulletComp.Initialize(aimDir, dynamicBulletSpeed, true);
            }
        }

        private void TriggerDiveAttack()
        {
            // Stage 1은 급강하 돌진 완전 금지
            if (currentStage <= 1 || activeEnemies.Count == 0) return;

            // 대열 내에 있는 적 중 1기 선정
            List<Enemy> candidates = new List<Enemy>();
            foreach (var e in activeEnemies)
            {
                if (e != null && e.State == EnemyState.InFormation)
                {
                    candidates.Add(e);
                }
            }

            if (candidates.Count == 0) return;
            Enemy diver = candidates[Random.Range(0, candidates.Count)];

            PlayerController player = FindAnyObjectByType<PlayerController>();
            Vector3 targetPos = (player != null) ? player.transform.position : new Vector3(0, -4.0f, 0);

            diver.StartDive(targetPos);
        }

        public void NotifyDiverEscaped(Enemy diver, int slotIdx)
        {
            activeEnemies.Remove(diver);
            HandleSlotVacated(slotIdx);
            CheckStageClear();
        }

        public void NotifyEnemyKilled(Enemy enemy, int slotIdx)
        {
            activeEnemies.Remove(enemy);
            HandleSlotVacated(slotIdx);
            CheckStageClear();
        }

        private void HandleSlotVacated(int slotIdx)
        {
            FormationSlot slot = formationSlots.Find(s => s.slotIndex == slotIdx);
            if (slot == null) return;

            slot.currentEnemy = null;

            // 스테이지 증원 쿼터가 남아있으면 상단 증원 예약
            if (remainingReinforcements > 0 && !slot.isReservedForReinforcement)
            {
                slot.isReservedForReinforcement = true;
                remainingReinforcements--;
                StartCoroutine(ReinforceSlotRoutine(slot));
            }
        }

        private IEnumerator ReinforceSlotRoutine(FormationSlot slot)
        {
            // 1.8~2.5초 대기 후 화면 상단 밖에서 스폰
            yield return new WaitForSeconds(Random.Range(1.8f, 2.5f));

            if (this == null || !gameObject.activeInHierarchy) yield break;

            Vector3 targetWorldPos = GetSlotWorldPosition(slot.slotIndex);
            Vector3 spawnPos = new Vector3(targetWorldPos.x, GameConstants.SCREEN_HEIGHT_HALF + 1.2f, 0);

            GameObject prefab = GetPrefabForType(slot.type);
            GameObject enemyObj = null;

            if (prefab != null)
            {
                enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
            }
            else
            {
                enemyObj = CreateFallbackEnemy(slot.type, spawnPos);
            }

            Enemy newEnemy = enemyObj.GetComponent<Enemy>();
            if (newEnemy == null) newEnemy = enemyObj.AddComponent<Enemy>();

            newEnemy.Setup(slot.type);
            newEnemy.AssignSlot(this, slot.slotIndex, slot.localOffset);
            newEnemy.OnDestroyed += HandleEnemyDestroyed;

            slot.currentEnemy = newEnemy;
            slot.isReservedForReinforcement = false;
            activeEnemies.Add(newEnemy);

            // 상단에서 슬롯으로 스무스 도킹 시작
            newEnemy.StartDocking(spawnPos, 1.4f);
        }

        public Vector3 GetSlotWorldPosition(int slotIdx)
        {
            FormationSlot slot = formationSlots.Find(s => s.slotIndex == slotIdx);
            if (slot != null)
            {
                CalculateCurrentMotion(out Vector3 motionOffset, out float pulseScale);
                return fleetAnchorPosition + (slot.localOffset * pulseScale) + motionOffset;
            }
            return transform.position;
        }

        public void SpawnItemDrop(Vector3 pos)
        {
            // 드롭 아이템 4종 중 가중치 기반 무작위 추첨
            // P (35%), SP (25%), Shield (15%), Gem (25%)
            float rand = Random.value;
            ItemType chosenType = ItemType.Powerup;

            if (rand < 0.35f) chosenType = ItemType.Powerup;
            else if (rand < 0.60f) chosenType = ItemType.Special;
            else if (rand < 0.75f) chosenType = ItemType.Shield;
            else chosenType = ItemType.Gem;

            GameObject itemObj = new GameObject($"Item_{chosenType}");
            itemObj.transform.position = pos;

            SpriteRenderer sr = itemObj.AddComponent<SpriteRenderer>();
            // 기본 원형/네모 스프라이트 생성
            if (bottomEnemyPrefab != null)
            {
                SpriteRenderer baseSr = bottomEnemyPrefab.GetComponent<SpriteRenderer>();
                if (baseSr != null) sr.sprite = baseSr.sprite;
            }
            itemObj.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            CircleCollider2D col = itemObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.3f;

            PowerupItem pItem = itemObj.AddComponent<PowerupItem>();
            pItem.Initialize(chosenType);
        }

        private void CheckStageClear()
        {
            if (activeEnemies.Count == 0 && remainingReinforcements <= 0)
            {
                if (InGameController.Instance != null)
                {
                    InGameController.Instance.HandleStageClear();
                }
                else
                {
                    Invoke(nameof(SpawnFleet), 1.0f);
                }
            }
        }

        private void ScheduleNextShot()
        {
            nextShootTime = Time.time + Random.Range(shootIntervalMin, shootIntervalMax);
        }

        private void ScheduleNextDive()
        {
            nextDiveTime = Time.time + Random.Range(diveIntervalMin, diveIntervalMax);
        }

        private void HandleEnemyDestroyed(Enemy enemy)
        {
            // enemy.Die() 내부에서 NotifyEnemyKilled가 호출되므로 속도 조정만 처리
            if (totalInitialEnemies > 0)
            {
                float destroyedRatio = 1f - ((float)activeEnemies.Count / totalInitialEnemies);
                currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, destroyedRatio);
            }
        }

        private GameObject GetPrefabForType(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Top: return topEnemyPrefab;
                case EnemyType.Mid: return midEnemyPrefab;
                case EnemyType.Bottom: return bottomEnemyPrefab;
                default: return bottomEnemyPrefab;
            }
        }

        private GameObject CreateFallbackEnemy(EnemyType type, Vector3 pos)
        {
            GameObject obj = new GameObject($"Enemy_{type}");
            obj.transform.position = pos;
            obj.transform.SetParent(transform);

            SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
            sr.color = type == EnemyType.Top ? Color.magenta : (type == EnemyType.Mid ? new Color(1f, 0.5f, 0.2f) : Color.yellow);

            BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            return obj;
        }

        public void ClearFleet()
        {
            StopAllCoroutines();
            CancelInvoke(nameof(SpawnFleet));

            foreach (var enemy in activeEnemies)
            {
                if (enemy != null) Destroy(enemy.gameObject);
            }
            activeEnemies.Clear();
            formationSlots.Clear();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugDropToInvasionLimit()
        {
            fleetAnchorPosition.y = invasionYLimit + 0.35f;
            transform.position = fleetAnchorPosition;
        }

        public void DebugKillAllExcept(int remainingCount = 1)
        {
            remainingReinforcements = 0;
            while (activeEnemies.Count > remainingCount)
            {
                var target = activeEnemies[activeEnemies.Count - 1];
                activeEnemies.RemoveAt(activeEnemies.Count - 1);
                if (target != null)
                {
                    Destroy(target.gameObject);
                }
            }
        }

        public void TriggerDebugDive()
        {
            TriggerDiveAttack();
        }
#endif

        private void OnDisable()
        {
            ClearFleet();
        }
    }
}
