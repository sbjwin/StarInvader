using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StarInvader
{
    /// <summary>
    /// HUD, 타이틀, 게임오버, 랭킹 화면 UI 제어 매니저
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("UI 패널들")]
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject titlePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject rankingPanel;

        [Header("HUD 텍스트")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text livesText;

        [Header("게임오버 텍스트")]
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text newRecordText;

        [Header("랭킹 텍스트")]
        [SerializeField] private Text rankingListText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void ShowTitle()
        {
            SetPanelActive(titlePanel);
        }

        public void ShowHUD()
        {
            SetPanelActive(hudPanel);
        }

        public void ShowGameOver(int finalScore, bool isNewRecord)
        {
            SetPanelActive(gameOverPanel);
            if (finalScoreText != null) finalScoreText.text = $"최종 점수: {finalScore}";
            if (newRecordText != null) newRecordText.gameObject.SetActive(isNewRecord);
        }

        public void ShowRanking()
        {
            SetPanelActive(rankingPanel);
            UpdateRankingText();
        }

        public void UpdateScore(int score, int highScore)
        {
            if (scoreText != null) scoreText.text = $"SCORE: {score:D5}";
            if (highScoreText != null) highScoreText.text = $"HI-SCORE: {highScore:D5}";
        }

        public void UpdateLives(int lives)
        {
            if (livesText != null)
            {
                string hearts = "";
                for (int i = 0; i < lives; i++) hearts += "♥ ";
                livesText.text = $"LIVES: {hearts.Trim()}";
            }
        }

        private void UpdateRankingText()
        {
            if (rankingListText == null || RankingManager.Instance == null) return;

            List<ScoreEntry> entries = RankingManager.Instance.GetTopEntries();
            if (entries.Count == 0)
            {
                rankingListText.text = "기록된 랭킹이 없습니다.";
                return;
            }

            string result = "";
            for (int i = 0; i < entries.Count; i++)
            {
                result += $"{i + 1}. {entries[i].name} - {entries[i].score}점 ({entries[i].date})\n";
            }
            rankingListText.text = result;
        }

        private void SetPanelActive(GameObject target)
        {
            if (hudPanel != null) hudPanel.SetActive(target == hudPanel);
            if (titlePanel != null) titlePanel.SetActive(target == titlePanel);
            if (gameOverPanel != null) gameOverPanel.SetActive(target == gameOverPanel);
            if (rankingPanel != null) rankingPanel.SetActive(target == rankingPanel);
        }
    }
}
