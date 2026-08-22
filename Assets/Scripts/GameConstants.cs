using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 게임 전반에서 사용되는 상수 및 기본 밸런스 값 정의 (constants.py 대응)
    /// </summary>
    public static class GameConstants
    {
        // 화면 경계 (카메라 Orthographic Size 5 기준)
        public const float SCREEN_WIDTH_HALF = 3.5f;
        public const float SCREEN_HEIGHT_HALF = 5.0f;

        // 플레이어 설정
        public const float PLAYER_SPEED = 8.0f;
        public const float PLAYER_START_Y = -4.0f;
        public const float PLAYER_SHOOT_COOLDOWN = 0.22f;
        public const int PLAYER_MAX_BULLETS = 3;
        public const int PLAYER_MAX_LIVES = 3;
        public const float PLAYER_INVINCIBLE_DURATION = 1.2f;

        // 탄환 설정
        public const float PLAYER_BULLET_SPEED = 14.0f;
        public const float ENEMY_BULLET_SPEED = 6.0f;

        // 적 편대 설정
        public const int ENEMY_ROWS = 3;
        public const int ENEMY_COLS = 8;
        public const float ENEMY_SPACING_X = 0.75f;
        public const float ENEMY_SPACING_Y = 0.65f;
        public const float ENEMY_START_Y = 3.5f;
        public const float ENEMY_BASE_SPEED_X = 1.3f;
        public const float ENEMY_DROP_DISTANCE = 0.18f;
        public const int SCORE_PER_ENEMY = 100;
        public const float INVASION_Y_LIMIT = -3.2f;

        // 랭킹 설정
        public const int MAX_RANKING_ENTRIES = 5;
    }
}
