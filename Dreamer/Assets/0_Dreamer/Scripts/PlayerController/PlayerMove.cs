using DG.Tweening;
using UnityEngine;

namespace Dreamer.Player
{
    /// <summary>
    /// 플레이어의 그리드(타일) 단위 이동 및 벽 충돌 처리 전담 컴포넌트
    /// </summary>
    public class PlayerMove : MonoBehaviour
    {
        [Header("그리드 이동 설정")]
        [SerializeField] private float gridSize = 1f;
        [SerializeField] private float moveDuration = 0.12f;
        [SerializeField] private LayerMask obstacleLayer;

        private PlayerVisual visual;
        private PlayerCombat combat;

        public bool IsMoving { get; private set; }
        public float GridSize => gridSize;

        private void Awake()
        {
            visual = GetComponent<PlayerVisual>();
            combat = GetComponent<PlayerCombat>();
        }

        /// <summary>
        /// 지정한 방향으로 그리드 1칸 이동 시도
        /// </summary>
        public bool TryGridMove(Vector2 direction)
        {
            // 이동 중이거나 공격 중일 때 이동 차단
            if (IsMoving || (combat != null && combat.IsAttacking)) return false;

            Vector3 targetPosition = transform.position + (Vector3)(direction * gridSize);

            // 장애물(벽/지층 등) 확인
            RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, gridSize, obstacleLayer);
            if (hit.collider != null)
            {
                // 장애물 부딪힘 연출 (Juice)
                transform.DOKill();
                transform.DOMove(transform.position + (Vector3)(direction * 0.15f), 0.05f)
                    .SetLoops(2, LoopType.Yoyo);
                return false;
            }

            IsMoving = true;

            if (visual != null)
            {
                visual.UpdateFacingDirection(direction.x);
                visual.PlayMoveSquash();
            }

            // 지정한 시간 동안 그리드 이동
            transform.DOMove(targetPosition, moveDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // 정확한 그리드 좌표 정렬
                    transform.position = new Vector3(
                        Mathf.Round(transform.position.x),
                        Mathf.Round(transform.position.y),
                        0f
                    );
                    IsMoving = false;
                });

            return true;
        }
    }
}