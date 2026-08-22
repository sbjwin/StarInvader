using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StarInvader.Editor
{
    [InitializeOnLoad]
    public class FixGameSceneSprites
    {
        static FixGameSceneSprites()
        {
            EditorApplication.delayCall += ProcessAllSpritesAndFixGameScene;
        }

        [MenuItem("Star Invader/인게임 스프라이트 크기 및 투명도 완벽 보정", false, 2)]
        public static void ProcessAllSpritesAndFixGameScene()
        {
            // 1. 플레이어 및 적, 이펙트 스프라이트 검은색 배경 투명화 및 PPU 설정
            ProcessSprite("Assets/GameAssets/images/player/player.png", 1450f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_top.png", 1700f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_mid.png", 1700f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_bottom.png", 1700f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_boss.png", 1100f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_ufo.png", 1600f, true);
            ProcessSprite("Assets/GameAssets/images/enemy/enemy_stealth.png", 1600f, true);
            ProcessSprite("Assets/GameAssets/images/effects/explosion.png", 1600f, true);

            // 2. Prefabs 업데이트
            UpdateEnemyPrefabs();

            // 3. GameScene 저장 및 업데이트
            UpdateGameScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("<color=cyan>[StarInvader]</color> 플레이어 및 적 기체 스프라이트 크기(PPU)와 투명 배경이 완벽하게 보정되었습니다!");
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
                    int w = tempTex.width;
                    int h = tempTex.height;
                    Color[] pixels = tempTex.GetPixels();
                    bool modified = false;

                    for (int i = 0; i < pixels.Length; i++)
                    {
                        Color c = pixels[i];
                        // 검은색 배경(RGB합이 0.08 미만)을 투명 알파로 변환
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
                importer.filterMode = FilterMode.Point; // 픽셀 아트 선명도 유지
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void UpdateEnemyPrefabs()
        {
            UpdatePrefab("Assets/Prefabs/Enemy_Top.prefab", "Assets/GameAssets/images/enemy/enemy_top.png");
            UpdatePrefab("Assets/Prefabs/Enemy_Mid.prefab", "Assets/GameAssets/images/enemy/enemy_mid.png");
            UpdatePrefab("Assets/Prefabs/Enemy_Bottom.prefab", "Assets/GameAssets/images/enemy/enemy_bottom.png");
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

            // 1. Background 재구성 (우주 은하수 배경 + 스크롤러)
            GameObject oldBg = GameObject.Find("Background");
            if (oldBg != null) Object.DestroyImmediate(oldBg);

            GameObject bgParent = new GameObject("Background");
            string bgSpritePath = "Assets/GameAssets/images/background/bg_space.png";
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
            serializedBg.FindProperty("scrollSpeed").floatValue = 1.5f;
            serializedBg.FindProperty("resetHeight").floatValue = 10f;
            serializedBg.ApplyModifiedProperties();

            // 2. Player 스프라이트 및 설정 확인
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                SpriteRenderer sr = playerObj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Sprite playerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GameAssets/images/player/player.png");
                    if (playerSprite != null) sr.sprite = playerSprite;
                    sr.sortingOrder = 5;
                }
                playerObj.transform.localScale = Vector3.one;
                playerObj.transform.position = new Vector3(0, GameConstants.PLAYER_START_Y, 0);

                BoxCollider2D col = playerObj.GetComponent<BoxCollider2D>();
                if (col != null)
                {
                    col.size = new Vector2(0.5f, 0.4f);
                    col.isTrigger = true;
                }
            }

            // 3. EnemyFleet 프리팹 재연결 확인
            GameObject fleetObj = GameObject.Find("EnemyFleet");
            if (fleetObj != null)
            {
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
                    serFleet.ApplyModifiedProperties();
                }
            }

            // 4. Global Managers 보장
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

            EditorSceneManager.SaveScene(scene, gameScenePath);
        }
    }
}
