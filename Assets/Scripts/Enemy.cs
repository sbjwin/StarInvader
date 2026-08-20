using System;
using UnityEngine;

namespace StarInvader
{
    public enum EnemyType
    {
        Top,    // 상단 (핑크)
        Mid,    // 중단 (주황)
        Bottom  // 하단 (노랑)
    }

    /// <summary>
    /// 개별 적 기체 컴포넌트 (enemy.py 대응)
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [Header("적 속성")]
        [SerializeField] private EnemyType enemyType = EnemyType.Bottom;
        [SerializeField] private int scoreValue = GameConstants.SCORE_PER_ENEMY;
        [SerializeField] private int maxHp = 1;

        private int currentHp;
        public event Action<Enemy> OnDestroyed;

        private void Awake()
        {
            currentHp = maxHp;
        }

        public void Setup(EnemyType type, int score = GameConstants.SCORE_PER_ENEMY)
        {
            enemyType = type;
            scoreValue = score;
        }

        public void TakeDamage(int damage = 1)
        {
            currentHp -= damage;
            if (currentHp <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            OnDestroyed?.Invoke(this);
            // 향후 폭발 이펙트/사운드 재생 추가 예정
            Destroy(gameObject);
        }

        public EnemyType Type => enemyType;
        public int ScoreValue => scoreValue;
    }
}
