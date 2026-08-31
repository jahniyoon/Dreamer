using DG.Tweening;
using Dreamer.Core;
using Dreamer.Data;
using UnityEngine;
namespace Dreamer.Tile
{
    /// <summary>
    /// 단일 지층 타일의 체력, 피격 반응, 파괴 및 이펙트/드롭 연동을 담당하는 컴포넌트
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class TileInstance : MonoBehaviour, IDamageable
    {
        [Header("지층 데이터")]
        [SerializeField] private TileData tileData;

        private SpriteRenderer spriteRenderer;
        private Collider2D tileCollider;
        private int currentHp;
        private Vector3 originalScale;
        private Vector2Int gridPosition; // 내 그리드 좌표 기억

        public TileData Data => tileData;
        public int CurrentHp => currentHp;
        public Vector2Int GridPosition => gridPosition;
        public bool IsDead => currentHp <= 0;

        /// <summary>
        ///  타일 파괴 시 좌표를 통보하는 정적 이벤트
        /// </summary>
        public static event System.Action<Vector2Int> OnTileDestroyed;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            tileCollider = GetComponent<Collider2D>();
            originalScale = transform.localScale;
        }

        /// <summary>
        /// 타일 데이터와 그리드 좌표를 기반으로 초기화
        /// </summary>
        public void InitTile(TileData data, Vector2Int gridPos)
        {
            tileData = data;
            gridPosition = gridPos;

            if (tileData == null) return;

            currentHp = tileData.MaxHp;
            transform.localScale = originalScale;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = tileData.TileSprite;
                spriteRenderer.color = tileData.TileColor;
            }

            if (tileCollider != null)
            {
                tileCollider.enabled = true;
            }
        }

        /// <summary>
        /// 타격 데미지 처리 및 쥬시 연출 실행
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (currentHp <= 0 || tileData == null) return;

            currentHp -= damage;
            Debug.Log($"[Tile] {tileData.TileName}에게 {damage}의 데미지!({currentHp})");
            // 1. 타격 피드백 (Juice)
            ApplyHitJuice();

            // 2. 파괴 여부 체크
            if (currentHp <= 0)
            {
                DestroyTile();
            }
        }

        private void DestroyTile()
        {
            if (tileCollider != null) tileCollider.enabled = false;

            OnTileDestroyed?.Invoke(gridPosition);

            // 파괴 VFX 생성 (ObjectPoolManager 활용)
            if (tileData.DestroyParticlePrefab != null && JuiceManager.Instance != null)
            {
                JuiceManager.Instance.SpawnVfx(tileData.DestroyParticlePrefab, transform.position, 1.5f);
            }

            // 아이템 드롭 로직 (확률 체크)
            TryDropItem();

            // 풀에 반환 또는 비활성화
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnToPool(gameObject, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void ApplyHitJuice()
        {
            // DOTween 피격 펀치 스케일 효과
            transform.DOKill();
            transform.localScale = originalScale;
            transform.DOPunchScale(new Vector3(-0.15f, 0.15f, 0f), 0.12f, 5, 0.5f)
                .OnComplete(() => transform.localScale = originalScale);

            // 카메라 흔들림 & 히트 스톱
            if (JuiceManager.Instance != null)
            {
                JuiceManager.Instance.ShakeCamera(tileData.CameraShakeIntensity);
                JuiceManager.Instance.DoHitStop(0.03f, 0.1f);

                if (tileData.DestroySound != null)
                {
                    JuiceManager.Instance.PlaySfxWithPitch(tileData.DestroySound, 0.8f, 0.15f);
                }
            }
        }

  
        private void TryDropItem()
        {
            if (tileData.DropItems == null || tileData.DropItems.Count == 0) return;

            float roll = Random.value;
            if (roll <= tileData.ItemDropRate)
            {
                int randomIndex = Random.Range(0, tileData.DropItems.Count);
                ItemData droppedItem = tileData.DropItems[randomIndex];
                Debug.Log($"[Tile] 지층 파괴 아이템 드롭: {droppedItem.ItemName}");
                // TODO: 아이템 필드 드롭 오브젝트 스폰
            }
        }
    }
}
