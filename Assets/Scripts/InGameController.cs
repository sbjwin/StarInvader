using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StarInvader
{
    /// <summary>
    /// GameScene 전용 인게임 컨트롤러 (실시간 HUD 갱신, 점수 콤보 배율, 보너스 UFO 스폰, 게임오버 판정 및 씬 전환)
    /// </summary>
    public class InGameController : MonoBehaviour
    {
        public static InGameController Instance { get; private set; }

        [Header("참조 컴포넌트")]
        [SerializeField] private PlayerController player;
        [SerializeField] private EnemyFleet enemyFleet;
        [SerializeField] private GameObject bonusUfoPrefab;

        [Header("HUD UI")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text highScoreText;
        [SerializeField] private Text livesText;
        [SerializeField] private Text comboText;
        [SerializeField] private Text stageText;
        [SerializeField] private Text weaponStatusText;

        [Header("콤보 설정")]
        [SerializeField] private float comboDuration = 2.0f;

        private int currentScore = 0;
        private int highScore = 0;
        private bool isGameOverTriggered = false;

        // 플레이어 상태 캐시
        private int playerWeaponLevel = 1;
        private float playerSpGauge = 0f;
        private bool playerHasShield = false;
        private int nextExtendMilestone = 10000; // 1만점, 3만점, 6만점 등 달성 시 1UP

        // 콤보 변수
        private int currentCombo = 0;
        private float comboTimer = 0f;
        private Coroutine comboFadeCoroutine;

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

            if (player == null) player = Object.FindAnyObjectByType<PlayerController>();

            if (player != null)
            {
                player.OnLivesChanged += HandleLivesChanged;
                player.OnPlayerDied += HandlePlayerDied;
                player.OnWeaponLevelChanged += HandleWeaponLevelChanged;
                player.OnSpChanged += HandleSpChanged;
                player.OnShieldChanged += HandleShieldChanged;

                playerWeaponLevel = player.WeaponLevel;
                playerSpGauge = player.SpGauge;
                playerHasShield = player.HasShield;
                UpdateLivesUI(player.CurrentLives);
            }

            if (comboText != null)
            {
                comboText.gameObject.SetActive(false);
            }

            if (enemyFleet == null) enemyFleet = Object.FindAnyObjectByType<EnemyFleet>();
            UpdateScoreUI();

            // 보너스 UFO 스포너 시작
            StartCoroutine(UfoSpawnerRoutine());
        }

        private void Update()
        {
            // 콤보 타이머 관리
            if (currentCombo > 0)
            {
                comboTimer -= Time.deltaTime;
                if (comboTimer <= 0f)
                {
                    ResetCombo();
                }
            }
        }

        private void OnDestroy()
        {
            if (player != null)
            {
                player.OnLivesChanged -= HandleLivesChanged;
                player.OnPlayerDied -= HandlePlayerDied;
                player.OnWeaponLevelChanged -= HandleWeaponLevelChanged;
                player.OnSpChanged -= HandleSpChanged;
                player.OnShieldChanged -= HandleShieldChanged;
            }
        }

        private void HandleWeaponLevelChanged(int level)
        {
            playerWeaponLevel = level;
            UpdateLivesUI(player != null ? player.CurrentLives : 3);
        }

        private void HandleSpChanged(float sp)
        {
            playerSpGauge = sp;
            UpdateScoreUI();
        }

        private void HandleShieldChanged(bool hasShield)
        {
            playerHasShield = hasShield;
            UpdateLivesUI(player != null ? player.CurrentLives : 3);
        }

        public void AddScore(int baseAmount)
        {
            if (isGameOverTriggered) return;

            // 콤보 갱신
            currentCombo++;
            comboTimer = comboDuration;

            int multiplier = Mathf.Min(currentCombo, 5); // 최대 5배
            int finalScore = baseAmount * multiplier;

            currentScore += finalScore;

            // 적 격파 시 플레이어 SP 소폭 충전 (기본 4%, 콤보 시 최대 10%)
            if (player != null)
            {
                player.AddSp(3.5f * multiplier);
            }

            // 스코어 마일스톤 1UP 지급 (1만점, 3만점, 6만점...)
            if (currentScore >= nextExtendMilestone)
            {
                if (player != null)
                {
                    player.AddLive(1);
                }
                if (SoundManager.Instance != null)
                {
                    SoundManager.Instance.PlayBonusSound();
                }

                if (comboText != null)
                {
                    comboText.text = $"{nextExtendMilestone:N0} PTS EXTEND!\n❤️ 1UP BONUS!";
                    comboText.color = Color.cyan;
                    comboText.gameObject.SetActive(true);
                    if (comboFadeCoroutine != null) StopCoroutine(comboFadeCoroutine);
                    comboFadeCoroutine = StartCoroutine(ComboFadeRoutine());
                }

                nextExtendMilestone += (nextExtendMilestone < 30000 ? 20000 : 30000);
            }

            if (currentScore > highScore)
            {
                highScore = currentScore;
            }

            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.CurrentScore = currentScore;
            }

            // 콤보 사운드 및 UI 갱신 (2콤보 이상일 때)
            if (multiplier >= 2)
            {
                if (SoundManager.Instance != null)
                {
                    float pitch = 1.0f + (multiplier - 1) * 0.15f;
                    SoundManager.Instance.PlayComboSound(pitch);
                }
                ShowComboUI(multiplier);
            }

            UpdateScoreUI();
        }

        private void ShowComboUI(int multiplier)
        {
            if (comboText == null) return;

            comboText.text = $"COMBO x{multiplier}!";
            comboText.gameObject.SetActive(true);

            if (comboFadeCoroutine != null) StopCoroutine(comboFadeCoroutine);
            comboFadeCoroutine = StartCoroutine(ComboFadeRoutine());
        }

        private IEnumerator ComboFadeRoutine()
        {
            if (comboText == null) yield break;

            Color c = Color.yellow;
            comboText.color = c;
            yield return new WaitForSeconds(1.2f);

            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, t / 0.4f);
                comboText.color = c;
                yield return null;
            }
            comboText.gameObject.SetActive(false);
        }

        private void ResetCombo()
        {
            currentCombo = 0;
            if (comboText != null)
            {
                comboText.gameObject.SetActive(false);
            }
        }

        public void ShowBonusScorePopup(Vector3 pos, int bonusScore)
        {
            // 보너스 점수 획득 시 콤보 UI에 표시
            if (comboText != null)
            {
                comboText.text = $"+{bonusScore} BONUS!";
                comboText.color = new Color(0.2f, 1.0f, 0.5f);
                comboText.gameObject.SetActive(true);

                if (comboFadeCoroutine != null) StopCoroutine(comboFadeCoroutine);
                comboFadeCoroutine = StartCoroutine(ComboFadeRoutine());
            }
        }

        private IEnumerator UfoSpawnerRoutine()
        {
            while (!isGameOverTriggered)
            {
                // 18~28초 랜덤 대기
                float waitTime = Random.Range(18f, 28f);
                yield return new WaitForSeconds(waitTime);

                if (isGameOverTriggered) yield break;

                SpawnBonusUfo();
            }
        }

        public void SpawnBonusUfo()
        {
            // 이미 씬에 활성화된 UFO가 있으면 스킵
            if (FindAnyObjectByType<BonusUfo>() != null) return;

            int dir = Random.value > 0.5f ? 1 : -1;
            float spawnX = dir > 0 ? -GameConstants.SCREEN_WIDTH_HALF - 0.8f : GameConstants.SCREEN_WIDTH_HALF + 0.8f;
            Vector3 spawnPos = new Vector3(spawnX, 4.15f, 0);

            if (bonusUfoPrefab != null)
            {
                GameObject ufoObj = Instantiate(bonusUfoPrefab, spawnPos, Quaternion.identity);
                BonusUfo ufoComp = ufoObj.GetComponent<BonusUfo>();
                if (ufoComp != null)
                {
                    ufoComp.Initialize(dir, 3.2f);
                }
            }
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void SetDebugCombo(int comboCount)
        {
            currentCombo = comboCount;
            comboTimer = comboDuration;
            if (comboText != null)
            {
                comboText.text = $"COMBO x{currentCombo}!";
                comboText.color = Color.yellow;
                comboText.gameObject.SetActive(true);
            }
        }
#endif

        private void HandleLivesChanged(int lives)
        {
            UpdateLivesUI(lives);
        }

        
        private bool isStageTransitioning = false;

        public void HandleStageClear()
        {
            if (isGameOverTriggered || isStageTransitioning) return;
            StartCoroutine(StageClearRoutine());
        }

        private IEnumerator StageClearRoutine()
        {
            isStageTransitioning = true;

            int clearedStage = (GameDataManager.Instance != null) ? GameDataManager.Instance.CurrentStage : 1;
            int bonusScore = clearedStage * 1000;

            // 1. 화면 내 모든 적 탄환 즉시 소멸 (안전 이완)
            Bullet[] bullets = FindObjectsByType<Bullet>(FindObjectsSortMode.None);
            foreach (var b in bullets)
            {
                if (b != null && b.IsEnemyBullet)
                {
                    Destroy(b.gameObject);
                }
            }

            // 2. 승리 팡파레 SFX
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayBonusSound();
            }

            // 3. 점수 가산 (클리어 보너스)
            AddScore(bonusScore);

            // 4. 4스테이지마다 1UP 지급 (관문 돌파 회복 보상)
            bool is1Up = (clearedStage % 4 == 0);
            if (is1Up && player != null)
            {
                player.AddLive(1);
            }

            // 5. 화면 중앙 클리어 배너
            if (comboText != null)
            {
                string msg = $"STAGE {clearedStage} CLEAR!\n+{bonusScore} PTS";
                if (is1Up) msg += "\n❤️ 1UP BONUS!";
                comboText.text = msg;
                comboText.color = new Color(0.2f, 1f, 0.7f);
                comboText.gameObject.SetActive(true);
            }

            // 1.8초간 클리어 성취감 유지
            yield return new WaitForSeconds(1.8f);

            // 6. 다음 스테이지 증가
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.CurrentStage++;
            }
            int nextStage = (GameDataManager.Instance != null) ? GameDataManager.Instance.CurrentStage : clearedStage + 1;

            if (comboText != null)
            {
                comboText.text = $"STAGE {nextStage} - READY!";
                comboText.color = Color.yellow;
            }

            yield return new WaitForSeconds(0.7f);

            if (comboText != null)
            {
                comboText.gameObject.SetActive(false);
            }

            UpdateScoreUI();
            isStageTransitioning = false;

            if (enemyFleet == null) enemyFleet = Object.FindAnyObjectByType<EnemyFleet>();
            if (enemyFleet != null)
            {
                enemyFleet.SpawnFleet();
            }
        }

        private void HandlePlayerDied()
        {
            TriggerGameOver();
        }

        public void TriggerGameOver()
        {
            if (isGameOverTriggered) return;
            isGameOverTriggered = true;

            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.StopUfoSound();
            }

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
            int stage = (GameDataManager.Instance != null) ? GameDataManager.Instance.CurrentStage : 1;
            if (stageText != null) stageText.text = $"STAGE {stage:D2}";
            if (scoreText != null) scoreText.text = $"점수: {currentScore:N0}";
            if (highScoreText != null) highScoreText.text = $"최고점수: {highScore:N0}";
            UpdateLivesUI(player != null ? player.CurrentLives : 3);
        }

        private void UpdateLivesUI(int lives)
        {
            string hearts = "";
            for (int i = 0; i < lives; i++) hearts += "♥ ";
            if (string.IsNullOrEmpty(hearts)) hearts = "NONE";

            if (livesText != null)
            {
                livesText.text = $"생명력: {hearts.Trim()}";
            }

            if (weaponStatusText != null)
            {
                string wpnName = playerWeaponLevel == 1 ? "기본 레이저" : (playerWeaponLevel == 2 ? "듀얼 빔" : "3방향 확산 빔");
                string shieldBadge = playerHasShield ? "가동중 (방어)" : "비활성";
                string spStatus = (playerSpGauge >= 100f) ? "READY! (X키)" : $"{Mathf.FloorToInt(playerSpGauge)}%";
                weaponStatusText.text = $"무기: {wpnName}\n보호막: {shieldBadge}\n필살기: {spStatus}";
            }
        }
    }
}
