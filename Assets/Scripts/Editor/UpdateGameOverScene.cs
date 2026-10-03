using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace StarInvader.Editor
{
    [InitializeOnLoad]
    public class UpdateGameOverScene
    {
        static UpdateGameOverScene()
        {
            EditorApplication.delayCall += RebuildGameOverScene;
        }

        [MenuItem("Star Invader/게임오버 씬 그래픽 UI 및 컨트롤러 완벽 업데이트", false, 3)]
        public static void RebuildGameOverScene()
        {
            string scenePath = "Assets/Scenes/GameOverScene.unity";
            Scene scene = EditorSceneManager.OpenScene(scenePath);

            // 기존 캔버스와 배경 정리
            GameObject oldCanvas = GameObject.Find("GameOverCanvas");
            if (oldCanvas == null) oldCanvas = GameObject.Find("Canvas");
            if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

            GameObject oldBg = GameObject.Find("Background");
            if (oldBg != null) Object.DestroyImmediate(oldBg);

            // 1. Background 설정 (우주 은하수 배경)
            GameObject bgObj = new GameObject("Background");
            SpriteRenderer bgSr = bgObj.AddComponent<SpriteRenderer>();
            string bgSpritePath = "Assets/GameAssets/images/background/bg_space.png";
            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>(bgSpritePath);
            if (bgSprite != null) bgSr.sprite = bgSprite;
            bgSr.sortingOrder = -10;
            bgObj.transform.position = Vector3.zero;

            // 2. GameOverCanvas 생성
            GameObject canvasObj = new GameObject("GameOverCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(600, 800);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem 보장
            if (GameObject.Find("EventSystem") == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // 3. 반투명 배경 패널
            GameObject panelObj = new GameObject("GameOverPanel");
            panelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRt = panelObj.AddComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero;
            panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero;
            panelRt.offsetMax = Vector2.zero;
            Image panelImg = panelObj.AddComponent<Image>();
            panelImg.color = new Color(0.02f, 0.02f, 0.06f, 0.75f);

            // 4. GAME OVER 타이틀 텍스트 (네온 레드 글로우)
            CreateUIText(panelObj.transform, "TitleText", "GAME OVER", new Vector2(0, 180), 54, new Color(1.0f, 0.15f, 0.2f), FontStyle.Bold);

            // 5. 최종 점수 텍스트 (골드/옐로우)
            Text finalScoreText = CreateUIText(panelObj.transform, "FinalScoreText", "최종 점수: 0", new Vector2(0, 60), 32, new Color(1.0f, 0.9f, 0.2f), FontStyle.Bold);

            // 6. 신기록 텍스트 (네온 마젠타)
            Text newRecordText = CreateUIText(panelObj.transform, "NewRecordText", "★ NEW RECORD! ★", new Vector2(0, 0), 28, new Color(1.0f, 0.2f, 0.8f), FontStyle.Bold);

            // 7. 조작 가이드 안내 텍스트
            CreateUIText(panelObj.transform, "RestartPromptText", "Press [ SPACE ] to Restart", new Vector2(0, -90), 22, new Color(0.85f, 0.92f, 1.0f), FontStyle.Normal);
            CreateUIText(panelObj.transform, "TitlePromptText", "[ ESC ] Title Screen", new Vector2(0, -135), 18, new Color(0.6f, 0.7f, 0.85f), FontStyle.Normal);

            // 8. GameOverController 컴포넌트 부착 및 직렬화 연결
            GameOverController gameOverCtrl = canvasObj.AddComponent<GameOverController>();
            SerializedObject serCtrl = new SerializedObject(gameOverCtrl);
            serCtrl.FindProperty("finalScoreText").objectReferenceValue = finalScoreText;
            serCtrl.FindProperty("newRecordText").objectReferenceValue = newRecordText;
            serCtrl.ApplyModifiedProperties();

            // 9. Camera 확인
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.orthographic = true;
                mainCam.orthographicSize = 5f;
                mainCam.backgroundColor = Color.black;
                if (mainCam.GetComponent<AudioListener>() == null)
                {
                    mainCam.gameObject.AddComponent<AudioListener>();
                }
            }

            // 10. Global Managers 보장
            if (GameObject.Find("GameDataManager") == null)
            {
                GameObject gdm = new GameObject("GameDataManager");
                gdm.AddComponent<GameDataManager>();
            }

            if (GameObject.Find("SoundManager") == null)
            {
                GameObject smObj = new GameObject("SoundManager");
                SoundManager sm = smObj.AddComponent<SoundManager>();
            }

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log("<color=cyan>[StarInvader]</color> GameOverScene 그래픽 UI와 GameOverController가 완벽하게 업데이트되었습니다!");
        }

        private static Text CreateUIText(Transform parent, string name, string text, Vector2 pos, int fontSize, Color color, FontStyle style = FontStyle.Normal)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(550, 80);

            Text txt = obj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.text = text;

            return txt;
        }
    }
}
