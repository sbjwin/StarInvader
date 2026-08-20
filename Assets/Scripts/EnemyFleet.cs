using System.Collections.Generic;
using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 적 편대 전체의 이동, 하강 및 가속 관리 (enemy.py의 편대 로직 대응)
    /// </summary>
    public class EnemyFleet : MonoBehaviour
    {
        [Header("프리팹 설정")]
        [SerializeField] private GameObject topEnemyPrefab;
        [SerializeField] private GameObject midEnemyPrefab;
        [SerializeField] private GameObject bottomEnemyPrefab;

        [Header("이동 설정")]
        [SerializeField] private float baseSpeed = GameConstants.ENEMY_BASE_SPEED_X;
        [SerializeField] private float maxSpeed = 5.5f;
        [SerializeField] private float dropDistance = GameConstants.ENEMY_DROP_DISTANCE;
        [SerializeField] private float boundaryX = GameConstants.SCREEN_WIDTH_HALF - 0.3f;

        private List<Enemy> activeEnemies = new List<Enemy>();
        private int totalInitialEnemies = 0;
        private int moveDirection = 1; // 1: 우측, -1: 좌측
        private float currentSpeed;

        private void Start()
        {
            SpawnFleet();
        }

        public void SpawnFleet()
        {
            // 기존 적이 남아있다면 제거
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
                        // 임시 적 기체 생성
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

            // 1. 편대 좌우 이동
            float deltaX = moveDirection * currentSpeed * Time.deltaTime;
            bool hitBoundary = false;

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null) continue;
                Vector3 pos = enemy.transform.position;
                pos.x += deltaX;
                enemy.transform.position = pos;

                // 화면 경계 충돌 체크
                if (moveDirection > 0 && pos.x >= boundaryX)
                {
                    hitBoundary = true;
                }
                else if (moveDirection < 0 && pos.x <= -boundaryX)
                {
                    hitBoundary = true;
                }
            }

            // 2. 경계 도달 시 방향 반전 및 전체 하강
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
        }

        private void HandleEnemyDestroyed(Enemy enemy)
        {
            activeEnemies.Remove(enemy);

            // 적이 줄어들수록 이동 속도 점진적 가속 (클래식 인베이더 기믹)
            if (totalInitialEnemies > 0)
            {
                float destroyedRatio = 1f - ((float)activeEnemies.Count / totalInitialEnemies);
                currentSpeed = Mathf.Lerp(baseSpeed, maxSpeed, destroyedRatio);
            }

            // 모든 적 처치 시 웨이브 재스폰
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
