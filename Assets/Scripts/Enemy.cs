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

    /// <summary>
    /// 개별 적 기체 컴포넌트 (피격 시 폭발 및 사운드 연동)
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        [Header("적 속성")]
        [SerializeField] private EnemyType enemyType = EnemyType.Bottom;
        [SerializeField] private int scoreValue = GameConstants.SCORE_PER_ENEMY;
        [SerializeField] private int maxHp = 1;
        [SerializeField] private GameObject explosionPrefab;

        private int currentHp;
        public event Action<Enemy> OnDestroyed;

        private void Awake()
        {
            currentHp = maxHp;
        }

        public void Setup(EnemyType type, int score = GameConstants.SCORE_PER_ENEMY, GameObject explosion = null)
        {
            enemyType = type;
            scoreValue = score;
            if (explosion != null) explosionPrefab = explosion;
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

            // 점수 가산
            if (InGameController.Instance != null)
            {
                InGameController.Instance.AddScore(scoreValue);
            }

            OnDestroyed?.Invoke(this);
            Destroy(gameObject);
        }

        public EnemyType Type => enemyType;
        public int ScoreValue => scoreValue;
    }
}
