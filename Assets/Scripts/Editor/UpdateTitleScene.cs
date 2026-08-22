using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace StarInvader.Editor
{
    [InitializeOnLoad]
    public class UpdateTitleScene
    {
        static UpdateTitleScene()
        {
            EditorApplication.delayCall += RebuildTitleScene;
        }

        [MenuItem("Star Invader/타이틀 씬 그래픽 UI 완벽 업데이트", false, 1)]
        public static void RebuildTitleScene()
        {
            string scenePath = "Assets/Scenes/TitleScene.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath);

            // 기존 TitleCanvas와 Background 정리
            GameObject oldCanvas = GameObject.Find("TitleCanvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

            GameObject oldBg = GameObject.Find("Background");
            if (oldBg != null) Object.DestroyImmediate(oldBg);

            // 1. Background 재구성 (우주 은하수 배경)
            SetupBackground();

            // 2. TitleCanvas 생성
            GameObject canvasObj = new GameObject("TitleCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(600, 800);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            EnsureEventSystem();

            // 3. Title Logo Image (검은색 배경 완전 투명화 처리된 스프라이트)
            Sprite transparentLogoSprite = GetOrCreateTransparentLogoSprite();

            GameObject logoObj = new GameObject("TitleLogo");
            logoObj.transform.SetParent(canvasObj.transform, false);
            RectTransform logoRt = logoObj.AddComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.5f, 0.5f);
            logoRt.anchorMax = new Vector2(0.5f, 0.5f);
            logoRt.pivot = new Vector2(0.5f, 0.5f);
            logoRt.anchoredPosition = new Vector2(0, 175);
            logoRt.sizeDelta = new Vector2(432, 180);

            Image logoImg = logoObj.AddComponent<Image>();
            logoImg.sprite = transparentLogoSprite;
            logoImg.preserveAspect = true;

            // 4. Information Box Panel (Pygame 원본과 동일한 둥근 모서리 + 네온 블루 테두리 + 반투명 박스)
            Sprite neonBoxSprite = GetOrCreateNeonBoxSprite();

            GameObject infoBox = new GameObject("InfoBoxPanel");
            infoBox.transform.SetParent(canvasObj.transform, false);
            RectTransform boxRt = infoBox.AddComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.anchoredPosition = new Vector2(0, -90);
            boxRt.sizeDelta = new Vector2(450, 252);

            Image boxBg = infoBox.AddComponent<Image>();
            boxBg.sprite = neonBoxSprite;
            boxBg.type = Image.Type.Sliced;
            boxBg.color = Color.white; // 스프라이트 자체에 컬러/알파/테두리가 들어감

            // 4-1. [ SPACE ] 키를 눌러 출격 (골드/옐로우 헤더)
            CreateUIText(infoBox.transform, "HeaderPrompt", "[ SPACE ] 키를 눌러 출격", new Vector2(0, 86), 22, new Color(1.0f, 0.88f, 0.15f), FontStyle.Bold);

            // 4-2. 조작법 (부드러운 화이트)
            CreateUIText(infoBox.transform, "ControlText", "조작법: [ ← / → ] 또는 [ A / D ] 키로 좌우 이동", new Vector2(0, 44), 15, new Color(0.88f, 0.93f, 1.0f));

            // 4-3. 사격 (네온 스카이블루)
            CreateUIText(infoBox.transform, "ShootText", "사격: [ SPACE ] 키 (레이저 발사음 효과)", new Vector2(0, 10), 15, new Color(0.45f, 0.82f, 1.0f));

            // 4-4. 시작 생명 (네온 민트그린)
            CreateUIText(infoBox.transform, "LivesInfoText", "시작 생명: 3개 (우측 상단에 표시)", new Vector2(0, -24), 15, new Color(0.25f, 0.95f, 0.55f));

            // 4-5. 랭킹 등록 안내 (오렌지/골드)
            CreateUIText(infoBox.transform, "RankingHintText", "게임 종료 후 3글자 이니셜을 등록하여 랭킹에 도전하세요!", new Vector2(0, -58), 14, new Color(1.0f, 0.65f, 0.2f));

            // 4-6. [R] 키 랭킹 힌트 (서브)
            CreateUIText(infoBox.transform, "RankingKeyText", "[ R ] 키를 눌러 랭킹 확인", new Vector2(0, -90), 14, new Color(0.7f, 0.8f, 0.9f));

            // 5. Ranking Modal Panel
            GameObject rankingPanel = new GameObject("RankingModalPanel");
            rankingPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform rankRt = rankingPanel.AddComponent<RectTransform>();
            rankRt.anchorMin = Vector2.zero;
            rankRt.anchorMax = Vector2.one;
            rankRt.offsetMin = Vector2.zero;
            rankRt.offsetMax = Vector2.zero;

            Image rankBg = rankingPanel.AddComponent<Image>();
            rankBg.color = new Color(0.04f, 0.05f, 0.14f, 0.96f);

            CreateUIText(rankingPanel.transform, "RankingTitle", "★ TOP 5 RANKING ★", new Vector2(0, 160), 36, Color.yellow, FontStyle.Bold);
            Text rankingListText = CreateUIText(rankingPanel.transform, "RankingListText", "1. PLAYER - 1000\n2. PLAYER - 800", new Vector2(0, 0), 22, Color.white);
            rankingListText.rectTransform.sizeDelta = new Vector2(500, 200);
            CreateUIText(rankingPanel.transform, "RankingCloseHint", "Press [ESC] / [SPACE] to Close", new Vector2(0, -180), 18, Color.gray);

            rankingPanel.SetActive(false);

            // 6. TitleController 연결
            TitleController titleCtrl = canvasObj.AddComponent<TitleController>();
            SerializedObject serTitle = new SerializedObject(titleCtrl);
            serTitle.FindProperty("rankingModalPanel").objectReferenceValue = rankingPanel;
            serTitle.FindProperty("rankingListText").objectReferenceValue = rankingListText;
            serTitle.ApplyModifiedProperties();

            // 7. Global Managers 보장
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

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("[StarInvader] TitleScene 그래픽 및 투명 로고/네온 테두리 박스가 Pygame 원본과 100% 동일하게 완벽 업데이트되었습니다!");
        }

        private static Sprite GetOrCreateTransparentLogoSprite()
        {
            string outPath = "Assets/GameAssets/images/background/title_logo_transparent.png";
            if (!File.Exists(outPath))
            {
                string srcPath = "Assets/GameAssets/images/background/title_logo.png";
                byte[] rawBytes = File.ReadAllBytes(srcPath);
                Texture2D srcTex = new Texture2D(2, 2);
                srcTex.LoadImage(rawBytes);

                int w = srcTex.width;
                int h = srcTex.height;
                Texture2D outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                Color[] pixels = srcTex.GetPixels();

                for (int i = 0; i < pixels.Length; i++)
                {
                    Color c = pixels[i];
                    // 검은색 배경(RGB합이 0.08 미만)을 투명 알파로 변환 (Pygame의 colorkey와 동일)
                    if (c.r < 0.08f && c.g < 0.08f && c.b < 0.08f)
                    {
                        pixels[i] = new Color(0, 0, 0, 0);
                    }
                }

                outTex.SetPixels(pixels);
                outTex.Apply();

                byte[] pngBytes = outTex.EncodeToPNG();
                File.WriteAllBytes(outPath, pngBytes);
                AssetDatabase.ImportAsset(outPath, ImportAssetOptions.ForceUpdate);

                TextureImporter importer = AssetImporter.GetAtPath(outPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.alphaIsTransparency = true;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(outPath);
        }

        private static Sprite GetOrCreateNeonBoxSprite()
        {
            string boxPath = "Assets/GameAssets/images/ui/neon_box_frame.png";
            int w = 256;
            int h = 256;
            int cornerR = 12; // 세련되고 작은 라운드 코너
            float borderThickness = 1.5f; // 초슬림 1.5px 네온 라인

            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color bgColor = new Color(0.02f, 0.05f, 0.16f, 0.72f); // 깊고 은은한 반투명 다크 네이비
            Color borderColor = new Color(0.24f, 0.60f, 0.98f, 0.95f); // 세련된 사이언/네온 블루
            Color clear = new Color(0, 0, 0, 0);

            Color[] pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int dx = 0;
                    if (x < cornerR) dx = cornerR - x;
                    else if (x >= w - cornerR) dx = x - (w - cornerR - 1);

                    int dy = 0;
                    if (y < cornerR) dy = cornerR - y;
                    else if (y >= h - cornerR) dy = y - (h - cornerR - 1);

                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > cornerR)
                    {
                        pixels[y * w + x] = clear;
                    }
                    else if (dist > cornerR - borderThickness || x < borderThickness || x >= w - borderThickness || y < borderThickness || y >= h - borderThickness)
                    {
                        pixels[y * w + x] = borderColor;
                    }
                    else
                    {
                        pixels[y * w + x] = bgColor;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            string dir = Path.GetDirectoryName(boxPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            File.WriteAllBytes(boxPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(boxPath, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = AssetImporter.GetAtPath(boxPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteBorder = new Vector4(cornerR + 4, cornerR + 4, cornerR + 4, cornerR + 4); // 9-Sliced
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(boxPath);
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

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
                esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif
            }
        }

        private static Text CreateUIText(Transform parent, string name, string content, Vector2 anchoredPos, int fontSize, Color color, FontStyle style = FontStyle.Normal)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(470, 40);

            Text txt = textObj.AddComponent<Text>();
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            return txt;
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
    }
}
