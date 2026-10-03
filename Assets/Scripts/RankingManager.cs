using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace StarInvader
{
    [Serializable]
    public class ScoreEntry
    {
        public string name;
        public int score;
        public int stage = 1;
        public string date;

        public ScoreEntry(string name, int score, int stage = 1)
        {
            this.name = name;
            this.score = score;
            this.stage = stage;
            this.date = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
        }
    }

    [Serializable]
    public class RankingData
    {
        public List<ScoreEntry> entries = new List<ScoreEntry>();
    }

    /// <summary>
    /// 로컬 JSON 파일 기반 최고 점수 및 랭킹 관리자 (ranking_manager.py 대응)
    /// </summary>
    public class RankingManager : MonoBehaviour
    {
        public static RankingManager Instance { get; private set; }

        private string saveFilePath;
        private RankingData rankingData = new RankingData();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            saveFilePath = Path.Combine(Application.persistentDataPath, "ranking.json");
            LoadRanking();
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
                Debug.LogWarning($"[RankingManager] 랭킹 로드 실패: {ex.Message}");
                rankingData = new RankingData();
            }
        }

        public void SaveScore(int score, string playerName = "PLAYER")
        {
            if (score <= 0) return;

            rankingData.entries.Add(new ScoreEntry(playerName, score));
            rankingData.entries = rankingData.entries
                .OrderByDescending(e => e.score)
                .Take(GameConstants.MAX_RANKING_ENTRIES)
                .ToList();

            try
            {
                string json = JsonUtility.ToJson(rankingData, true);
                File.WriteAllText(saveFilePath, json);
                Debug.Log($"[RankingManager] 랭킹 저장 완료: {score}점");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RankingManager] 랭킹 저장 실패: {ex.Message}");
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
