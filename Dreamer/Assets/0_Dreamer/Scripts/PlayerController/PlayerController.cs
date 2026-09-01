using Dreamer.Core;
using Dreamer.Tile;
using UnityEngine;


namespace Dreamer.Player
{
    /// <summary>
    /// 입력 수집, 이동 및 하위 컴포넌트(Stats, Visual, Combat)를 조율하는 메인 컨트롤러
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    [RequireComponent(typeof(PlayerInputHandler), typeof(PlayerMove))]
    [RequireComponent(typeof(PlayerCombat), typeof(PlayerVisual))]
    public class PlayerController : MonoBehaviour
    {
        public PlayerInputHandler InputHandler { get; private set; }
        public PlayerMove Movement { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public PlayerVisual Visual { get; private set; }
        public PlayerStatsHandler Stats { get; private set; }

        private void Awake()
        {
            InputHandler = GetComponent<PlayerInputHandler>();
            Movement = GetComponent<PlayerMove>();
            Combat = GetComponent<PlayerCombat>();
            Visual = GetComponent<PlayerVisual>();
            Stats = GetComponent<PlayerStatsHandler>();
        }

        private void Update()
        {
            ProcessBumpCombatMovement();
        }

        /// <summary>
        /// 방향 입력 발생 시: 1) 타격 실행 -> 2) 정수 그리드 장애물 검사 -> 3) 진입 또는 제자리 반동
        /// </summary>
        private void ProcessBumpCombatMovement()
        {
            if (InputHandler == null || Movement == null || Combat == null) return;

            // 이동 중이거나 공격 후딜레이 중 중복 실행 완전 차단
            if (Movement.IsMoving || Combat.IsAttacking) return;

            Vector2 rawDir = InputHandler.RawInputDirection;
            if (rawDir.magnitude < 0.3f) return;

            Vector2Int targetDir = Vector2Int.zero;

            // 5방향 정수 방향 추출
            if (rawDir.y < -0.3f && Mathf.Abs(rawDir.x) > 0.3f)
            {
                targetDir = new Vector2Int(rawDir.x > 0 ? 1 : -1, -1);
            }
            else if (Mathf.Abs(rawDir.x) > 0.5f)
            {
                targetDir = new Vector2Int(rawDir.x > 0 ? 1 : -1, 0);
            }
            else if (rawDir.y < -0.5f)
            {
                targetDir = Vector2Int.down;
            }

            if (targetDir == Vector2Int.zero) return;

            // 현재 플레이어의 고정 정수 논리 좌표 가져오기
            Vector2Int originGridPos = Movement.CurrentGridPos;

            // 1단계: 해당 방향으로 즉시 타격 실행 (공격 쿨타임/실패 시 즉시 중단)
            if (!Combat.TryAttack(targetDir, originGridPos, Movement.GridSize))
            {
                return;
            }

            // 2단계: 대각선 이동 시 옆(가로) 타일 장애물/미파괴 타일 체크
            if (targetDir.x != 0 && targetDir.y != 0)
            {
                Vector2 sideCheckWorldPos = new Vector2((originGridPos.x + targetDir.x) * Movement.GridSize, originGridPos.y * Movement.GridSize);
                if (IsPositionBlocked(sideCheckWorldPos))
                {
                    // 옆 타일 체력이 남아있거나 벽/적이 있으면 진행 불가 -> 제자리 반동 찌그러짐
                    Movement.TriggerBumpJuice(new Vector2Int(targetDir.x, 0));
                    TurnManager.DispatchPlayerTurn(); // 턴 통보
                    return;
                }
            }

            // 3단계: 최종 목표 타일 장애물 체크 (체력이 남아있는 지층 타일, 적, 외벽인 경우)
            Vector2 targetCheckWorldPos = new Vector2((originGridPos.x + targetDir.x) * Movement.GridSize, (originGridPos.y + targetDir.y) * Movement.GridSize);

            if (IsPositionBlocked(targetCheckWorldPos))
            {
                // 지층 HP가 아직 남아있거나 적/벽에 막힘 -> 이동하지 않고 제자리 반동 찌그러짐 연출!
                Movement.TriggerBumpJuice(targetDir);
            }
            else
            {
                // 지층/적이 제거되어 비어있음 -> 목표 칸으로 1칸 정상 이동!
                Movement.ExecuteGridMove(targetDir);
            }

            // 플레이어의 이동/공격 판정 완료 후 턴 이벤트 발송 (적들이 플레이어의 신규 위치 참조)
            TurnManager.DispatchPlayerTurn();
        }
        /// <summary>
        /// 플레이어 아래쪽 방향으로 연속 탐색하여 빈 공간(파괴된 타일) 수만큼 즉시 낙하 처리
        /// </summary>
        public void CheckAndApplyGravity()
        {
            if (Movement == null || Movement.IsMoving) return;

            int fallDistance = 0;
            Vector2Int currentGridPos = Movement.CurrentGridPos;

            // 아래쪽으로 1칸씩 스캔하여 막힌 타일/벽을 만날 때까지 거리 계산
            while (true)
            {
                Vector2Int checkGridPos = currentGridPos + (Vector2Int.down * (fallDistance + 1));
                Vector2 checkWorldPos = new Vector2(checkGridPos.x * Movement.GridSize, checkGridPos.y * Movement.GridSize);

                if (IsPositionBlocked(checkWorldPos))
                {
                    break; // 막힌 타일/벽을 만나면 스캔 중단
                }

                fallDistance++;

                // 안전장치: 최대 30칸 이상 연속 낙하 루프 방지
                if (fallDistance >= 30) break;
            }

            // 낙하할 빈 공간이 1칸 이상 존재하면 즉시 낙하 연출 실행
            if (fallDistance > 0)
            {
                Movement.ExecuteMultiGridFall(fallDistance);
            }
        }
        /// <summary>
        /// 해당 위치에 체력이 남아있는 지층 타일이나 외벽 장애물이 존재하는지 검사
        /// </summary>
        private bool IsPositionBlocked(Vector2 worldPos)
        {
            LayerMask blockingLayers = Movement.ObstacleLayer;

            // 1차: 지정된 레이어 기반 검사
            Collider2D hit = Physics2D.OverlapCircle(worldPos, Movement.GridSize * 0.35f, blockingLayers);

            // 2차: 인스펙터 레이어 설정 누락 대비 레이어 무관 검사
            if (hit == null)
            {
                hit = Physics2D.OverlapCircle(worldPos, Movement.GridSize * 0.35f);
            }

            if (hit != null)
            {
                // 지층 타일인 경우 CurrentHp를 직접 검사 (체력이 1 이상 남아있으면 무조건 막힘)
                if (hit.TryGetComponent<TileInstance>(out var tileInstance))
                {
                    return tileInstance.CurrentHp > 0;
                }

                // 플레이어 자신이나 트리거가 아닌 외벽/장애물이면 막힘 판정
                if (!hit.CompareTag("Player") && !hit.isTrigger)
                {
                    return true;
                }
            }

            return false;
        }
    }

}
