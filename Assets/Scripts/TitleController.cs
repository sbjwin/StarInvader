using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StarInvader
{
    /// <summary>
    /// TitleScene 전용 컨트롤러 (타이틀 UI, 랭킹 모달 팝업, 게임 시작 씬 전환)
    /// </summary>
    public class TitleController : MonoBehaviour
    {
        [Header("UI 패널 & 텍스트")]
        [SerializeField] private GameObject rankingModalPanel;
        [SerializeField] private Text rankingListText;

        private bool isRankingOpen = false;

        private void Start()
        {
            if (rankingModalPanel != null)
            {
                rankingModalPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (isRankingOpen)
            {
                if (InputHelper.IsEscapePressed() || InputHelper.IsActionPressed() || InputHelper.IsRankingPressed())
                {
                    CloseRanking();
                }
            }
            else
            {
                if (InputHelper.IsActionPressed())
                {
                    StartGame();
                }
                else if (InputHelper.IsRankingPressed())
                {
                    OpenRanking();
                }
            }
        }

        public void StartGame()
        {
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.ResetGameScore();
            }

            SceneManager.LoadScene("GameScene");
        }

        public void OpenRanking()
        {
            isRankingOpen = true;
            if (rankingModalPanel != null)
            {
                rankingModalPanel.SetActive(true);
            }
            UpdateRankingUI();
        }

        public void CloseRanking()
        {
            isRankingOpen = false;
            if (rankingModalPanel != null)
            {
                rankingModalPanel.SetActive(false);
            }
        }

        private void UpdateRankingUI()
        {
            if (rankingListText == null) return;

            if (GameDataManager.Instance == null)
            {
                rankingListText.text = "기록된 랭킹이 없습니다.";
                return;
            }

            List<ScoreEntry> entries = GameDataManager.Instance.GetTopEntries();
            if (entries == null || entries.Count == 0)
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
    }
}
