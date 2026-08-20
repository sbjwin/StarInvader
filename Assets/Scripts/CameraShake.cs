using System.Collections;
using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 피격 및 폭발 시 카메라 흔들림 연출
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private Vector3 originalPos;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            originalPos = transform.localPosition;
        }

        public void TriggerShake(float duration = 0.2f, float magnitude = 0.18f)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;

                transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = originalPos;
            shakeCoroutine = null;
        }
    }
}
