using DG.Tweening;
using Dreamer.Core;
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
        [SerializeField] private float moveDuration = 0.08f;
        [SerializeField] private LayerMask obstacleLayer;

        private PlayerVisual visual;

        public bool IsMoving { get; private set; }
        public float GridSize => gridSize;
        public LayerMask ObstacleLayer => obstacleLayer;

        /// <summary>
        /// 단 1픽셀의 소수점 오차도 허용하지 않는 플레이어의 정수 논리 그리드 좌표
        /// </summary>
        public Vector2Int CurrentGridPos { get; private set; }

        private void Awake()
        {
            visual = GetComponent<PlayerVisual>();
            SyncGridPosFromTransform();
        }

        /// <summary>
        /// Transform의 현재 위치를 기반으로 정수 논리 그리드 좌표 동기화
        /// </summary>
        public void SyncGridPosFromTransform()
        {
            CurrentGridPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / gridSize),
                Mathf.RoundToInt(transform.position.y / gridSize)
            );
            TurnManager.UpdatePlayerPosition(CurrentGridPos); // Update the player's position in the TurnManager
            transform.position = new Vector3(CurrentGridPos.x * gridSize, CurrentGridPos.y * gridSize, 0f);
        }

        /// <summary>
        /// 논리 그리드 좌표를 1칸 변경하고 해당 위치로 슬라이딩 이동
        /// </summary>
        public bool ExecuteGridMove(Vector2Int direction)
        {
            if (IsMoving || (direction.x == 0 && direction.y == 0)) return false;

            IsMoving = true;

            // 이동 시작 즉시 논리 정수 좌표 변경 (연타 시 중복 오판 방지 핵심)
            CurrentGridPos += direction;
            TurnManager.UpdatePlayerPosition(CurrentGridPos); // Update the player's position in the TurnManager

            Vector3 targetWorldPos = new Vector3(CurrentGridPos.x * gridSize, CurrentGridPos.y * gridSize, 0f);

            if (visual != null)
            {
                visual.UpdateFacingDirection(direction.x);
                visual.PlayMoveSquash();
            }

            transform.DOKill();
            transform.DOMove(targetWorldPos, moveDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // 이동 완료 후 월드 위치를 정수 좌표에 완벽히 고정
                    transform.position = targetWorldPos;
                    IsMoving = false;
                });

            return true;
        }
        /// <summary>
        /// 하단 발판이 파괴되었을 때, N칸 깊이만큼 바닥에 착지할 때까지 가속 낙하 연출
        /// </summary>
        public void ExecuteMultiGridFall(int fallDistance)
        {
            if (IsMoving || fallDistance <= 0) return;

            IsMoving = true;

            // 정수 그리드 Y 좌표 차감
            CurrentGridPos += Vector2Int.down * fallDistance;
            TurnManager.UpdatePlayerPosition(CurrentGridPos); // Update the player's position in the TurnManager

            Vector3 targetWorldPos = new Vector3(CurrentGridPos.x * gridSize, CurrentGridPos.y * gridSize, 0f);

            // 낙하 거리에 비례한 자연스러운 낙하 시간 계산 (가속 느낌)
            float fallDuration = Mathf.Sqrt(fallDistance) * 0.08f;

            transform.DOKill();
            transform.DOMove(targetWorldPos, fallDuration)
                .SetEase(Ease.InQuad) // 중력 가속도 연출
                .OnComplete(() =>
                {
                    transform.position = targetWorldPos;
                    IsMoving = false;

                    // 착지 피드백 (Juice)
                    if (visual != null) visual.PlayMoveSquash();
                    if (Dreamer.Core.JuiceManager.Instance != null)
                    {
                        Dreamer.Core.JuiceManager.Instance.ShakeCamera(0.05f * fallDistance);
                    }
                });
        }


        /// <summary>
        /// 막힌 지층/벽 타격 시 제자리에서 튕기는 찌그러짐 쥬시 연출
        /// </summary>
        public void TriggerBumpJuice(Vector2Int bumpDirection)
        {
            if (IsMoving) return;

            IsMoving = true;

            if (visual != null && bumpDirection.x != 0)
            {
                visual.UpdateFacingDirection(bumpDirection.x);
            }

            Vector3 startWorldPos = new Vector3(CurrentGridPos.x * gridSize, CurrentGridPos.y * gridSize, 0f);
            Vector3 bumpOffset = new Vector3(bumpDirection.x, bumpDirection.y, 0f).normalized * 0.1f;

            transform.DOKill();
            transform.DOMove(startWorldPos + bumpOffset, 0.035f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() =>
                {
                    // 연출 종료 후 원점 보장 및 잠금 해제
                    transform.position = startWorldPos;
                    IsMoving = false;
                });
        }
    }
}
