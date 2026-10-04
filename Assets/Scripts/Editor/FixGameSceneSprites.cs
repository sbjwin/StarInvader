using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace StarInvader.Editor
{
    public class FixGameSceneSprites
    {
        [MenuItem("Star Invader/인게임 스프라이트 크기, 탄환 및 사운드 시스템 완벽 보정", false, 2)]
        public static void ProcessAllSpritesAndFixGameScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[StarInvader] 플레이 모드 중에는 스프라이트 보정을 실행할 수 없습니다.");
                return;
            }

            // 1. 탄환 텍스처 생성 (플레이어 네온 시안 빔, 적 네온 레드 펄스 빔)
            CreateBulletSprites();

            // 2. 플레이어 및 적, 이펙트 스프라이트 검은색 배경 투명화 및 PPU 설정
            ProcessSprite("Assets/GameAssets/images/player/player.png", 1450f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_top.png", 1700f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_mid.png", 1700f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_bottom.png", 1700f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_boss.png", 1100f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_ufo.png", 1600f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_stealth.png", 1600f, true);
            ProcessSprite("Assets/GameAssets/images/effects/explosion.png", 1600f, true);

            // 3. Bullet & Enemy Prefabs 업데이트
            UpdateBulletPrefabs();
            UpdateEnemyPrefabs();

            // 4. GameScene 저장 및 컴포넌트 재연결
            UpdateGameScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=cyan>[StarInvader]</color> 플레이어/적 탄환 스프라이트 생성, 프리팹 연결 및 인게임 시스템이 완벽하게 갱신되었습니다!");
        }

        private static void CreateBulletSprites()
        {
            string dir = "Assets/GameAssets/images/effects";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            // 플레이어 탄환 스프라이트 (8x24 픽셀 네온 시안/화이트 빔)
            string playerBulletPath = "Assets/GameAssets/images/effects/bullet_player.png";
            if (!File.Exists(playerBulletPath))
            {
                Texture2D tex = new Texture2D(8, 24, TextureFormat.RGBA32, false);
                Color cyanGlow = new Color(0.0f, 0.95f, 1.0f, 0.9f);
                Color whiteCore = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                Color outerGlow = new Color(0.0f, 0.5f, 1.0f, 0.4f);

                for (int y = 0; y < 24; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        if (x >= 3 && x <= 4 && y >= 3 && y <= 20)
                            tex.SetPixel(x, y, whiteCore);
                        else if (x >= 2 && x <= 5 && y >= 1 && y <= 22)
                            tex.SetPixel(x, y, cyanGlow);
                        else if (x >= 1 && x <= 6)
                            tex.SetPixel(x, y, outerGlow);
                        else
                            tex.SetPixel(x, y, Color.clear);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(playerBulletPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            ProcessSprite(playerBulletPath, 50f, false);

            // 적 탄환 스프라이트 (8x24 픽셀 네온 레드/오렌지 지그재그 빔)
            string enemyBulletPath = "Assets/GameAssets/images/effects/bullet_enemy.png";
            if (!File.Exists(enemyBulletPath))
            {
                Texture2D tex = new Texture2D(8, 24, TextureFormat.RGBA32, false);
                Color redGlow = new Color(1.0f, 0.15f, 0.2f, 0.95f);
                Color yellowCore = new Color(1.0f, 0.9f, 0.3f, 1.0f);
                Color outerGlow = new Color(1.0f, 0.3f, 0.0f, 0.4f);

                for (int y = 0; y < 24; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        // 펄스 지그재그 형태
                        int waveOffset = ((y / 4) % 2 == 0) ? 0 : 1;
                        int centerX = 3 + waveOffset;

                        if (x == centerX && y >= 2 && y <= 21)
                            tex.SetPixel(x, y, yellowCore);
                        else if (Mathf.Abs(x - centerX) <= 1 && y >= 1 && y <= 22)
                            tex.SetPixel(x, y, redGlow);
                        else if (Mathf.Abs(x - centerX) <= 2)
                            tex.SetPixel(x, y, outerGlow);
                        else
                            tex.SetPixel(x, y, Color.clear);
                    }
                }
                tex.Apply();
                File.WriteAllBytes(enemyBulletPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
            }
            ProcessSprite(enemyBulletPath, 50f, false);
        }

        private static void ProcessSprite(string assetPath, float ppu, bool removeBlackBg)
        {
            if (!File.Exists(assetPath)) return;

            if (removeBlackBg)
            {
                byte[] rawBytes = File.ReadAllBytes(assetPath);
                Texture2D tempTex = new Texture2D(2, 2);
                if (tempTex.LoadImage(rawBytes))
                {
                    Color[] pixels = tempTex.GetPixels();
                    bool modified = false;

                    for (int i = 0; i < pixels.Length; i++)
                    {
                        Color c = pixels[i];
                        if (c.a > 0.5f && c.r < 0.08f && c.g < 0.08f && c.b < 0.08f)
                        {
                            pixels[i] = new Color(0, 0, 0, 0);
                            modified = true;
                        }
                    }

                    if (modified)
                    {
                        tempTex.SetPixels(pixels);
                        tempTex.Apply();
                        File.WriteAllBytes(assetPath, tempTex.EncodeToPNG());
                    }
                    Object.DestroyImmediate(tempTex);
                }
            }

            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = ppu;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void UpdateBulletPrefabs()
        {
            // PlayerBullet.prefab
            string pbPath = "Assets/Prefabs/PlayerBullet.prefab";
            GameObject pbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pbPath);
            Sprite pbSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/images/effects/bullet_player.png");

            if (pbPrefab != null && pbSprite != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(pbPrefab) as GameObject;
                if (instance != null)
                {
                    SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = pbSprite;
                        sr.color = Color.white;
                        sr.sortingOrder = 10;
                    }
                    BoxCollider2D col = instance.GetComponent<BoxCollider2D>();
                    if (col != null)
                    {
                        col.size = new Vector2(0.18f, 0.45f);
                        col.isTrigger = true;
                    }
                    Bullet bullet = instance.GetComponent<Bullet>();
                    if (bullet != null)
                    {
                        SerializedObject sObj = new SerializedObject(bullet);
                        sObj.FindProperty("isEnemyBullet").boolValue = false;
                        sObj.FindProperty("speed").floatValue = GameConstants.PLAYER_BULLET_SPEED;
                        sObj.ApplyModifiedProperties();
                    }
                    PrefabUtility.SaveAsPrefabAsset(instance, pbPath);
                    Object.DestroyImmediate(instance);
                }
            }

            // EnemyBullet.prefab
            string ebPath = "Assets/Prefabs/EnemyBullet.prefab";
            GameObject ebPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ebPath);
            Sprite ebSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/images/effects/bullet_enemy.png");

            if (ebPrefab != null && ebSprite != null)
            {
                GameObject instance = PrefabUtility.InstantiatePrefab(ebPrefab) as GameObject;
                if (instance != null)
                {
                    SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = ebSprite;
                        sr.color = Color.white;
                        sr.sortingOrder = 10;
                    }
                    BoxCollider2D col = instance.GetComponent<BoxCollider2D>();
                    if (col != null)
                    {
                        col.size = new Vector2(0.18f, 0.45f);
                        col.isTrigger = true;
                    }
                    Bullet bullet = instance.GetComponent<Bullet>();
                    if (bullet != null)
                    {
                        SerializedObject sObj = new SerializedObject(bullet);
                        sObj.FindProperty("isEnemyBullet").boolValue = true;
                        sObj.FindProperty("speed").floatValue = GameConstants.ENEMY_BULLET_SPEED;
                        sObj.ApplyModifiedProperties();
                    }
                    PrefabUtility.SaveAsPrefabAsset(instance, ebPath);
                    Object.DestroyImmediate(instance);
                }
            }
        }

        private static void UpdateEnemyPrefabs()
        {
            UpdatePrefab("Assets/Prefabs/Enemy_Top.prefab", "Assets/GameAssets/images/enemy/enemy_top.png");
            UpdatePrefab("Assets/Prefabs/Enemy_Mid.prefab", "Assets/GameAssets/images/enemy/enemy_mid.png");
            UpdatePrefab("Assets/Prefabs/Enemy_Bottom.prefab", "Assets/GameAssets/images/enemy/enemy_bottom.png");
            UpdatePrefab("Assets/Prefabs/BonusUfo.prefab", "Assets/GameAssets/images/enemy/enemy_ufo.png");
            UpdatePrefab("Assets/Prefabs/ExplosionEffect.prefab", "Assets/GameAssets/images/effects/explosion.png");
        }

        private static void UpdatePrefab(string prefabPath, string spritePath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite == null) return;

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance != null)
            {
                SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.sprite = sprite;
                    sr.sortingOrder = 5;
                }
                instance.transform.localScale = Vector3.one;

                PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                Object.DestroyImmediate(instance);
            }
        }

        private static void UpdateGameScene()
        {
            string gameScenePath = "Assets/Scenes/GameScene.unity";
            Scene scene = EditorSceneManager.OpenScene(gameScenePath, OpenSceneMode.Single);

            // 1. Background (우주 은하수 배경 + 스크롤러)
            GameObject oldBg = GameObject.Find("Background");
            if (oldBg != null) Object.DestroyImmediate(oldBg);

            GameObject bgParent = new GameObject("Background");
            string bgSpritePath = "Assets/GameAssets/images/background/bg_space.png";
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgSpritePath);

            GameObject b1 = new GameObject("BG_Layer1");
            b1.transform.SetParent(bgParent.transform);
            b1.transform.position = Vector3.zero;
            b1.transform.localScale = new Vector3(2.3f, 1f, 1f);
            SpriteRenderer sr1 = b1.AddComponent<SpriteRenderer>();
            sr1.sprite = bgSprite;
            sr1.sortingOrder = -10;

            GameObject b2 = new GameObject("BG_Layer2");
            b2.transform.SetParent(bgParent.transform);
            b2.transform.position = new Vector3(0, 10f, 0);
            b2.transform.localScale = new Vector3(2.3f, 1f, 1f);
            SpriteRenderer sr2 = b2.AddComponent<SpriteRenderer>();
            sr2.sprite = bgSprite;
            sr2.sortingOrder = -10;

            BackgroundScroller scroller = bgParent.AddComponent<BackgroundScroller>();
            SerializedObject serializedBg = new SerializedObject(scroller);
            serializedBg.FindProperty("bg1").objectReferenceValue = b1.transform;
            serializedBg.FindProperty("bg2").objectReferenceValue = b2.transform;
            serializedBg.FindProperty("scrollSpeed").floatValue = 1.5f;
            serializedBg.FindProperty("resetHeight").floatValue = 10f;
            serializedBg.ApplyModifiedProperties();

            // 2. Player 설정 및 PlayerBullet 프리팹 연결
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                playerObj = new GameObject("Player");
                playerObj.AddComponent<SpriteRenderer>();
                playerObj.AddComponent<BoxCollider2D>();
                playerObj.AddComponent<PlayerController>();
            }

            SpriteRenderer playerSr = playerObj.GetComponent<SpriteRenderer>();
            if (playerSr != null)
            {
                Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/images/player/player.png");
                if (playerSprite != null) playerSr.sprite = playerSprite;
                playerSr.sortingOrder = 5;
            }
            playerObj.transform.localScale = Vector3.one;
            playerObj.transform.position = new Vector3(0, GameConstants.PLAYER_START_Y, 0);

            BoxCollider2D pCol = playerObj.GetComponent<BoxCollider2D>();
            if (pCol != null)
            {
                pCol.size = new Vector2(0.5f, 0.4f);
                pCol.isTrigger = true;
            }

            PlayerController playerCtrl = playerObj.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                SerializedObject serPlayer = new SerializedObject(playerCtrl);
                GameObject pbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PlayerBullet.prefab");
                serPlayer.FindProperty("bulletPrefab").objectReferenceValue = pbPrefab;
                serPlayer.FindProperty("moveSpeed").floatValue = GameConstants.PLAYER_SPEED;
                serPlayer.FindProperty("shootCooldown").floatValue = GameConstants.PLAYER_SHOOT_COOLDOWN;
                serPlayer.FindProperty("maxConcurrentBullets").intValue = GameConstants.PLAYER_MAX_BULLETS;
                serPlayer.ApplyModifiedProperties();
            }

            // 3. EnemyFleet 프리팹 재연결
            GameObject fleetObj = GameObject.Find("EnemyFleet");
            if (fleetObj == null)
            {
                fleetObj = new GameObject("EnemyFleet");
                fleetObj.AddComponent<EnemyFleet>();
            }

            fleetObj.transform.position = Vector3.zero;
            fleetObj.transform.localScale = Vector3.one;

            EnemyFleet fleet = fleetObj.GetComponent<EnemyFleet>();
            if (fleet != null)
            {
                SerializedObject serFleet = new SerializedObject(fleet);
                serFleet.FindProperty("topEnemyPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Top.prefab");
                serFleet.FindProperty("midEnemyPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Mid.prefab");
                serFleet.FindProperty("bottomEnemyPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Bottom.prefab");
                serFleet.FindProperty("enemyBulletPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/EnemyBullet.prefab");
                serFleet.FindProperty("baseSpeed").floatValue = GameConstants.ENEMY_BASE_SPEED_X;
                serFleet.FindProperty("maxSpeed").floatValue = 3.6f;
                serFleet.FindProperty("dropDistance").floatValue = GameConstants.ENEMY_DROP_DISTANCE;
                serFleet.FindProperty("invasionYLimit").floatValue = GameConstants.INVASION_Y_LIMIT;
                serFleet.FindProperty("shootIntervalMin").floatValue = 0.8f;
                serFleet.FindProperty("shootIntervalMax").floatValue = 2.0f;
                serFleet.ApplyModifiedProperties();
            }

            // 4. InGameController & HUD Canvas
            GameObject igcObj = GameObject.Find("InGameController");
            if (igcObj == null)
            {
                igcObj = new GameObject("InGameController");
                igcObj.AddComponent<InGameController>();
            }
            InGameController igc = igcObj.GetComponent<InGameController>();

            // HUD Canvas 및 16:9 PC 와이드 최적화
            GameObject hudCanvas = GameObject.Find("HUDCanvas");
            Text comboTextComp = null;
            if (hudCanvas != null)
            {
                CanvasScaler scaler = hudCanvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 0.5f;
                }

                // ScoreText 16:9 상단 좌측
                GameObject scoreObj = GameObject.Find("ScoreText");
                if (scoreObj != null)
                {
                    RectTransform sRt = scoreObj.GetComponent<RectTransform>();
                    sRt.anchorMin = new Vector2(0, 1);
                    sRt.anchorMax = new Vector2(0, 1);
                    sRt.pivot = new Vector2(0, 1);
                    sRt.anchoredPosition = new Vector2(40, -30);
                    sRt.sizeDelta = new Vector2(650, 50);
                    Text sText = scoreObj.GetComponent<Text>();
                    if (sText != null) { sText.fontSize = 30; sText.fontStyle = FontStyle.Bold; }
                }

                // HighScoreText 16:9 상단 우측
                GameObject hiScoreObj = GameObject.Find("HighScoreText");
                if (hiScoreObj != null)
                {
                    RectTransform hRt = hiScoreObj.GetComponent<RectTransform>();
                    hRt.anchorMin = new Vector2(1, 1);
                    hRt.anchorMax = new Vector2(1, 1);
                    hRt.pivot = new Vector2(1, 1);
                    hRt.anchoredPosition = new Vector2(-40, -30);
                    hRt.sizeDelta = new Vector2(450, 50);
                    Text hText = hiScoreObj.GetComponent<Text>();
                    if (hText != null) { hText.fontSize = 30; hText.fontStyle = FontStyle.Bold; hText.alignment = TextAnchor.MiddleRight; }
                }

                // LivesText 16:9 하단 전폭
                GameObject livesObj = GameObject.Find("LivesText");
                if (livesObj != null)
                {
                    RectTransform lRt = livesObj.GetComponent<RectTransform>();
                    lRt.anchorMin = new Vector2(0, 0);
                    lRt.anchorMax = new Vector2(1, 0);
                    lRt.pivot = new Vector2(0, 0);
                    lRt.anchoredPosition = new Vector2(40, 30);
                    lRt.sizeDelta = new Vector2(-80, 50);
                    Text lText = livesObj.GetComponent<Text>();
                    if (lText != null) { lText.fontSize = 28; lText.fontStyle = FontStyle.Bold; }
                }

                Transform comboTr = hudCanvas.transform.Find("ComboText");
                if (comboTr == null)
                {
                    GameObject cObj = new GameObject("ComboText");
                    cObj.transform.SetParent(hudCanvas.transform, false);
                    RectTransform cRt = cObj.AddComponent<RectTransform>();
                    cRt.anchorMin = new Vector2(0.5f, 0.5f);
                    cRt.anchorMax = new Vector2(0.5f, 0.5f);
                    cRt.pivot = new Vector2(0.5f, 0.5f);
                    cRt.anchoredPosition = new Vector2(0, 180);
                    cRt.sizeDelta = new Vector2(800, 120);

                    comboTextComp = cObj.AddComponent<Text>();
                    comboTextComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
                    comboTextComp.fontSize = 36;
                    comboTextComp.fontStyle = FontStyle.Bold;
                    comboTextComp.alignment = TextAnchor.MiddleCenter;
                    comboTextComp.color = Color.yellow;
                    comboTextComp.text = "COMBO x2!";
                    cObj.SetActive(false);
                }
                else
                {
                    comboTextComp = comboTr.GetComponent<Text>();
                }
            }

            if (igc != null)
            {
                SerializedObject serIgc = new SerializedObject(igc);
                serIgc.FindProperty("player").objectReferenceValue = playerCtrl;
                serIgc.FindProperty("enemyFleet").objectReferenceValue = fleet;
                serIgc.FindProperty("bonusUfoPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BonusUfo.prefab");

                // UI Text 매핑
                GameObject scoreObj = GameObject.Find("ScoreText");
                GameObject hiScoreObj = GameObject.Find("HighScoreText");
                GameObject livesObj = GameObject.Find("LivesText");

                if (scoreObj != null) serIgc.FindProperty("scoreText").objectReferenceValue = scoreObj.GetComponent<Text>();
                if (hiScoreObj != null) serIgc.FindProperty("highScoreText").objectReferenceValue = hiScoreObj.GetComponent<Text>();
                if (livesObj != null) serIgc.FindProperty("livesText").objectReferenceValue = livesObj.GetComponent<Text>();
                if (comboTextComp != null) serIgc.FindProperty("comboText").objectReferenceValue = comboTextComp;

                serIgc.ApplyModifiedProperties();
            }

            // 5. Global Managers 보장
            if (GameObject.Find("GameDataManager") == null)
            {
                GameObject gdm = new GameObject("GameDataManager");
                gdm.AddComponent<GameDataManager>();
            }

            if (GameObject.Find("SoundManager") == null)
            {
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
            }

            // 6. Camera 확인
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5f;
                mainCam.backgroundColor = Color.black;
                if (mainCam.GetComponent<CameraShake>() == null)
                {
                    mainCam.gameObject.AddComponent<CameraShake>();
                }
                if (mainCam.GetComponent<AudioListener>() == null)
                {
                    mainCam.gameObject.AddComponent<AudioListener>();
                }
            }

            EditorSceneManager.SaveScene(scene, gameScenePath);
        }
    }
}
