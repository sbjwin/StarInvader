using System.Collections;
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

        private bool canAcceptInput = false;

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

            // 게임오버 직후 스페이스바 연타로 인한 즉시 재시작 방지 (0.35초 딜레이)
            StartCoroutine(EnableInputRoutine());
        }

        private IEnumerator EnableInputRoutine()
        {
            yield return new WaitForSeconds(0.35f);
            canAcceptInput = true;
        }

        private void Update()
        {
            if (!canAcceptInput) return;

            if (InputHelper.IsActionPressed())
            {
                Debug.Log("[StarInvader] GameOverScene -> Restarting GameScene...");
                RestartGame();
            }
            else if (InputHelper.IsEscapePressed())
            {
                Debug.Log("[StarInvader] GameOverScene -> Returning to TitleScene...");
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
