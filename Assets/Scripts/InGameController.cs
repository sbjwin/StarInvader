using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StarInvader
{
    /// <summary>
    /// GameScene 전용 인게임 컨트롤러 (실시간 HUD 갱신, 점수 누적, 게임오버 판정 및 씬 전환)
    /// </summary>
    public class InGameController : MonoBehaviour
    {
        public static InGameController Instance { get; private set; }

        [Header("참조 컴포넌트")]
        [SerializeField] private PlayerController player;
        [SerializeField] private EnemyFleet enemyFleet;

        [Header("HUD UI")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text livesText;

        private int currentScore = 0;
        private int highScore = 0;
        private bool isGameOverTriggered = false;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (GameDataManager.Instance != null)
            {
                currentScore = GameDataManager.Instance.CurrentScore;
                highScore = GameDataManager.Instance.GetHighScore();
            }

            if (player != null)
            {
                player.OnLivesChanged += HandleLivesChanged;
                player.OnPlayerDied += HandlePlayerDied;
                UpdateLivesUI(player.CurrentLives);
            }

            UpdateScoreUI();
        }

        public void AddScore(int amount)
        {
            if (isGameOverTriggered) return;

            currentScore += amount;
            if (currentScore > highScore)
            {
                highScore = currentScore;
            }

            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.CurrentScore = currentScore;
            }

            UpdateScoreUI();
        }

        private void HandleLivesChanged(int lives)
        {
            UpdateLivesUI(lives);
        }

        private void HandlePlayerDied()
        {
            TriggerGameOver();
        }

        public void TriggerGameOver()
        {
            if (isGameOverTriggered) return;
            isGameOverTriggered = true;

            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.RecordFinalScore();
            }

            StartCoroutine(GameOverTransitionRoutine());
        }

        private IEnumerator GameOverTransitionRoutine()
        {
            yield return new WaitForSeconds(1.0f);
            SceneManager.LoadScene("GameOverScene");
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null) scoreText.text = $"SCORE: {currentScore:D5}";
            if (highScoreText != null) highScoreText.text = $"HI-SCORE: {highScore:D5}";
        }

        private void UpdateLivesUI(int lives)
        {
            if (livesText != null)
            {
                string hearts = "";
                for (int i = 0; i < lives; i++) hearts += "♥ ";
                livesText.text = $"LIVES: {hearts.Trim()}";
            }
        }
    }
}
