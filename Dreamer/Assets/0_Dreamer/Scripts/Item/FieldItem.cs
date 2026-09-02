using DG.Tweening;
using Dreamer.Core;
using UnityEngine;
using static TreeEditor.TreeEditorHelper;

namespace Dreamer.Item
{

    public enum OreType
    {
        Iron = 0,       // 철 (최대 내구도 강화용)
        Gold = 1,       // 금 (강도/방어력 강화용)
        Diamond = 2,    // 다이아몬드 (공격력 강화용)
        Mushroom = 3,

        RepairKit = 10,  // 소모품: 곡괭이 수리
        SparePickaxe = 11// 소모품: 곡괭이 즉시 수리
    }

    /// <summary>
    /// 필드에 독립적으로 스폰되는 광석/소모품 아이템 컴포넌트
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class FieldItem : MonoBehaviour
    {
        [Header("아이템 설정")]
        [SerializeField] private OreType oreType = OreType.Iron;
        [SerializeField] private int amount = 1;
        [SerializeField] private float collectRadius = 0.45f; // 플레이어가 같은 위치(타일)에 도달하여 획득되는 거리

        [Header("아이템 스프라이트 / SFX")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] private string collectSfx;

        private Transform playerTransform;
        private bool isBeingCollected;
        private Tween idleFloatTween;
        private Vector2Int gridPos;

        public OreType Type => oreType;
        public int Amount => amount;
        public Vector2Int GridPos => gridPos;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

            // 물리 충돌 차단을 위해 Trigger 설정
            if (TryGetComponent<Collider2D>(out var col))
            {
                col.isTrigger = true;
            }
        }


        public virtual void InitItem(OreType type, int itemAmount, Vector2Int initialGridPos)
        {
            oreType = type;
            amount = itemAmount;
            gridPos = initialGridPos;
            isBeingCollected = false;

            FindPlayer();

            // 필드 생성 톡! 튀어오르는 연출 (Pop animation)
            transform.DOKill();
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);

            // 잔잔하게 공중 위아래로 둥둥 뜨는 애니메이션
            idleFloatTween?.Kill();
            idleFloatTween = transform.DOPath(
                new Vector3[] { transform.position + Vector3.up * 0.08f, transform.position },
                1.2f, PathType.Linear
            ).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }
        }

        private void Update()
        {
            if (isBeingCollected) return;

            if (playerTransform == null)
            {
                FindPlayer();
                if (playerTransform == null) return;
            }

            float dist = Vector2.Distance(transform.position, playerTransform.position);

            // 플레이어가 아이템과 같은 위치(타일)로 도달했을 때 획득 처리
            if (dist <= collectRadius)
            {
                TriggerCollectEffect();
            }
        }

        /// <summary>
        /// 플레이어가 같은 위치에 들어왔을 때 톡! 튀어오르며 흡수되는 찰진 연출
        /// </summary>
        protected virtual void TriggerCollectEffect()
        {
            isBeingCollected = true;
            idleFloatTween?.Kill();

            transform.DOKill();

            // 0.2초 만에 톡! 커졌다가 작아지며 획득
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOScale(Vector3.one * 1.35f, 0.08f).SetEase(Ease.OutQuad));
            seq.Append(transform.DOScale(Vector3.zero, 0.12f).SetEase(Ease.InBack));
            seq.OnComplete(CollectItem);
        }

        protected virtual void CollectItem()
        {
            // 플레이어 자원 매니저에 자원 추가
            if (PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.AddResource(oreType, amount);
            }

            // 사운드 및 이펙트 연출
            AudioManager.Instance.PlaySFX(collectSfx);
            Kill(); // 아이템 풀 반환 또는 비활성화
         
        }
        public void Kill()
        {
            // 올바른 예 (자기 자신 인스턴스만 반납)
            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnToPool(gameObject, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            idleFloatTween?.Kill();
            transform.DOKill();
        }
    }
}