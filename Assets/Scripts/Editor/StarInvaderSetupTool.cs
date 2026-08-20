using UnityEngine;
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
            // 1단계 요소 먼저 확인/구성
            SetupCamera();
            GameObject bulletPrefabObj = SetupBulletPrefab();
            SetupPlayer(bulletPrefabObj);

            // 2단계 적 프리팹 및 EnemyFleet 구성
            GameObject topEnemyPrefab = SetupEnemyPrefab("Enemy_Top", "Assets/GameAssets/images/enemy/enemy_top.png", EnemyType.Top, Color.magenta);
            GameObject midEnemyPrefab = SetupEnemyPrefab("Enemy_Mid", "Assets/GameAssets/images/enemy/enemy_mid.png", EnemyType.Mid, new Color(1f, 0.6f, 0.2f));
            GameObject bottomEnemyPrefab = SetupEnemyPrefab("Enemy_Bottom", "Assets/GameAssets/images/enemy/enemy_bottom.png", EnemyType.Bottom, Color.yellow);

            // EnemyFleet 오브젝트 씬에 배치
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
            serializedFleet.ApplyModifiedProperties();

            Selection.activeGameObject = fleetObj;
            EditorUtility.DisplayDialog("Star Invader", "2단계 (적 편대 24기 스폰, 좌우 이동/하강 및 탄환 충돌 피격) 구성이 완료되었습니다!\n\n유니티 상단의 [▶ (Play)] 버튼을 눌러 테스트해 보세요.", "확인");
        }

        private static void SetupCamera()
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
                bSr.color = new Color(0.47f, 1.0f, 1.0f, 1.0f); // 네온 시안
                
                Texture2D bulletTex = MakeColorTexture(16, 40, Color.cyan);
                Sprite bulletSprite = Sprite.Create(bulletTex, new Rect(0, 0, 16, 40), new Vector2(0.5f, 0.5f), 100f);
                bSr.sprite = bulletSprite;

                BoxCollider2D bc = tempBullet.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(0.16f, 0.4f);

                tempBullet.AddComponent<Bullet>();

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

        private static GameObject SetupEnemyPrefab(string name, string spritePath, EnemyType type, Color fallbackColor)
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

                Enemy enemy = tempEnemy.AddComponent<Enemy>();
                enemy.Setup(type);

                prefabObj = PrefabUtility.SaveAsPrefabAsset(tempEnemy, prefabPath);
                GameObject.DestroyImmediate(tempEnemy);
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
