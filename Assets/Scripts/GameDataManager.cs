using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 씬(Title <-> Game <-> GameOver) 간에 파괴되지 않고 점수, 신기록, 랭킹을 영구 유지하는 글로벌 서비스
    /// </summary>
    public class GameDataManager : MonoBehaviour
    {
        public static GameDataManager Instance { get; private set; }

        public int CurrentScore { get; set; } = 0;
        public int LastFinalScore { get; private set; } = 0;
        public bool IsNewRecord { get; private set; } = false;

        private string saveFilePath;
        private RankingData rankingData = new RankingData();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                saveFilePath = Path.Combine(Application.persistentDataPath, "ranking.json");
                LoadRanking();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void ResetGameScore()
        {
            CurrentScore = 0;
            IsNewRecord = false;
        }

        public void AddScore(int amount)
        {
            CurrentScore += amount;
        }

        public void RecordFinalScore()
        {
            LastFinalScore = CurrentScore;
            int previousHighScore = GetHighScore();
            IsNewRecord = LastFinalScore > 0 && LastFinalScore >= previousHighScore;

            if (LastFinalScore > 0)
            {
                SaveScore(LastFinalScore);
            }
        }

        public void LoadRanking()
        {
            try
            {
                if (File.Exists(saveFilePath))
                {
                    string json = File.ReadAllText(saveFilePath);
                    rankingData = JsonUtility.FromJson<RankingData>(json) ?? new RankingData();
                }
                else
                {
                    rankingData = new RankingData();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameDataManager] 랭킹 로드 실패: {ex.Message}");
                rankingData = new RankingData();
            }
        }

        public void SaveScore(int score, string playerName = "PLAYER")
        {
            rankingData.entries.Add(new ScoreEntry(playerName, score));
            rankingData.entries = rankingData.entries
                .OrderByDescending(e => e.score)
                .Take(GameConstants.MAX_RANKING_ENTRIES)
                .ToList();

            try
            {
                string json = JsonUtility.ToJson(rankingData, true);
                File.WriteAllText(saveFilePath, json);
                Debug.Log($"[GameDataManager] 랭킹 저장 완료: {score}점");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameDataManager] 랭킹 저장 실패: {ex.Message}");
            }
        }

        public int GetHighScore()
        {
            if (rankingData.entries.Count > 0)
            {
                return rankingData.entries[0].score;
            }
            return 0;
        }

        public List<ScoreEntry> GetTopEntries()
        {
            return rankingData.entries;
        }
    }
}
