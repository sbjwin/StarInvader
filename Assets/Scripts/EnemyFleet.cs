using System.Collections.Generic;
using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 적 편대 이동, 속도 가속, 반격 사격 및 침략 한계선 관리 (enemy.py 대응)
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
        [SerializeField] private float maxSpeed = 5.5f;
        [SerializeField] private float dropDistance = GameConstants.ENEMY_DROP_DISTANCE;
        [SerializeField] private float boundaryX = GameConstants.SCREEN_WIDTH_HALF - 0.3f;
        [SerializeField] private float invasionYLimit = GameConstants.INVASION_Y_LIMIT;

        [Header("사격 설정")]
        [SerializeField] private float shootIntervalMin = 0.8f;
        [SerializeField] private float shootIntervalMax = 2.0f;

        private List<Enemy> activeEnemies = new List<Enemy>();
        private int totalInitialEnemies = 0;
        private int moveDirection = 1; // 1: 우측, -1: 좌측
        private float currentSpeed;
        private float nextShootTime = 0f;

        private void Start()
        {
            SpawnFleet();
            ScheduleNextShot();
        }

        public void SpawnFleet()
        {
            ClearFleet();

            int rows = GameConstants.ENEMY_ROWS;
            int cols = GameConstants.ENEMY_COLS;
            float spacingX = GameConstants.ENEMY_SPACING_X;
            float spacingY = GameConstants.ENEMY_SPACING_Y;
            float startY = GameConstants.ENEMY_START_Y;

            float totalWidth = (cols - 1) * spacingX;
            float startX = -totalWidth / 2f;

            for (int r = 0; r < rows; r++)
            {
                EnemyType rowType = (r == 0) ? EnemyType.Top : (r == 1 ? EnemyType.Mid : EnemyType.Bottom);
                GameObject prefab = GetPrefabForType(rowType);

                for (int c = 0; c < cols; c++)
                {
                    Vector3 spawnPos = new Vector3(startX + (c * spacingX), startY - (r * spacingY), 0);
                    GameObject enemyObj = null;

                    if (prefab != null)
                    {
                        enemyObj = Instantiate(prefab, spawnPos, Quaternion.identity, transform);
                    }
                    else
                    {
                        enemyObj = CreateFallbackEnemy(rowType, spawnPos);
                    }

                    Enemy enemyComp = enemyObj.GetComponent<Enemy>();
                    if (enemyComp == null) enemyComp = enemyObj.AddComponent<Enemy>();
                    enemyComp.Setup(rowType);
                    enemyComp.OnDestroyed += HandleEnemyDestroyed;

                    activeEnemies.Add(enemyComp);
                }
            }

            totalInitialEnemies = activeEnemies.Count;
            currentSpeed = baseSpeed;
            moveDirection = 1;
        }

        private void Update()
        {
            if (activeEnemies.Count == 0) return;

            // 1. 편대 이동
            float deltaX = moveDirection * currentSpeed * Time.deltaTime;
            bool hitBoundary = false;
            bool reachedInvasionLimit = false;

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null) continue;
                Vector3 pos = enemy.transform.position;
                pos.x += deltaX;
                enemy.transform.position = pos;

                // 좌우 경계 도달 확인
                if (moveDirection > 0 && pos.x >= boundaryX) hitBoundary = true;
                else if (moveDirection < 0 && pos.x <= -boundaryX) hitBoundary = true;

                // 침략 한계선 도달 확인
                if (pos.y <= invasionYLimit) reachedInvasionLimit = true;
            }

            // 2. 방향 반전 및 하강
            if (hitBoundary)
            {
                moveDirection *= -1;
                foreach (var enemy in activeEnemies)
                {
                    if (enemy == null) continue;
                    Vector3 pos = enemy.transform.position;
                    pos.y -= dropDistance;
                    enemy.transform.position = pos;
                }
            }

            // 3. 침략선 돌파 판정
            if (reachedInvasionLimit)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                if (player != null && player.gameObject.activeSelf)
                {
                    Debug.LogWarning("[외계인 침략 성공] 적이 방어선을 뚫었습니다!");
                    player.TakeDamage(999); // 즉시 패배 처리
                }
            }

            // 4. 적 반격 사격
            if (Time.time >= nextShootTime)
            {
                ShootRandomEnemyBullet();
                ScheduleNextShot();
            }
        }

        private void ShootRandomEnemyBullet()
        {
            List<Enemy> bottomEnemies = GetBottomEnemies();
            if (bottomEnemies.Count == 0) return;

            Enemy shooter = bottomEnemies[Random.Range(0, bottomEnemies.Count)];
            if (shooter != null && enemyBulletPrefab != null)
            {
                Vector3 spawnPos = shooter.transform.position + Vector3.down * 0.3f;
                Instantiate(enemyBulletPrefab, spawnPos, Quaternion.identity);
            }
        }

        private List<Enemy> GetBottomEnemies()
        {
            // 각 열에서 가장 아래쪽에 있는 적만 선별 (스페이스 인베이더 규칙)
            Dictionary<int, Enemy> columnBottomMap = new Dictionary<int, Enemy>();

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null) continue;
                // X좌표를 반올림하여 열(Column) 식별
                int colKey = Mathf.RoundToInt(enemy.transform.position.x * 10f);

                if (!columnBottomMap.ContainsKey(colKey) || enemy.transform.position.y < columnBottomMap[colKey].transform.position.y)
                {
                    columnBottomMap[colKey] = enemy;
                }
            }

            return new List<Enemy>(columnBottomMap.Values);
        }

        private void ScheduleNextShot()
        {
            nextShootTime = Time.time + Random.Range(shootIntervalMin, shootIntervalMax);
        }

        private void HandleEnemyDestroyed(Enemy enemy)
        {
            activeEnemies.Remove(enemy);

            if (totalInitialEnemies > 0)
            {
                float destroyedRatio = 1f - ((float)activeEnemies.Count / totalInitialEnemies);
                currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, destroyedRatio);
            }

            if (activeEnemies.Count == 0)
            {
                Invoke(nameof(SpawnFleet), 1.0f);
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
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null) Destroy(enemy.gameObject);
            }
            activeEnemies.Clear();
        }
    }
}
