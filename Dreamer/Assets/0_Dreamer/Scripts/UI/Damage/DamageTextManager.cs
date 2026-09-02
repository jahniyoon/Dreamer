using Dreamer.Core;
using UnityEngine;

namespace Dreamer.UI
{
    /// <summary>
    /// 오브젝트 풀을 활용하여 데미지 텍스트를 전역에서 손쉽게 생성하는 매니저
    /// </summary>
    public class DamageTextManager : MonoBehaviour
    {
        public static DamageTextManager Instance { get; private set; }

        [Header("데미지 텍스트 프리팹 설정")]
        [SerializeField] private DamageText damageTextPrefab;
        [SerializeField] private int initialPoolSize = 20;
        [Header("데미지 텍스트 설정")]
        public float jumpPower = 6.0f;
        public float targetPosY = -3.0f;
        public float duration = 0.5f;
        [Range(0,1)]public float fadeRatio = 0.35f;
        public Color playerColor;
        public Color enemyColor;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

 

        /// <summary>
        /// 특정 월드 좌표에 데미지 숫자를 띄웁니다.
        /// isPlayerDamage: true면 플레이어 내구도 감소(빨강), false면 적/지층 타격(노랑)
        /// </summary>
        public void SpawnDamageText(Vector3 worldPos, int damage, bool isPlayerDamage)
        {
            if (damageTextPrefab == null) return;

            // 텍스트들이 정확히 겹치지 않도록 미세한 랜덤 오프셋 적용
            Vector3 randomOffset = (Vector3)(UnityEngine.Random.insideUnitCircle * 0.25f);
            Vector3 spawnPos = worldPos + randomOffset;

            DamageText textObj = null;

            if (ObjectPoolManager.Instance != null)
            {
                textObj = ObjectPoolManager.Instance.SpawnFromPool(damageTextPrefab, spawnPos, Quaternion.identity, transform, 1f);
            }
            else
            {
                textObj = Instantiate(damageTextPrefab, spawnPos, Quaternion.identity);
            }

            if (textObj != null)
            {
                textObj.Show(damage, isPlayerDamage, spawnPos);
            }
        }
    }
}