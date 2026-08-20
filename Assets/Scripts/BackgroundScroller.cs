using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 우주 배경 무한 세로 스크롤 연출 (bg_space.png 대응)
    /// </summary>
    public class BackgroundScroller : MonoBehaviour
    {
        [Header("스크롤 설정")]
        [SerializeField] private float scrollSpeed = 1.2f;
        [SerializeField] private float resetHeight = 10.0f;

        [Header("배경 레이어들 (위/아래 2장)")]
        [SerializeField] private Transform bg1;
        [SerializeField] private Transform bg2;

        private void Update()
        {
            if (bg1 == null || bg2 == null) return;

            float delta = scrollSpeed * Time.deltaTime;
            bg1.position += Vector3.down * delta;
            bg2.position += Vector3.down * delta;

            // 화면 아래로 벗어난 배경을 다시 위쪽으로 재배치
            if (bg1.position.y <= -resetHeight)
            {
                bg1.position = new Vector3(bg1.position.x, bg2.position.y + resetHeight, bg1.position.z);
            }

            if (bg2.position.y <= -resetHeight)
            {
                bg2.position = new Vector3(bg2.position.x, bg1.position.y + resetHeight, bg2.position.z);
            }
        }
    }
}
