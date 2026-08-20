using UnityEngine;

namespace StarInvader
{
    /// <summary>
    /// 적 파괴 시 폭발 애니메이션 및 자동 파괴
    /// </summary>
    public class ExplosionEffect : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 0.4f;
        [SerializeField] private float expandSpeed = 2.5f;

        private SpriteRenderer sr;
        private float timer = 0f;
        private Color initialColor;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null) initialColor = sr.color;
        }

        private void Update()
        {
            timer += Time.deltaTime;
            float progress = timer / lifeTime;

            // 점점 커지면서 투명해지는 연출
            transform.localScale += Vector3.one * (expandSpeed * Time.deltaTime);

            if (sr != null)
            {
                Color c = initialColor;
                c.a = Mathf.Lerp(1f, 0f, progress);
                sr.color = c;
            }

            if (timer >= lifeTime)
            {
                Destroy(gameObject);
            }
        }
    }
}
