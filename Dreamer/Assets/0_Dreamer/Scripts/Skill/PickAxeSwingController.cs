using DG.Tweening;
using Dreamer.Core;
using Dreamer.Data;
using UnityEngine;
namespace Dreamer.Player
{
    public enum SwingDirection
    {
        Right,
        Left,
        Up,
        Down,
        DiagonalDown
    }

    public class PickaxeSwingController : MonoBehaviour
    {
        [Header("곡괭이 Transform (플레이어 손 위치에 자식으로 배치)")]
        [SerializeField] private Transform pickaxeTransform;
        [SerializeField] private Transform broken;
        [SerializeField] private Transform handTransform;

        [Header("휘두르기 연출 설정")]
        [SerializeField] private float swingDuration = 0.12f; // 앙빅 스타일은 0.1~0.15초로 매우 빠름!
        [SerializeField] private float swingAngle = 80f;     // 휘두르는 각도
        [SerializeField] private Ease swingEase = Ease.OutBack; // 살짝 튕기는 찰진 연출

        private bool isSwinging = false;
        private Vector3 defaultLocalPos;
        private Quaternion defaultLocalRot;

        [Header("곡괭이 SpriteRenderer 참조")]
        [SerializeField] private SpriteRenderer pickaxeSpriteRenderer;
        private PlayerController Player;

        private void OnEnable()
        {
            // 오브젝트 활성화 시 또는 게임 시작 시 현재 착용 중인 곡괭이 이미지로 갱신
            Player.Stats.OnPickaxeChanged += UpdatePickaxeSprite;
        }
        private void OnDisable()
        {
            Player.Stats.OnPickaxeChanged -= UpdatePickaxeSprite;
        }

        /// <summary>
        /// 세이브 데이터에 지정된 착용 곡괭이 ID 기반으로 스프라이트 이미지 즉시 교체
        /// </summary>
        public void UpdatePickaxeSprite(ItemData item)
        {
            Debug.Log(item.ItemId + " 교체");
   
            if (item != null && item.ItemIcon != null)
            {
                // 2. 곡괭이 이미지 교체
                pickaxeSpriteRenderer.sprite = item.ItemIcon;
            }
        }
        private void Awake()
        {
            Player = GetComponent<PlayerController>();
        }

        private void Start()
        {
            if (pickaxeTransform != null)
            {
                defaultLocalPos = pickaxeTransform.localPosition;
                defaultLocalRot = pickaxeTransform.localRotation;
            }
        }

        public void ResetPickAxe(bool isBroken = false)
        {
            pickaxeTransform.localPosition = defaultLocalPos;
            pickaxeTransform.localRotation = defaultLocalRot;

            pickaxeTransform.gameObject.SetActive(!isBroken);
            broken.gameObject.SetActive(isBroken);
        }
        /// <summary>
        /// 입력 방향 Vector2를 받아 5방향 휘두르기 실행
        /// </summary>
        public void Swing(Vector2 inputDir)
        {
            if (isSwinging || pickaxeTransform == null) return;

            SwingDirection dir = CalculateSwingDirection(inputDir);
            ExecuteSwingMotion(dir);
            AudioManager.Instance.PlaySFX("SwingPick");
        }

        private SwingDirection CalculateSwingDirection(Vector2 dir)
        {
            // 입력이 없거나 대각선일 경우 기본 우측/정면
            if (dir == Vector2.zero) return SwingDirection.Right;

            if (dir.y > 0.5f) return SwingDirection.Up;
            if (dir.y < -0.5f && Mathf.Abs(dir.x) < 0.3f) return SwingDirection.Down;
            if (dir.x < -0.5f) return SwingDirection.Left;
            if (dir.x > 0.5f) return SwingDirection.Right;

            return SwingDirection.DiagonalDown;
        }
        private void ExecuteSwingMotion(SwingDirection dir)
        {
            isSwinging = true;

            // 1. 손(Hand) 방향 반전 처리 (Right: Y=180, Left: Y=0)
            if (handTransform != null)
            {
                bool isFacingRight = (dir == SwingDirection.Right ||
                                      (dir == SwingDirection.DiagonalDown && pickaxeTransform.position.x >= handTransform.position.x));

                // 요청하신 좌우 회전값 적용
                handTransform.localRotation = Quaternion.Euler(0f, isFacingRight ? 0f : 180f, 0f);
            }

            // 2. 방향에 따른 시작 각도 및 타격 목표 각도 연산
            Vector3 targetEuler = Vector3.zero;
            Vector3 punchOffset = Vector3.zero;

            switch (dir)
            {
                case SwingDirection.Right:
                case SwingDirection.Left:
                    targetEuler = new Vector3(0, 0, -swingAngle);
                    punchOffset = new Vector3(0.3f, -0.1f, 0);
                    break;

                case SwingDirection.Up:
                    targetEuler = new Vector3(0, 0, 45f);
                    punchOffset = new Vector3(0, 0.4f, 0);
                    break;

                case SwingDirection.Down:
                    targetEuler = new Vector3(0, 0, -135f);
                    punchOffset = new Vector3(0, -0.4f, 0);
                    break;

                case SwingDirection.DiagonalDown:
                    targetEuler = new Vector3(0, 0, -100f);
                    punchOffset = new Vector3(0.2f, -0.3f, 0);
                    break;
            }

            // 3. DOTween 애니메이션 연출
            Sequence swingSeq = DOTween.Sequence();

            // 찍기 (강하게 후려치기)
            swingSeq.Append(pickaxeTransform.DOLocalRotate(targetEuler, swingDuration).SetEase(swingEase));
            swingSeq.Join(pickaxeTransform.DOLocalMove(defaultLocalPos + punchOffset, swingDuration));

            // 복귀 (원래 자세로 복귀)
            swingSeq.Append(pickaxeTransform.DOLocalRotateQuaternion(defaultLocalRot, swingDuration * 0.8f));
            swingSeq.Join(pickaxeTransform.DOLocalMove(defaultLocalPos, swingDuration * 0.8f));

            swingSeq.OnComplete(() =>
            {
                isSwinging = false;
            });
        }
    }
}