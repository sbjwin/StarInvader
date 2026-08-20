using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

namespace StarInvader.Editor
{
    public class StarInvaderSetupTool : EditorWindow
    {
        [MenuItem("Star Invader/1단계: 플레이어 및 씬 자동 구성", false, 1)]
        public static void SetupStep1Scene()
        {
            SetupCamera();
            GameObject bulletPrefabObj = SetupBulletPrefab();
            SetupPlayer(bulletPrefabObj);

            EditorUtility.DisplayDialog("Star Invader", "1단계 (카메라, 플레이어, 탄환) 구성이 완료되었습니다!\n\n유니티 상단의 [▶ (Play)] 버튼을 눌러 테스트해 보세요.", "확인");
        }

        [MenuItem("Star Invader/2단계: 적 편대 및 충돌 시스템 구성", false, 2)]
        public static void SetupStep2Scene()
        {
            SetupStep3SceneInternal(false);
            EditorUtility.DisplayDialog("Star Invader", "2단계 구성이 완료되었습니다!\n\n유니티 상단의 [▶ (Play)] 버튼을 눌러 테스트해 보세요.", "확인");
        }

        [MenuItem("Star Invader/3단계: 적 공격 및 플레이어 라이프 시스템 구성", false, 3)]
        public static void SetupStep3Scene()
        {
            SetupStep3SceneInternal(true);
            EditorUtility.DisplayDialog("Star Invader", "3단계 구성이 완료되었습니다!\n\n유니티 상단의 [▶ (Play)] 버튼을 눌러 테스트해 보세요.", "확인");
        }

        [MenuItem("Star Invader/4단계: 사운드 및 이펙트/배경 연출 구성", false, 4)]
        public static void SetupStep4Scene()
        {
            SetupStep4SceneInternal();
            EditorUtility.DisplayDialog("Star Invader", "4단계 (사운드, 폭발 이펙트, 우주 배경 스크롤링, 피격 시 카메라 흔들림) 구성이 완료되었습니다!\n\n유니티 상단의 [▶ (Play)] 버튼을 눌러 테스트해 보세요.", "확인");
        }

        [MenuItem("Star Invader/5단계: 전체 게임 루프 및 UI/랭킹 시스템 완성 (최종)", false, 5)]
        public static void SetupStep5Scene()
        {
            // 1~4단계 요소 구성
            SetupStep4SceneInternal();

            // 랭킹 매니저
            GameObject rmObj = GameObject.Find("RankingManager");
            if (rmObj == null) rmObj = new GameObject("RankingManager");
            if (rmObj.GetComponent<RankingManager>() == null) rmObj.AddComponent<RankingManager>();

            // UI 캔버스 및 패널 생성
            GameObject canvasObj = SetupCanvasAndUI();

            // 게임 매니저 세팅
            GameObject gmObj = GameObject.Find("GameManager");
            if (gmObj == null) gmObj = new GameObject("GameManager");
            GameManager gm = gmObj.GetComponent<GameManager>();
            if (gm == null) gm = gmObj.AddComponent<GameManager>();

            PlayerController player = FindAnyObjectByType<PlayerController>();
            EnemyFleet fleet = FindAnyObjectByType<EnemyFleet>();

            SerializedObject serializedGm = new SerializedObject(gm);
            serializedGm.FindProperty("player").objectReferenceValue = player;
            serializedGm.FindProperty("enemyFleet").objectReferenceValue = fleet;
            serializedGm.ApplyModifiedProperties();

            EditorUtility.DisplayDialog("Star Invader", "★ 5단계 (전체 게임 루프, HUD/타이틀/게임오버/랭킹 UI 및 로컬 JSON 저장) 구성이 완료되었습니다!\n\n[▶ Play] 버튼을 누르고 Space 키를 눌러 게임을 시작해 보세요!", "확인");
        }

        private static void SetupStep4SceneInternal()
        {
            Camera mainCam = SetupCamera();
            if (mainCam.GetComponent<CameraShake>() == null)
            {
                mainCam.gameObject.AddComponent<CameraShake>();
            }

            SetupBackground();
            SetupSoundManager();

            GameObject explosionPrefab = SetupExplosionPrefab();
            GameObject bulletPrefabObj = SetupBulletPrefab();
            SetupPlayer(bulletPrefabObj);
            SetupStep3SceneInternal(true, explosionPrefab);
        }

        private static GameObject SetupCanvasAndUI()
        {
            GameObject canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null)
            {
                canvasObj = new GameObject("Canvas");
                Canvas canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(600, 800);
                scaler.matchWidthOrHeight = 0.5f;
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // UIManager 컴포넌트
            UIManager uiManager = canvasObj.GetComponent<UIManager>();
            if (uiManager == null) uiManager = canvasObj.AddComponent<UIManager>();

            // 1. HUD Panel
            GameObject hudPanel = CreateOrGetUIPanel(canvasObj.transform, "HUD_Panel");
            Text scoreText = CreateOrGetUIText(hudPanel.transform, "ScoreText", "SCORE: 00000", new Vector2(20, -20), new Vector2(0, 1), 24, Color.cyan);
            Text highScoreText = CreateOrGetUIText(hudPanel.transform, "HighScoreText", "HI-SCORE: 00000", new Vector2(-20, -20), new Vector2(1, 1), 24, Color.yellow);
            Text livesText = CreateOrGetUIText(hudPanel.transform, "LivesText", "LIVES: ♥ ♥ ♥", new Vector2(20, 20), new Vector2(0, 0), 24, Color.green);

            // 2. Title Panel
            GameObject titlePanel = CreateOrGetUIPanel(canvasObj.transform, "Title_Panel");
            CreateOrGetUIText(titlePanel.transform, "TitleText", "STAR INVADER", new Vector2(0, 120), new Vector2(0.5f, 0.5f), 48, Color.cyan);
            CreateOrGetUIText(titlePanel.transform, "SubText", "Press SPACE to Start\n[R] View Ranking", new Vector2(0, -60), new Vector2(0.5f, 0.5f), 24, Color.white);

            // 3. GameOver Panel
            GameObject gameOverPanel = CreateOrGetUIPanel(canvasObj.transform, "GameOver_Panel");
            CreateOrGetUIText(gameOverPanel.transform, "GameOverText", "GAME OVER", new Vector2(0, 100), new Vector2(0.5f, 0.5f), 48, Color.red);
            Text finalScoreText = CreateOrGetUIText(gameOverPanel.transform, "FinalScoreText", "최종 점수: 0", new Vector2(0, 20), new Vector2(0.5f, 0.5f), 28, Color.yellow);
            Text newRecordText = CreateOrGetUIText(gameOverPanel.transform, "NewRecordText", "★ NEW RECORD! ★", new Vector2(0, -30), new Vector2(0.5f, 0.5f), 26, Color.magenta);
            CreateOrGetUIText(gameOverPanel.transform, "RestartText", "Press SPACE to Restart\n[ESC] Title Screen", new Vector2(0, -100), new Vector2(0.5f, 0.5f), 22, Color.white);

            // 4. Ranking Panel
            GameObject rankingPanel = CreateOrGetUIPanel(canvasObj.transform, "Ranking_Panel");
            CreateOrGetUIText(rankingPanel.transform, "RankingTitleText", "TOP 5 RANKING", new Vector2(0, 150), new Vector2(0.5f, 0.5f), 40, Color.yellow);
            Text rankingListText = CreateOrGetUIText(rankingPanel.transform, "RankingListText", "1. PLAYER - 1000\n2. PLAYER - 800", new Vector2(0, 0), new Vector2(0.5f, 0.5f), 22, Color.white);
            CreateOrGetUIText(rankingPanel.transform, "RankingBackText", "Press [ESC] to Back", new Vector2(0, -180), new Vector2(0.5f, 0.5f), 20, Color.gray);

            // UIManager Serialized 연결
            SerializedObject serializedUI = new SerializedObject(uiManager);
            serializedUI.FindProperty("hudPanel").objectReferenceValue = hudPanel;
            serializedUI.FindProperty("titlePanel").objectReferenceValue = titlePanel;
            serializedUI.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            serializedUI.FindProperty("rankingPanel").objectReferenceValue = rankingPanel;

            serializedUI.FindProperty("scoreText").objectReferenceValue = scoreText;
            serializedUI.FindProperty("highScoreText").objectReferenceValue = highScoreText;
            serializedUI.FindProperty("livesText").objectReferenceValue = livesText;
            serializedUI.FindProperty("finalScoreText").objectReferenceValue = finalScoreText;
            serializedUI.FindProperty("newRecordText").objectReferenceValue = newRecordText;
            serializedUI.FindProperty("rankingListText").objectReferenceValue = rankingListText;
            serializedUI.ApplyModifiedProperties();

            return canvasObj;
        }

        private static GameObject CreateOrGetUIPanel(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) return t.gameObject;

            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return panel;
        }

        private static Text CreateOrGetUIText(Transform parent, string name, string content, Vector2 anchoredPos, Vector2 anchor, int fontSize, Color color)
        {
            Transform t = parent.Find(name);
            GameObject textObj = (t != null) ? t.gameObject : new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rt = textObj.GetComponent<RectTransform>();
            if (rt == null) rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(500, 150);

            Text txt = textObj.GetComponent<Text>();
            if (txt == null) txt = textObj.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = (anchor == new Vector2(0.5f, 0.5f)) ? TextAnchor.MiddleCenter : (anchor.x == 0 ? TextAnchor.UpperLeft : TextAnchor.UpperRight);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            return txt;
        }

        private static Camera SetupCamera()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                mainCam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            mainCam.orthographic = true;
            mainCam.orthographicSize = 5f;
            mainCam.transform.position = new Vector3(0, 0, -10f);
            mainCam.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1f);
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            return mainCam;
        }

        private static void SetupBackground()
        {
            GameObject bgParent = GameObject.Find("Background");
            if (bgParent == null) bgParent = new GameObject("Background");

            string bgSpritePath = "Assets/GameAssets/images/background/bg_space.png";
            EnsureSpriteImport(bgSpritePath);
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgSpritePath);

            Transform bg1 = bgParent.transform.Find("BG_Layer1");
            if (bg1 == null)
            {
                GameObject b1 = new GameObject("BG_Layer1");
                b1.transform.SetParent(bgParent.transform);
                b1.transform.position = Vector3.zero;
                SpriteRenderer sr1 = b1.AddComponent<SpriteRenderer>();
                sr1.sprite = bgSprite;
                sr1.sortingOrder = -10;
                bg1 = b1.transform;
            }

            Transform bg2 = bgParent.transform.Find("BG_Layer2");
            if (bg2 == null)
            {
                GameObject b2 = new GameObject("BG_Layer2");
                b2.transform.SetParent(bgParent.transform);
                b2.transform.position = new Vector3(0, 10f, 0);
                SpriteRenderer sr2 = b2.AddComponent<SpriteRenderer>();
                sr2.sprite = bgSprite;
                sr2.sortingOrder = -10;
                bg2 = b2.transform;
            }

            BackgroundScroller scroller = bgParent.GetComponent<BackgroundScroller>();
            if (scroller == null) scroller = bgParent.AddComponent<BackgroundScroller>();

            SerializedObject serializedBg = new SerializedObject(scroller);
            serializedBg.FindProperty("bg1").objectReferenceValue = bg1;
            serializedBg.FindProperty("bg2").objectReferenceValue = bg2;
            serializedBg.ApplyModifiedProperties();
        }

        private static void SetupSoundManager()
        {
            GameObject smObj = GameObject.Find("SoundManager");
            if (smObj == null) smObj = new GameObject("SoundManager");

            SoundManager sm = smObj.GetComponent<SoundManager>();
            if (sm == null) sm = smObj.AddComponent<SoundManager>();

            string shootSoundPath = "Assets/GameAssets/sounds/laser_shoot.wav";
            AudioClip shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>(shootSoundPath);

            SerializedObject serializedSm = new SerializedObject(sm);
            if (shootClip != null)
            {
                serializedSm.FindProperty("shootClip").objectReferenceValue = shootClip;
            }
            serializedSm.ApplyModifiedProperties();
        }

        private static GameObject SetupExplosionPrefab()
        {
            string prefabsDir = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            string prefabPath = "Assets/Prefabs/ExplosionEffect.prefab";
            GameObject expPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (expPrefab == null)
            {
                GameObject tempExp = new GameObject("ExplosionEffect");
                SpriteRenderer sr = tempExp.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 5;

                string expSpritePath = "Assets/GameAssets/images/effects/explosion.png";
                EnsureSpriteImport(expSpritePath);
                Sprite expSprite = AssetDatabase.LoadAssetAtPath<Sprite>(expSpritePath);
                if (expSprite != null)
                {
                    sr.sprite = expSprite;
                }
                else
                {
                    sr.color = new Color(1f, 0.7f, 0.2f, 1f);
                    Texture2D tex = MakeColorTexture(32, 32, Color.orange);
                    sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
                }

                tempExp.AddComponent<ExplosionEffect>();

                expPrefab = PrefabUtility.SaveAsPrefabAsset(tempExp, prefabPath);
                GameObject.DestroyImmediate(tempExp);
            }
            return expPrefab;
        }

        private static void SetupStep3SceneInternal(bool includeEnemyBullets, GameObject explosionPrefab = null)
        {
            SetupCamera();
            GameObject bulletPrefabObj = SetupBulletPrefab();
            SetupPlayer(bulletPrefabObj);

            GameObject topEnemyPrefab = SetupEnemyPrefab("Enemy_Top", "Assets/GameAssets/images/enemy/enemy_top.png", EnemyType.Top, Color.magenta, explosionPrefab);
            GameObject midEnemyPrefab = SetupEnemyPrefab("Enemy_Mid", "Assets/GameAssets/images/enemy/enemy_mid.png", EnemyType.Mid, new Color(1f, 0.6f, 0.2f), explosionPrefab);
            GameObject bottomEnemyPrefab = SetupEnemyPrefab("Enemy_Bottom", "Assets/GameAssets/images/enemy/enemy_bottom.png", EnemyType.Bottom, Color.yellow, explosionPrefab);
            GameObject enemyBulletPrefab = SetupEnemyBulletPrefab();

            GameObject fleetObj = GameObject.Find("EnemyFleet");
            if (fleetObj == null)
            {
                fleetObj = new GameObject("EnemyFleet");
            }

            EnemyFleet fleet = fleetObj.GetComponent<EnemyFleet>();
            if (fleet == null) fleet = fleetObj.AddComponent<EnemyFleet>();

            SerializedObject serializedFleet = new SerializedObject(fleet);
            serializedFleet.FindProperty("topEnemyPrefab").objectReferenceValue = topEnemyPrefab;
            serializedFleet.FindProperty("midEnemyPrefab").objectReferenceValue = midEnemyPrefab;
            serializedFleet.FindProperty("bottomEnemyPrefab").objectReferenceValue = bottomEnemyPrefab;
            if (includeEnemyBullets)
            {
                serializedFleet.FindProperty("enemyBulletPrefab").objectReferenceValue = enemyBulletPrefab;
            }
            serializedFleet.ApplyModifiedProperties();

            Selection.activeGameObject = fleetObj;
        }

        private static GameObject SetupBulletPrefab()
        {
            string prefabsDir = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            string bulletPrefabPath = "Assets/Prefabs/PlayerBullet.prefab";
            GameObject bulletPrefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPrefabPath);
            if (bulletPrefabObj == null)
            {
                GameObject tempBullet = new GameObject("PlayerBullet");
                SpriteRenderer bSr = tempBullet.AddComponent<SpriteRenderer>();
                bSr.color = new Color(0.47f, 1.0f, 1.0f, 1.0f);
                
                Texture2D bulletTex = MakeColorTexture(16, 40, Color.cyan);
                Sprite bulletSprite = Sprite.Create(bulletTex, new Rect(0, 0, 16, 40), new Vector2(0.5f, 0.5f), 100f);
                bSr.sprite = bulletSprite;

                BoxCollider2D bc = tempBullet.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(0.16f, 0.4f);

                Bullet b = tempBullet.AddComponent<Bullet>();
                b.SetSpeed(GameConstants.PLAYER_BULLET_SPEED);
                b.SetEnemyBullet(false);

                bulletPrefabObj = PrefabUtility.SaveAsPrefabAsset(tempBullet, bulletPrefabPath);
                GameObject.DestroyImmediate(tempBullet);
            }
            return bulletPrefabObj;
        }

        private static GameObject SetupEnemyBulletPrefab()
        {
            string prefabsDir = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            string bulletPrefabPath = "Assets/Prefabs/EnemyBullet.prefab";
            GameObject bulletPrefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(bulletPrefabPath);
            if (bulletPrefabObj == null)
            {
                GameObject tempBullet = new GameObject("EnemyBullet");
                SpriteRenderer bSr = tempBullet.AddComponent<SpriteRenderer>();
                bSr.color = new Color(1.0f, 0.35f, 0.35f, 1.0f);
                
                Texture2D bulletTex = MakeColorTexture(16, 40, Color.red);
                Sprite bulletSprite = Sprite.Create(bulletTex, new Rect(0, 0, 16, 40), new Vector2(0.5f, 0.5f), 100f);
                bSr.sprite = bulletSprite;

                BoxCollider2D bc = tempBullet.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(0.16f, 0.4f);

                Bullet b = tempBullet.AddComponent<Bullet>();
                b.SetSpeed(GameConstants.ENEMY_BULLET_SPEED);
                b.SetEnemyBullet(true);

                bulletPrefabObj = PrefabUtility.SaveAsPrefabAsset(tempBullet, bulletPrefabPath);
                GameObject.DestroyImmediate(tempBullet);
            }
            return bulletPrefabObj;
        }

        private static void SetupPlayer(GameObject bulletPrefabObj)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                playerObj = new GameObject("Player");
            }

            playerObj.transform.position = new Vector3(0, GameConstants.PLAYER_START_Y, 0);
            SpriteRenderer sr = playerObj.GetComponent<SpriteRenderer>();
            if (sr == null) sr = playerObj.AddComponent<SpriteRenderer>();

            string playerSpritePath = "Assets/GameAssets/images/player/player.png";
            EnsureSpriteImport(playerSpritePath);
            Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(playerSpritePath);
            if (playerSprite != null) sr.sprite = playerSprite;

            BoxCollider2D col = playerObj.GetComponent<BoxCollider2D>();
            if (col == null) col = playerObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.5f, 0.4f);

            PlayerController controller = playerObj.GetComponent<PlayerController>();
            if (controller == null) controller = playerObj.AddComponent<PlayerController>();

            SerializedObject serializedPlayer = new SerializedObject(controller);
            SerializedProperty bulletProp = serializedPlayer.FindProperty("bulletPrefab");
            if (bulletProp != null && bulletPrefabObj != null)
            {
                bulletProp.objectReferenceValue = bulletPrefabObj;
                serializedPlayer.ApplyModifiedProperties();
            }
        }

        private static GameObject SetupEnemyPrefab(string name, string spritePath, EnemyType type, Color fallbackColor, GameObject explosionPrefab = null)
        {
            string prefabsDir = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            string prefabPath = $"Assets/Prefabs/{name}.prefab";
            GameObject prefabObj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabObj == null)
            {
                GameObject tempEnemy = new GameObject(name);
                SpriteRenderer sr = tempEnemy.AddComponent<SpriteRenderer>();

                EnsureSpriteImport(spritePath);
                Sprite enemySprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (enemySprite != null)
                {
                    sr.sprite = enemySprite;
                }
                else
                {
                    sr.color = fallbackColor;
                    Texture2D tex = MakeColorTexture(32, 32, fallbackColor);
                    sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
                }

                BoxCollider2D col = tempEnemy.AddComponent<BoxCollider2D>();
                col.isTrigger = true;
                col.size = new Vector2(0.4f, 0.32f);

                Enemy enemy = tempEnemy.AddComponent<Enemy>();
                enemy.Setup(type, GameConstants.SCORE_PER_ENEMY, explosionPrefab);

                prefabObj = PrefabUtility.SaveAsPrefabAsset(tempEnemy, prefabPath);
                GameObject.DestroyImmediate(tempEnemy);
            }
            else if (explosionPrefab != null)
            {
                Enemy enemy = prefabObj.GetComponent<Enemy>();
                if (enemy != null)
                {
                    SerializedObject serEnemy = new SerializedObject(enemy);
                    serEnemy.FindProperty("explosionPrefab").objectReferenceValue = explosionPrefab;
                    serEnemy.ApplyModifiedProperties();
                }
            }
            return prefabObj;
        }

        private static void EnsureSpriteImport(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
        }

        private static Texture2D MakeColorTexture(int width, int height, Color col)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            tex.SetPixels(pix);
            tex.Apply();
            return tex;
        }
    }
}
