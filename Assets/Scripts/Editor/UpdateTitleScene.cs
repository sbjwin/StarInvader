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

            // 기존 TitleCanvas와 Background 제거 후 재구성
            GameObject oldCanvas = GameObject.Find("TitleCanvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

            GameObject oldBg = GameObject.Find("Background");
            if (oldBg != null) Object.DestroyImmediate(oldBg);

            // 1. Background 재구성
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

            // 3. Title Logo Image
            string logoPath = "Assets/GameAssets/images/background/title_logo.png";
            EnsureSpriteImport(logoPath);
            Sprite logoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(logoPath);

            GameObject logoObj = new GameObject("TitleLogo");
            logoObj.transform.SetParent(canvasObj.transform, false);
            RectTransform logoRt = logoObj.AddComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.5f, 0.5f);
            logoRt.anchorMax = new Vector2(0.5f, 0.5f);
            logoRt.pivot = new Vector2(0.5f, 0.5f);
            logoRt.anchoredPosition = new Vector2(0, 180);
            logoRt.sizeDelta = new Vector2(460, 220);

            Image logoImg = logoObj.AddComponent<Image>();
            logoImg.sprite = logoSprite;
            logoImg.preserveAspect = true;

            // 4. Information Box Panel (반투명 네온 박스)
            GameObject infoBox = new GameObject("InfoBoxPanel");
            infoBox.transform.SetParent(canvasObj.transform, false);
            RectTransform boxRt = infoBox.AddComponent<RectTransform>();
            boxRt.anchorMin = new Vector2(0.5f, 0.5f);
            boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.anchoredPosition = new Vector2(0, -120);
            boxRt.sizeDelta = new Vector2(490, 310);

            Image boxBg = infoBox.AddComponent<Image>();
            boxBg.color = new Color(0.03f, 0.06f, 0.18f, 0.82f); // 어두운 네온 반투명 블루

            // 박스 내부 텍스트들
            // 4-1. [ SPACE ] 키를 눌러 출격 (헤더)
            CreateUIText(infoBox.transform, "HeaderPrompt", "[ SPACE ] 키를 눌러 출격", new Vector2(0, 105), new Vector2(0.5f, 0.5f), 26, Color.white, FontStyle.Bold);

            // 4-2. 조작법
            CreateUIText(infoBox.transform, "ControlText", "조작법: [ ← / → ] 또는 [ A / D ] 키로 좌우 이동", new Vector2(0, 50), new Vector2(0.5f, 0.5f), 18, new Color(0.65f, 0.82f, 1.0f));

            // 4-3. 사격
            CreateUIText(infoBox.transform, "ShootText", "사격: [ SPACE ] 키 (레이저 발사음 효과)", new Vector2(0, 10), new Vector2(0.5f, 0.5f), 18, new Color(0.65f, 0.82f, 1.0f));

            // 4-4. 시작 생명
            CreateUIText(infoBox.transform, "LivesInfoText", "시작 생명: 3개 (우측 상단에 표시)", new Vector2(0, -30), new Vector2(0.5f, 0.5f), 18, new Color(0.4f, 1.0f, 0.7f));

            // 4-5. 랭킹 도전 안내 & [R] 키 힌트
            CreateUIText(infoBox.transform, "RankingHintText", "게임 종료 후 3글자 이니셜을 등록하여 랭킹에 도전하세요!\n[ R ] 키를 눌러 랭킹 확인", new Vector2(0, -85), new Vector2(0.5f, 0.5f), 16, new Color(1.0f, 0.75f, 0.25f));

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

            CreateUIText(rankingPanel.transform, "RankingTitle", "★ TOP 5 RANKING ★", new Vector2(0, 160), new Vector2(0.5f, 0.5f), 38, Color.yellow, FontStyle.Bold);
            Text rankingListText = CreateUIText(rankingPanel.transform, "RankingListText", "1. PLAYER - 1000\n2. PLAYER - 800", new Vector2(0, 0), new Vector2(0.5f, 0.5f), 24, Color.white);
            CreateUIText(rankingPanel.transform, "RankingCloseHint", "Press [ESC] / [SPACE] to Close", new Vector2(0, -180), new Vector2(0.5f, 0.5f), 20, Color.gray);

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
            Debug.Log("[StarInvader] TitleScene 그래픽 및 UI 구성이 원본과 동일하게 완벽 업데이트되었습니다!");
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

        private static Text CreateUIText(Transform parent, string name, string content, Vector2 anchoredPos, Vector2 anchor, int fontSize, Color color, FontStyle style = FontStyle.Normal)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(470, 60);

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
