using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 게임 전반에서 사용되는 상수 및 기본 밸런스 값 정의 (constants.py 대응)
    /// </summary>
    public static class GameConstants
    {
        // 화면 경계 (카메라 Orthographic Size 5 기준, 종횡비 3:4 / 9:16)
        public const float SCREEN_WIDTH_HALF = 3.5f;   // X축 좌우 이동 한계 (-3.5 ~ 3.5)
        public const float SCREEN_HEIGHT_HALF = 5.0f;  // Y축 상하 경계 (-5.0 ~ 5.0)

        // 플레이어 설정
        public const float PLAYER_SPEED = 8.0f;
        public const float PLAYER_START_Y = -4.0f;
        public const float PLAYER_SHOOT_COOLDOWN = 0.22f; // 발사 쿨다운(초)
        public const int PLAYER_MAX_BULLETS = 3;          // 화면 동시 탄환 수
        public const int PLAYER_MAX_LIVES = 3;
        public const float PLAYER_INVINCIBLE_DURATION = 1.2f;

        // 탄환 설정
        public const float PLAYER_BULLET_SPEED = 14.0f;
        public const float ENEMY_BULLET_SPEED = 6.0f;
    }
}
