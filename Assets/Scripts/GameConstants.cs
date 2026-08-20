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

        // 적 편대 (Alien Fleet) 설정
        public const int ENEMY_ROWS = 3;                  // 편대 행 수
        public const int ENEMY_COLS = 8;                  // 편대 열 수 (총 24기)
        public const float ENEMY_SPACING_X = 0.75f;       // 가로 간격
        public const float ENEMY_SPACING_Y = 0.65f;       // 세로 간격
        public const float ENEMY_START_Y = 3.5f;          // 편대 시작 Y 위치
        public const float ENEMY_BASE_SPEED_X = 1.8f;     // 기본 이동 속도
        public const float ENEMY_DROP_DISTANCE = 0.35f;   // 벽 충돌 시 하강 거리
        public const int SCORE_PER_ENEMY = 100;           // 적 격파 기본 점수
        public const float INVASION_Y_LIMIT = -3.2f;      // 침략 한계선
    }
}
