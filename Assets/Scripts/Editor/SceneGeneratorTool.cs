using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace StarInvader.Editor
{
    [InitializeOnLoad]
    public class SceneGeneratorTool
    {
        static SceneGeneratorTool()
        {
            // 에디터 로드 시 씬 파일이 없는 경우 최초 1회 자동 생성
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists("Assets/Scenes/TitleScene.unity"))
                {
                    GenerateAllScenes();
                }
            };
        }

        [MenuItem("Star Invader/★ 멀티 씬 자동 생성 및 빌드 세팅 구성 (Title/Game/GameOver)", false, 0)]
        public static void GenerateAllScenes()
        {
            AssetDatabase.Refresh();

            string scenesDir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            GameObject bulletPrefab = SetupBulletPrefab();
            GameObject explosionPrefab = SetupExplosionPrefab();
            GameObject enemyBulletPrefab = SetupEnemyBulletPrefab();
            GameObject topEnemyPrefab = SetupEnemyPrefab("Enemy_Top", "Assets/GameAssets/images/enemy/enemy_top.png", EnemyType.Top, Color.magenta, explosionPrefab);
            GameObject midEnemyPrefab = SetupEnemyPrefab("Enemy_Mid", "Assets/GameAssets/images/enemy/enemy_mid.png", EnemyType.Mid, new Color(1f, 0.6f, 0.2f), explosionPrefab);
            GameObject bottomEnemyPrefab = SetupEnemyPrefab("Enemy_Bottom", "Assets/GameAssets/images/enemy/enemy_bottom.png", EnemyType.Bottom, Color.yellow, explosionPrefab);

            // 1. TitleScene 생성
            CreateTitleScene();

            // 2. GameScene 생성
            CreateGameScene(bulletPrefab, topEnemyPrefab, midEnemyPrefab, bottomEnemyPrefab, enemyBulletPrefab);

            // 3. GameOverScene 생성
            CreateGameOverScene();

            // 4. Build Settings 등록
            RegisterScenesInBuildSettings();

            // 5. 최초 진입점인 TitleScene을 활성화
            EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");

            EditorUtility.DisplayDialog("Star Invader", "★ 3단 씬(TitleScene ➡️ GameScene ➡️ GameOverScene) 분리 및 자동 생성이 완료되었습니다!\n\n이제 [▶ Play] 버튼만 누르면 스페이스바로 자유롭게 씬을 넘나들며 플레이할 수 있습니다!", "확인");
        }

        private static void CreateTitleScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            SetupCamera();

            // Background
            SetupBackground();

            // Global Managers (DontDestroyOnLoad)
            GameObject gdmObj = new GameObject("GameDataManager");
            gdmObj.AddComponent<GameDataManager>();

            GameObject smObj = new GameObject("SoundManager");
            SoundManager sm = smObj.AddComponent<SoundManager>();
            string shootSoundPath = "Assets/GameAssets/sounds/laser_shoot.wav";
            AudioClip shootClip = AssetDatabase.LoadAssetAtPath<AudioClip>(shootSoundPath);
            if (shootClip != null)
            {
                SerializedObject serSm = new SerializedObject(sm);
                serSm.FindProperty("shootClip").objectReferenceValue = shootClip;
                serSm.ApplyModifiedProperties();
            }

            // Canvas & UI
            GameObject canvasObj = CreateBaseCanvas("TitleCanvas");
            SetupEventSystem();

            // Title Main Panel
            GameObject titlePanel = CreateUIPanel(canvasObj.transform, "TitlePanel");
            CreateUIText(titlePanel.transform, "TitleText", "STAR INVADER", new Vector2(0, 140), new Vector2(0.5f, 0.5f), 52, Color.cyan);
            CreateUIText(titlePanel.transform, "SubText", "Press SPACE to Start\n\n[R] View Ranking", new Vector2(0, -50), new Vector2(0.5f, 0.5f), 26, Color.white);

            // Ranking Modal Panel
            GameObject rankingPanel = CreateUIPanel(canvasObj.transform, "RankingModalPanel");
            Image rankBg = rankingPanel.AddComponent<Image>();
            rankBg.color = new Color(0.05f, 0.05f, 0.12f, 0.95f);

            CreateUIText(rankingPanel.transform, "RankingTitle", "★ TOP 5 RANKING ★", new Vector2(0, 160), new Vector2(0.5f, 0.5f), 38, Color.yellow);
            Text rankingListText = CreateUIText(rankingPanel.transform, "RankingListText", "1. PLAYER - 1000\n2. PLAYER - 800", new Vector2(0, 0), new Vector2(0.5f, 0.5f), 24, Color.white);
            CreateUIText(rankingPanel.transform, "RankingCloseHint", "Press [ESC] / [SPACE] to Close", new Vector2(0, -180), new Vector2(0.5f, 0.5f), 20, Color.gray);

            // TitleController
            TitleController titleCtrl = canvasObj.AddComponent<TitleController>();
            SerializedObject serTitle = new SerializedObject(titleCtrl);
            serTitle.FindProperty("rankingModalPanel").objectReferenceValue = rankingPanel;
            serTitle.FindProperty("rankingListText").objectReferenceValue = rankingListText;
            serTitle.ApplyModifiedProperties();

            rankingPanel.SetActive(false);

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/TitleScene.unity");
        }

        private static void CreateGameScene(GameObject bulletPrefab, GameObject topEnemy, GameObject midEnemy, GameObject bottomEnemy, GameObject enemyBullet)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera + Shake
            Camera cam = SetupCamera();
            cam.gameObject.AddComponent<CameraShake>();

            // Background
            SetupBackground();

            // Player
            GameObject playerObj = SetupPlayer(bulletPrefab);
            PlayerController playerCtrl = playerObj.GetComponent<PlayerController>();

            // EnemyFleet
            GameObject fleetObj = new GameObject("EnemyFleet");
            EnemyFleet fleet = fleetObj.AddComponent<EnemyFleet>();
            SerializedObject serFleet = new SerializedObject(fleet);
            serFleet.FindProperty("topEnemyPrefab").objectReferenceValue = topEnemy;
            serFleet.FindProperty("midEnemyPrefab").objectReferenceValue = midEnemy;
            serFleet.FindProperty("bottomEnemyPrefab").objectReferenceValue = bottomEnemy;
            serFleet.FindProperty("enemyBulletPrefab").objectReferenceValue = enemyBullet;
            serFleet.ApplyModifiedProperties();

            // Canvas & HUD
            GameObject canvasObj = CreateBaseCanvas("HUDCanvas");
            SetupEventSystem();

            GameObject hudPanel = CreateUIPanel(canvasObj.transform, "HUDPanel");
            Text scoreText = CreateUIText(hudPanel.transform, "ScoreText", "SCORE: 00000", new Vector2(25, -25), new Vector2(0, 1), 24, Color.cyan);
            Text highScoreText = CreateUIText(hudPanel.transform, "HighScoreText", "HI-SCORE: 00000", new Vector2(-25, -25), new Vector2(1, 1), 24, Color.yellow);
            Text livesText = CreateUIText(hudPanel.transform, "LivesText", "LIVES: ♥ ♥ ♥", new Vector2(25, 25), new Vector2(0, 0), 24, Color.green);

            // InGameController
            InGameController inGameCtrl = canvasObj.AddComponent<InGameController>();
            SerializedObject serInGame = new SerializedObject(inGameCtrl);
            serInGame.FindProperty("player").objectReferenceValue = playerCtrl;
            serInGame.FindProperty("enemyFleet").objectReferenceValue = fleet;
            serInGame.FindProperty("scoreText").objectReferenceValue = scoreText;
            serInGame.FindProperty("highScoreText").objectReferenceValue = highScoreText;
            serInGame.FindProperty("livesText").objectReferenceValue = livesText;
            serInGame.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
        }

        private static void CreateGameOverScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            SetupCamera();

            // Background
            SetupBackground();

            // Canvas & GameOver UI
            GameObject canvasObj = CreateBaseCanvas("GameOverCanvas");
            SetupEventSystem();

            GameObject panel = CreateUIPanel(canvasObj.transform, "GameOverPanel");
            CreateUIText(panel.transform, "GameOverHeader", "GAME OVER", new Vector2(0, 130), new Vector2(0.5f, 0.5f), 52, Color.red);
            Text finalScoreText = CreateUIText(panel.transform, "FinalScoreText", "최종 점수: 0", new Vector2(0, 40), new Vector2(0.5f, 0.5f), 30, Color.yellow);
            Text newRecordText = CreateUIText(panel.transform, "NewRecordText", "★ NEW RECORD! ★", new Vector2(0, -20), new Vector2(0.5f, 0.5f), 28, Color.magenta);
            CreateUIText(panel.transform, "PromptText", "Press SPACE to Restart\n[ESC] Title Screen", new Vector2(0, -110), new Vector2(0.5f, 0.5f), 24, Color.white);

            // GameOverController
            GameOverController goCtrl = canvasObj.AddComponent<GameOverController>();
            SerializedObject serGo = new SerializedObject(goCtrl);
            serGo.FindProperty("finalScoreText").objectReferenceValue = finalScoreText;
            serGo.FindProperty("newRecordText").objectReferenceValue = newRecordText;
            serGo.ApplyModifiedProperties();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameOverScene.unity");
        }

        private static void RegisterScenesInBuildSettings()
        {
            EditorBuildSettingsScene[] buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/TitleScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/GameScene.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/GameOverScene.unity", true)
            };
            EditorBuildSettings.scenes = buildScenes;
        }

        private static Camera SetupCamera()
        {
            GameObject camObj = new GameObject("Main Camera");
            Camera cam = camObj.AddComponent<Camera>();
            camObj.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 0, -10f);
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            return cam;
        }

        private static void SetupBackground()
        {
            GameObject bgParent = new GameObject("Background");

            string bgSpritePath = "Assets/GameAssets/images/background/bg_space.png";
            EnsureSpriteImport(bgSpritePath);
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgSpritePath);

            GameObject b1 = new GameObject("BG_Layer1");
            b1.transform.SetParent(bgParent.transform);
            b1.transform.position = Vector3.zero;
            SpriteRenderer sr1 = b1.AddComponent<SpriteRenderer>();
            sr1.sprite = bgSprite;
            sr1.sortingOrder = -10;

            GameObject b2 = new GameObject("BG_Layer2");
            b2.transform.SetParent(bgParent.transform);
            b2.transform.position = new Vector3(0, 10f, 0);
            SpriteRenderer sr2 = b2.AddComponent<SpriteRenderer>();
            sr2.sprite = bgSprite;
            sr2.sortingOrder = -10;

            BackgroundScroller scroller = bgParent.AddComponent<BackgroundScroller>();
            SerializedObject serializedBg = new SerializedObject(scroller);
            serializedBg.FindProperty("bg1").objectReferenceValue = b1.transform;
            serializedBg.FindProperty("bg2").objectReferenceValue = b2.transform;
            serializedBg.ApplyModifiedProperties();
        }

        private static GameObject CreateBaseCanvas(string canvasName)
        {
            GameObject canvasObj = new GameObject(canvasName);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(600, 800);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            return canvasObj;
        }

        private static void SetupEventSystem()
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();

#if ENABLE_INPUT_SYSTEM
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
        }

        private static GameObject CreateUIPanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return panel;
        }

        private static Text CreateUIText(Transform parent, string name, string content, Vector2 anchoredPos, Vector2 anchor, int fontSize, Color color)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(550, 160);

            Text txt = textObj.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = (anchor == new Vector2(0.5f, 0.5f)) ? TextAnchor.MiddleCenter : (anchor.x == 0 ? TextAnchor.UpperLeft : TextAnchor.UpperRight);
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            return txt;
        }

        private static GameObject SetupPlayer(GameObject bulletPrefabObj)
        {
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(0, GameConstants.PLAYER_START_Y, 0);

            SpriteRenderer sr = playerObj.AddComponent<SpriteRenderer>();
            string playerSpritePath = "Assets/GameAssets/images/player/player.png";
            EnsureSpriteImport(playerSpritePath);
            Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(playerSpritePath);
            if (playerSprite != null) sr.sprite = playerSprite;

            BoxCollider2D col = playerObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.5f, 0.4f);

            PlayerController controller = playerObj.AddComponent<PlayerController>();
            SerializedObject ser = new SerializedObject(controller);
            ser.FindProperty("bulletPrefab").objectReferenceValue = bulletPrefabObj;
            ser.ApplyModifiedProperties();

            return playerObj;
        }

        private static GameObject SetupBulletPrefab()
        {
            string prefabsDir = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir)) AssetDatabase.CreateFolder("Assets", "Prefabs");

            string path = "Assets/Prefabs/PlayerBullet.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject temp = new GameObject("PlayerBullet");
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.47f, 1.0f, 1.0f, 1.0f);
            Texture2D tex = MakeColorTexture(16, 40, Color.cyan);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 40), new Vector2(0.5f, 0.5f), 100f);

            BoxCollider2D bc = temp.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(0.16f, 0.4f);

            Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            Bullet b = temp.AddComponent<Bullet>();
            b.SetSpeed(GameConstants.PLAYER_BULLET_SPEED);
            b.SetEnemyBullet(false);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            GameObject.DestroyImmediate(temp);
            return prefab;
        }

        private static GameObject SetupEnemyBulletPrefab()
        {
            string path = "Assets/Prefabs/EnemyBullet.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject temp = new GameObject("EnemyBullet");
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.color = new Color(1.0f, 0.35f, 0.35f, 1.0f);
            Texture2D tex = MakeColorTexture(16, 40, Color.red);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 40), new Vector2(0.5f, 0.5f), 100f);

            BoxCollider2D bc = temp.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(0.16f, 0.4f);

            Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            Bullet b = temp.AddComponent<Bullet>();
            b.SetSpeed(GameConstants.ENEMY_BULLET_SPEED);
            b.SetEnemyBullet(true);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            GameObject.DestroyImmediate(temp);
            return prefab;
        }

        private static GameObject SetupEnemyPrefab(string name, string spritePath, EnemyType type, Color fallbackColor, GameObject explosionPrefab)
        {
            string path = $"Assets/Prefabs/{name}.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject temp = new GameObject(name);
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            EnsureSpriteImport(spritePath);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (s != null) sr.sprite = s;
            else
            {
                sr.color = fallbackColor;
                Texture2D tex = MakeColorTexture(32, 32, fallbackColor);
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
            }

            BoxCollider2D col = temp.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.4f, 0.32f);

            Enemy enemy = temp.AddComponent<Enemy>();
            enemy.Setup(type, GameConstants.SCORE_PER_ENEMY, explosionPrefab);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            GameObject.DestroyImmediate(temp);
            return prefab;
        }

        private static GameObject SetupExplosionPrefab()
        {
            string path = "Assets/Prefabs/ExplosionEffect.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            GameObject temp = new GameObject("ExplosionEffect");
            SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 5;
            string expSpritePath = "Assets/GameAssets/images/effects/explosion.png";
            EnsureSpriteImport(expSpritePath);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(expSpritePath);
            if (s != null) sr.sprite = s;
            else
            {
                Texture2D tex = MakeColorTexture(32, 32, Color.orange);
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 100f);
            }

            temp.AddComponent<ExplosionEffect>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
            GameObject.DestroyImmediate(temp);
            return prefab;
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
