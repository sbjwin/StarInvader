using UnityEngine;

namespace StarInvader
{
    public enum GameState
    {
        Title,
        Playing,
        GameOver,
        Ranking
    }

    /// <summary>
    /// 게임 전체 상태 머신, 점수 누적 및 게임 루프 관리 (main.py 대응)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("게임 상태")]
        [SerializeField] private GameState currentState = GameState.Title;

        [Header("참조 컴포넌트")]
        [SerializeField] private PlayerController player;
        [SerializeField] private EnemyFleet enemyFleet;

        private int currentScore = 0;
        private int highScore = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (RankingManager.Instance != null)
            {
                highScore = RankingManager.Instance.GetHighScore();
            }

            if (player != null)
            {
                player.OnLivesChanged += HandlePlayerLivesChanged;
                player.OnPlayerDied += HandlePlayerDied;
            }

            SetState(GameState.Title);
        }

        private void Update()
        {
            switch (currentState)
            {
                case GameState.Title:
                    if (InputHelper.IsActionPressed())
                    {
                        StartGame();
                    }
                    else if (InputHelper.IsRankingPressed())
                    {
                        ShowRanking();
                    }
                    break;

                case GameState.GameOver:
                    if (InputHelper.IsActionPressed())
                    {
                        StartGame();
                    }
                    else if (InputHelper.IsEscapePressed())
                    {
                        SetState(GameState.Title);
                    }
                    break;

                case GameState.Ranking:
                    if (InputHelper.IsEscapePressed() || InputHelper.IsActionPressed())
                    {
                        SetState(GameState.Title);
                    }
                    break;
            }
        }

        public void StartGame()
        {
            currentScore = 0;
            if (RankingManager.Instance != null)
            {
                highScore = RankingManager.Instance.GetHighScore();
            }

            if (player != null)
            {
                player.ResetPlayer();
            }

            if (enemyFleet != null)
            {
                enemyFleet.gameObject.SetActive(true);
                enemyFleet.SpawnFleet();
            }

            SetState(GameState.Playing);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateScore(currentScore, highScore);
                UIManager.Instance.UpdateLives(player != null ? player.CurrentLives : 3);
            }
        }

        public void AddScore(int score)
        {
            if (currentState != GameState.Playing) return;

            currentScore += score;
            if (currentScore > highScore)
            {
                highScore = currentScore;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateScore(currentScore, highScore);
            }
        }

        private void HandlePlayerLivesChanged(int lives)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateLives(lives);
            }
        }

        private void HandlePlayerDied()
        {
            GameOver();
        }

        public void GameOver()
        {
            bool isNewRecord = false;
            if (RankingManager.Instance != null)
            {
                isNewRecord = currentScore > 0 && currentScore >= RankingManager.Instance.GetHighScore();
                RankingManager.Instance.SaveScore(currentScore);
            }

            if (enemyFleet != null)
            {
                enemyFleet.ClearFleet();
            }

            SetState(GameState.GameOver);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowGameOver(currentScore, isNewRecord);
            }
        }

        public void ShowRanking()
        {
            SetState(GameState.Ranking);
        }

        public void SetState(GameState newState)
        {
            currentState = newState;

            if (UIManager.Instance != null)
            {
                switch (currentState)
                {
                    case GameState.Title:
                        UIManager.Instance.ShowTitle();
                        if (player != null) player.gameObject.SetActive(false);
                        if (enemyFleet != null) enemyFleet.gameObject.SetActive(false);
                        break;

                    case GameState.Playing:
                        UIManager.Instance.ShowHUD();
                        break;

                    case GameState.GameOver:
                        // UIManager.ShowGameOver()는 GameOver()에서 직접 호출
                        break;

                    case GameState.Ranking:
                        UIManager.Instance.ShowRanking();
                        break;
                }
            }
        }

        public GameState CurrentState => currentState;
    }
}
