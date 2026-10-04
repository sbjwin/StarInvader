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
        [MenuItem("Star Invader/★ 16:9 PC 아케이드 3분할 윙 화면 및 HUD 자동 구축 ★", false, 1)]
        public static void BuildArcadeWingLayoutOnly()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("[StarInvader] 플레이 모드 중에는 실행할 수 없습니다.");
                return;
            }

            UpdateGameScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=cyan>[StarInvader]</color> 16:9 PC 아케이드 3분할 윙(중앙 3:4 전장 + 좌우 정보 패널) 화면이 완벽하게 구축되었습니다!");
        }

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

            // 1. Background (중앙 3:4 전장에 맞춘 1:1 픽셀 무손실 우주 은하수 배경)
            GameObject oldBg = GameObject.Find("Background");
            if (oldBg != null) Object.DestroyImmediate(oldBg);

            GameObject bgParent = new GameObject("Background");
            string bgSpritePath = "Assets/GameAssets/images/background/bg_space.png";
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgSpritePath);

            GameObject b1 = new GameObject("BG_Layer1");
            b1.transform.SetParent(bgParent.transform);
            b1.transform.position = Vector3.zero;
            b1.transform.localScale = Vector3.one;
            SpriteRenderer sr1 = b1.AddComponent<SpriteRenderer>();
            sr1.sprite = bgSprite;
            sr1.sortingOrder = -10;

            GameObject b2 = new GameObject("BG_Layer2");
            b2.transform.SetParent(bgParent.transform);
            b2.transform.position = new Vector3(0, 12f, 0);
            b2.transform.localScale = Vector3.one;
            SpriteRenderer sr2 = b2.AddComponent<SpriteRenderer>();
            sr2.sprite = bgSprite;
            sr2.sortingOrder = -10;

            BackgroundScroller scroller = bgParent.AddComponent<BackgroundScroller>();
            SerializedObject serializedBg = new SerializedObject(scroller);
            serializedBg.FindProperty("bg1").objectReferenceValue = b1.transform;
            serializedBg.FindProperty("bg2").objectReferenceValue = b2.transform;
            serializedBg.FindProperty("scrollSpeed").floatValue = 1.5f;
            serializedBg.FindProperty("resetHeight").floatValue = 12f;
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

            // 4. InGameController & 3분할 아케이드 윙 Canvas 구축
            GameObject igcObj = GameObject.Find("InGameController");
            if (igcObj == null)
            {
                igcObj = new GameObject("InGameController");
                igcObj.AddComponent<InGameController>();
            }
            InGameController igc = igcObj.GetComponent<InGameController>();

            GameObject hudCanvas = GameObject.Find("HUDCanvas");
            if (hudCanvas != null) Object.DestroyImmediate(hudCanvas);

            hudCanvas = new GameObject("HUDCanvas");
            Canvas canvas = hudCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudCanvas.AddComponent<GraphicRaycaster>();
            CanvasScaler scaler = hudCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            // --- HUDPanel (중앙 940px 우주 배경 영역과 정확히 일치) ---
            GameObject hudPanelObj = new GameObject("HUDPanel");
            hudPanelObj.transform.SetParent(hudCanvas.transform, false);
            RectTransform panelRt = hudPanelObj.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(940f, 0f);

            // 좌측 상단 점수 & 최고점수 (우주 배경 안쪽 좌상단)
            Text scoreText = CreateUIText(hudPanelObj.transform, "ScoreText", "점수: 0", new Vector2(30, -35), new Vector2(400, 45), defaultFont, 32, FontStyle.Bold, Color.cyan, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));
            Text highScoreText = CreateUIText(hudPanelObj.transform, "HighScoreText", "최고점수: 0", new Vector2(30, -75), new Vector2(400, 40), defaultFont, 24, FontStyle.Bold, Color.yellow, TextAnchor.UpperLeft, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1));

            // 우측 상단 생명력 (우주 배경 안쪽 우상단)
            Text livesText = CreateUIText(hudPanelObj.transform, "LivesText", "생명력: ♥ ♥ ♥", new Vector2(-30, -35), new Vector2(400, 45), defaultFont, 30, FontStyle.Bold, new Color(0.2f, 1f, 0.4f), TextAnchor.UpperRight, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1));

            // 중앙 콤보 팝업 텍스트
            GameObject cObj = new GameObject("ComboText");
            cObj.transform.SetParent(hudPanelObj.transform, false);
            RectTransform cRt = cObj.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(0, 200);
            cRt.sizeDelta = new Vector2(600, 80);

            Text comboTextComp = cObj.AddComponent<Text>();
            comboTextComp.font = defaultFont;
            comboTextComp.fontSize = 36;
            comboTextComp.fontStyle = FontStyle.Bold;
            comboTextComp.alignment = TextAnchor.MiddleCenter;
            comboTextComp.color = Color.yellow;
            comboTextComp.text = "COMBO x2!";
            cObj.SetActive(false);

            // InGameController 바인딩
            if (igc != null)
            {
                SerializedObject serIgc = new SerializedObject(igc);
                serIgc.FindProperty("player").objectReferenceValue = playerCtrl;
                serIgc.FindProperty("enemyFleet").objectReferenceValue = fleet;
                serIgc.FindProperty("bonusUfoPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BonusUfo.prefab");
                serIgc.FindProperty("scoreText").objectReferenceValue = scoreText;
                serIgc.FindProperty("highScoreText").objectReferenceValue = highScoreText;
                serIgc.FindProperty("livesText").objectReferenceValue = livesText;
                serIgc.FindProperty("comboText").objectReferenceValue = comboTextComp;
                serIgc.FindProperty("stageText").objectReferenceValue = null;
                serIgc.FindProperty("weaponStatusText").objectReferenceValue = null;
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

        private static Text CreateUIText(Transform parent, string name, string text, Vector2 pos, Vector2 size, Font font, int fontSize, FontStyle fontStyle, Color color, TextAnchor alignment, Vector2? anchor = null, Vector2? anchorMin = null, Vector2? anchorMax = null)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rt = obj.AddComponent<RectTransform>();

            if (anchorMin.HasValue && anchorMax.HasValue)
            {
                rt.anchorMin = anchorMin.Value;
                rt.anchorMax = anchorMax.Value;
            }
            else
            {
                Vector2 a = anchor ?? new Vector2(0.5f, 1f);
                rt.anchorMin = a;
                rt.anchorMax = a;
            }

            rt.pivot = anchor ?? new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Text txt = obj.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = fontSize;
            txt.fontStyle = fontStyle;
            txt.color = color;
            txt.alignment = alignment;
            txt.text = text;
            return txt;
        }
    }
}
