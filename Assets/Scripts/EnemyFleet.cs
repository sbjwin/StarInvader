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
        private float lastDropTime = -10f;
        private const float DROP_COOLDOWN = 0.45f;

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

            float dynamicStartY = Mathf.Max(2.8f, GameConstants.ENEMY_START_Y - (stage - 1) * 0.15f);
            float dynamicBaseSpeed = Mathf.Min(2.0f, baseSpeed + (stage - 1) * 0.08f);

            // [밸런스 완화] 사격 주기: Stage 1은 2.5~3.8초, Stage 4도 1.7~2.7초로 보고 피할 수 있는 템포 유지
            shootIntervalMin = Mathf.Max(1.5f, 2.5f - (stage - 1) * 0.25f);
            shootIntervalMax = Mathf.Max(2.3f, 3.8f - (stage - 1) * 0.35f);

            // [밸런스 완화] 상단 증원 쿼터: 피로감 완화 (Stage 1: 0기, Stage 2: 1기, Stage 3+: 2기로 제한)
            if (stage == 1) remainingReinforcements = 0;
            else if (stage == 2) remainingReinforcements = 1;
            else remainingReinforcements = Mathf.Min(2, 1 + (stage - 2));

            // [밸런스 완화] 급강하 돌진 주기: Stage 1은 완전 금지, Stage 2는 12~18초, Stage 4는 9~14초로 여유 확보
            if (stage == 1)
            {
                diveIntervalMin = 9999f;
                diveIntervalMax = 9999f;
            }
            else if (stage == 2)
            {
                diveIntervalMin = 12.0f;
                diveIntervalMax = 18.0f;
            }
            else
            {
                diveIntervalMin = Mathf.Max(8.0f, 11.0f - (stage - 3) * 1.0f);
                diveIntervalMax = Mathf.Max(12.0f, 15.0f - (stage - 3) * 1.0f);
            }

            boundaryX = GameConstants.SCREEN_WIDTH_HALF - 0.5f;
            fleetAnchorPosition = new Vector3(0, dynamicStartY, 0);
            transform.position = fleetAnchorPosition;

            // 스테이지별 16:9 PC 와이드 포메이션 레이아웃 획득
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

            // 스테이지 시작 시 최소 안전 유예 시간 부여 (시작하자마자 기습 사격/돌진 방지)
            nextShootTime = Time.time + 2.5f;
            nextDiveTime = Time.time + 6.0f;
        }

        private string[] GetFormationLayoutForStage(int stage)
        {
            int patternIndex = (stage - 1) % 6;
            switch (patternIndex)
            {
                case 0: // Stage 1: 아케이드 V자 전초 대형 (Arrowhead, 13기 - 입문형)
                    return new string[]
                    {
                        "....T....",
                        "...M.M...",
                        "..M...M..",
                        ".B.B.B.B.",
                        "B.......B"
                    };

                case 1: // Stage 2: 아케이드 다이아몬드 돌파형 (Diamond, 15기 - 화력 확장)
                    return new string[]
                    {
                        "....T....",
                        "...M.M...",
                        "..M.T.M..",
                        ".B.M.M.B.",
                        "..B.B.B..",
                        "....B...."
                    };

                case 2: // Stage 3: W자 듀얼 윙 폭격형 (Dual Wings, 18기 - 양익 압박)
                    return new string[]
                    {
                        "T.......T",
                        ".M.T.T.M.",
                        ".M.M.M.M.",
                        "B.B.B.B.B",
                        ".B.....B."
                    };

                case 3: // Stage 4: 철벽 요새 크로스형 (Fortress Cross, 20기 - 방어벽 밀집)
                    return new string[]
                    {
                        ".T..T..T.",
                        "..M.T.M..",
                        "M..M.M..M",
                        ".M.B.B.M.",
                        "B.B...B.B"
                    };

                case 4: // Stage 5: 인베이더 스페이스 스컬 대형 (Space Skull, 22기 - 해골 위압감)
                    return new string[]
                    {
                        "..T.T.T..",
                        ".T..T..T.",
                        "M.T...T.M",
                        ".M.M.M.M.",
                        ".B.B.B.B.",
                        "..B...B.."
                    };

                case 5: // Stage 6+: 인피니티 옥타곤 결전 대형 (Infinity Octagon, 24기 - 최종 화력전)
                default:
                    return new string[]
                    {
                        "T...T...T",
                        ".M.T.T.M.",
                        "M.M.M.M.M",
                        ".B.M.M.B.",
                        "B.B.B.B.B"
                    };
            }
        }

        private void BuildFormationSlotsFromLayout(string[] layout)
        {
            formationSlots.Clear();

            int rows = layout.Length;
            int cols = layout[0].Length;
            float spacingX = 0.65f;
            float spacingY = 0.55f;

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

        private int GetHpForEnemy(EnemyType type, int stage)
        {
            if (stage <= 1) return 1;

            switch (type)
            {
                case EnemyType.Top:
                    return stage >= 4 ? 3 : 2; // 지휘관기: Stage 2~3은 2, Stage 4+는 3
                case EnemyType.Mid:
                    return stage >= 3 ? 2 : 1; // 중형기: Stage 3+는 2
                case EnemyType.Bottom:
                default:
                    return 1;
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

            int hp = GetHpForEnemy(slot.type, currentStage);
            enemyComp.Setup(slot.type, hp);
            enemyComp.AssignSlot(this, slot.slotIndex, slot.localOffset);
            enemyComp.OnDestroyed += HandleEnemyDestroyed;

            slot.currentEnemy = enemyComp;
            slot.isReservedForReinforcement = false;
            activeEnemies.Add(enemyComp);
        }

        private void CalculateCurrentMotion(out Vector3 motionOffset, out float pulseScale)
        {
            int motionPattern = (currentStage - 1) % 5;
            float motionOffsetX = 0f;
            float motionOffsetY = 0f;
            pulseScale = 1.0f;

            switch (motionPattern)
            {
                case 0: // Stage 1: 완만한 호흡 부유 (Gentle Floating)
                    motionOffsetY = Mathf.Sin(motionTimer * 2.0f) * 0.20f;
                    break;

                case 1: // Stage 2: 롤러코스터 파도타기 웨이브 (Sine Wave Surfing)
                    motionOffsetX = Mathf.Cos(motionTimer * 2.2f) * 0.25f;
                    motionOffsetY = Mathf.Sin(motionTimer * 3.2f) * 0.35f;
                    break;

                case 2: // Stage 3: 8자(∞) 입체 선회 궤적 (Figure-8 Orbit)
                    motionOffsetX = Mathf.Sin(motionTimer * 1.8f) * 0.40f;
                    motionOffsetY = Mathf.Sin(motionTimer * 3.6f) * 0.25f;
                    break;

                case 3: // Stage 4: 진자 스윙 & 호흡 펄스 (Pendulum Pulse)
                    motionOffsetX = Mathf.Sin(motionTimer * 2.4f) * 0.30f;
                    motionOffsetY = Mathf.Cos(motionTimer * 2.4f) * 0.20f;
                    pulseScale = 1.0f + Mathf.Sin(motionTimer * 2.6f) * 0.08f;
                    break;

                case 4: // Stage 5+: 다이나믹 카오스 요동 (Dynamic Chaos Sway)
                    motionOffsetX = (Mathf.Sin(motionTimer * 2.6f) + Mathf.Cos(motionTimer * 1.3f)) * 0.22f;
                    motionOffsetY = Mathf.Sin(motionTimer * 3.4f) * 0.28f;
                    pulseScale = 1.0f + Mathf.Sin(motionTimer * 3.0f) * 0.06f;
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

            // 2. 대열 내 적기 위치 동기화 및 외곽 경계/침략 판정용 AABB 산출 (Zero GC)
            float minEnemyX = float.MaxValue;
            float maxEnemyX = float.MinValue;
            float minEnemyY = float.MaxValue;
            bool hasFormationEnemy = false;

            for (int i = 0; i < formationSlots.Count; i++)
            {
                FormationSlot slot = formationSlots[i];
                if (slot.currentEnemy != null && slot.currentEnemy.State == EnemyState.InFormation)
                {
                    hasFormationEnemy = true;
                    Vector3 worldPos = fleetAnchorPosition + (slot.localOffset * pulseScale) + motionOffset;
                    slot.currentEnemy.transform.position = worldPos;

                    if (worldPos.x < minEnemyX) minEnemyX = worldPos.x;
                    if (worldPos.x > maxEnemyX) maxEnemyX = worldPos.x;
                    if (worldPos.y < minEnemyY) minEnemyY = worldPos.y;
                }
            }

            // 3. 경계 도달 시 방향 반전 및 스냅 보정 + 하강 쿨다운 적용 (연쇄 급강하 무한루프 완벽 차단)
            if (hasFormationEnemy)
            {
                bool canDrop = (Time.time >= lastDropTime + DROP_COOLDOWN);

                if (moveDirection > 0 && maxEnemyX >= boundaryX)
                {
                    moveDirection = -1;
                    float overshot = maxEnemyX - boundaryX;
                    fleetAnchorPosition.x -= overshot;

                    if (canDrop)
                    {
                        fleetAnchorPosition.y -= dropDistance;
                        lastDropTime = Time.time;
                    }
                    transform.position = fleetAnchorPosition + motionOffset;
                }
                else if (moveDirection < 0 && minEnemyX <= -boundaryX)
                {
                    moveDirection = 1;
                    float overshot = -boundaryX - minEnemyX;
                    fleetAnchorPosition.x += overshot;

                    if (canDrop)
                    {
                        fleetAnchorPosition.y -= dropDistance;
                        lastDropTime = Time.time;
                    }
                    transform.position = fleetAnchorPosition + motionOffset;
                }

                // 4. 침략 한계선 돌파 판정
                if (minEnemyY <= invasionYLimit)
                {
                    PlayerController player = FindAnyObjectByType<PlayerController>();
                    if (player != null && player.gameObject.activeSelf)
                    {
                        Debug.LogWarning("[외계인 침략 성공] 적 편대가 방어선을 돌파했습니다!");
                        player.TakeDamage(999);
                    }
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

            // [정통 아케이드 룰] 각 열(X축)에서 살아있는 적 중 '가장 아래쪽(최전선 앞줄)' 적기만 사격 후보로 선정!
            Dictionary<int, Enemy> lowestEnemyPerCol = new Dictionary<int, Enemy>();

            for (int i = 0; i < formationSlots.Count; i++)
            {
                var slot = formationSlots[i];
                if (slot.currentEnemy != null && slot.currentEnemy.State == EnemyState.InFormation)
                {
                    int colKey = Mathf.RoundToInt(slot.localOffset.x * 100f);

                    if (!lowestEnemyPerCol.ContainsKey(colKey))
                    {
                        lowestEnemyPerCol[colKey] = slot.currentEnemy;
                    }
                    else
                    {
                        if (slot.localOffset.y < lowestEnemyPerCol[colKey].transform.localPosition.y)
                        {
                            lowestEnemyPerCol[colKey] = slot.currentEnemy;
                        }
                    }
                }
            }

            if (lowestEnemyPerCol.Count == 0) return;

            List<Enemy> frontlineShooters = new List<Enemy>(lowestEnemyPerCol.Values);
            Enemy shooter = frontlineShooters[Random.Range(0, frontlineShooters.Count)];
            if (shooter == null) return;

            Vector3 spawnPos = shooter.transform.position + Vector3.down * 0.35f;
            float dynamicBulletSpeed = Mathf.Min(4.2f, 3.0f + (currentStage - 1) * 0.25f);

            // [다채로운 피격 탄환 패턴]
            // 1: 단발 수직 (|)
            // 2: 2중 피탄 사격 (/ \) - 각도 ±14°
            // 3: 3단 피탄 사격 (/ | \) - 각도 -18°, 0°, +18°
            int bulletPattern = 1;
            float roll = Random.value;

            if (shooter.Type == EnemyType.Top)
            {
                // 지휘관기: 고스테이지일수록 3단 확산 사격 빈도 대폭 증가!
                if (currentStage >= 3)
                {
                    bulletPattern = (roll < 0.60f) ? 3 : 2;
                }
                else if (currentStage == 2)
                {
                    if (roll < 0.35f) bulletPattern = 3;
                    else if (roll < 0.75f) bulletPattern = 2;
                    else bulletPattern = 1;
                }
                else
                {
                    bulletPattern = (roll < 0.40f) ? 2 : 1;
                }
            }
            else if (shooter.Type == EnemyType.Mid)
            {
                // 중형기: 2중 사격 위주 + 상위 스테이지에서 3단 사격 혼합
                if (currentStage >= 3)
                {
                    if (roll < 0.30f) bulletPattern = 3;
                    else if (roll < 0.75f) bulletPattern = 2;
                    else bulletPattern = 1;
                }
                else if (currentStage == 2)
                {
                    bulletPattern = (roll < 0.45f) ? 2 : 1;
                }
                else
                {
                    bulletPattern = (roll < 0.20f) ? 2 : 1;
                }
            }
            else // EnemyType.Bottom
            {
                // 일반 졸개: 기본 단발, 고스테이지에서 2중 부채꼴 견제
                if (currentStage >= 3)
                {
                    bulletPattern = (roll < 0.35f) ? 2 : 1;
                }
                else if (currentStage == 2)
                {
                    bulletPattern = (roll < 0.20f) ? 2 : 1;
                }
                else
                {
                    bulletPattern = 1;
                }
            }

            // 패턴별 탄환 발사
            switch (bulletPattern)
            {
                case 1: // 단발 (|)
                    FireBullet(spawnPos, Vector2.down, dynamicBulletSpeed);
                    break;

                case 2: // 2중 피탄 사격 (/ \)
                    FireBullet(spawnPos + new Vector3(-0.15f, 0, 0), Quaternion.Euler(0, 0, 14f) * Vector2.down, dynamicBulletSpeed);
                    FireBullet(spawnPos + new Vector3(0.15f, 0, 0), Quaternion.Euler(0, 0, -14f) * Vector2.down, dynamicBulletSpeed);
                    break;

                case 3: // 3단 피탄 사격 (/ | \)
                    FireBullet(spawnPos, Vector2.down, dynamicBulletSpeed);
                    FireBullet(spawnPos + new Vector3(-0.2f, 0, 0), Quaternion.Euler(0, 0, 18f) * Vector2.down, dynamicBulletSpeed);
                    FireBullet(spawnPos + new Vector3(0.2f, 0, 0), Quaternion.Euler(0, 0, -18f) * Vector2.down, dynamicBulletSpeed);
                    break;
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
            float dynamicBulletSpeed = Mathf.Min(4.2f, 3.0f + (currentStage - 1) * 0.25f);
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

            // 대열 내에 있는 적 수집
            List<Enemy> candidates = new List<Enemy>();
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                var e = activeEnemies[i];
                if (e != null && e.State == EnemyState.InFormation)
                {
                    candidates.Add(e);
                }
            }

            if (candidates.Count == 0) return;

            PlayerController player = FindAnyObjectByType<PlayerController>();
            Vector3 targetPos = (player != null) ? player.transform.position : new Vector3(0, -4.0f, 0);

            // 1기 출격
            Enemy diver1 = candidates[Random.Range(0, candidates.Count)];
            diver1.StartDive(targetPos);
            candidates.Remove(diver1);

            // Stage 3 이상에서는 35% 확률로 2기 편대 동시 급강하!
            if (currentStage >= 3 && candidates.Count > 0 && Random.value < 0.35f)
            {
                Enemy diver2 = candidates[Random.Range(0, candidates.Count)];
                diver2.StartDive(targetPos);
            }
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

            newEnemy.Setup(slot.type, GetHpForEnemy(slot.type, currentStage));
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
