using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StarInvader
{
    /// <summary>
    /// GameOverScene 전용 컨트롤러 (최종 점수, 신기록 표시, 재도전 및 타이틀 씬 전환)
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [Header("UI 텍스트")]
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Text newRecordText;

        private void Start()
        {
            int finalScore = 0;
            bool isNewRecord = false;

            if (GameDataManager.Instance != null)
            {
                finalScore = GameDataManager.Instance.LastFinalScore;
                isNewRecord = GameDataManager.Instance.IsNewRecord;
            }

            if (finalScoreText != null)
            {
                finalScoreText.text = $"최종 점수: {finalScore}";
            }

            if (newRecordText != null)
            {
                newRecordText.gameObject.SetActive(isNewRecord);
            }
        }

        private void Update()
        {
            if (InputHelper.IsActionPressed())
            {
                RestartGame();
            }
            else if (InputHelper.IsEscapePressed())
            {
                GoToTitle();
            }
        }

        public void RestartGame()
        {
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.ResetGameScore();
            }
            SceneManager.LoadScene("GameScene");
        }

        public void GoToTitle()
        {
            SceneManager.LoadScene("TitleScene");
        }
    }
}
