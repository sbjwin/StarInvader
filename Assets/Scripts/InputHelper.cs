using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarInvader
{
    /// <summary>
    /// Unity Old Input Manager와 New Input System을 모두 완벽 지원하는 크로스 플랫폼 입력 헬퍼
    /// Active Input Handling이 New Input System 전용이어도 InvalidOperationException이 발생하지 않습니다.
    /// </summary>
    public static class InputHelper
    {
        public static float GetHorizontalAxis()
        {
            float axis = 0f;

#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) axis -= 1f;
                    if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) axis += 1f;
                }
            }
            catch { }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            // Fallback to Legacy Input (Only if Legacy Input is enabled in PlayerSettings)
            if (axis == 0f)
            {
                try
                {
                    if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) axis -= 1f;
                    if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) axis += 1f;
                    if (axis == 0f) axis = Input.GetAxisRaw("Horizontal");
                }
                catch { }
            }
#endif

            return Mathf.Clamp(axis, -1f, 1f);
        }

        public static bool IsShootingHeld()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Keyboard.current != null && (Keyboard.current.spaceKey.isPressed || Keyboard.current.enterKey.isPressed))
                {
                    return true;
                }
                if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                {
                    return true;
                }
            }
            catch { }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.Return) || Input.GetButton("Fire1"))
                {
                    return true;
                }
            }
            catch { }
#endif

            return false;
        }

        public static bool IsActionPressed()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
                {
                    return true;
                }
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    return true;
                }
            }
            catch { }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Fire1"))
                {
                    return true;
                }
            }
            catch { }
#endif

            return false;
        }

        public static bool IsRankingPressed()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    return true;
                }
            }
            catch { }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.R)) return true;
            }
            catch { }
#endif

            return false;
        }

        public static bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            try
            {
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    return true;
                }
            }
            catch { }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            try
            {
                if (Input.GetKeyDown(KeyCode.Escape)) return true;
            }
            catch { }
#endif

            return false;
        }
    }
}
