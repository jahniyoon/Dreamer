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

            // 1단계: 해당 방향으로 즉시 타격 실행 (지층 체력 감속)
            Combat.TryAttack(targetDir, originGridPos, Movement.GridSize);

            // 2단계: 대각선 이동 시 옆(가로) 타일 장애물 체크
            if (targetDir.x != 0 && targetDir.y != 0)
            {
                Vector2 sideCheckWorldPos = new Vector2((originGridPos.x + targetDir.x) * Movement.GridSize, originGridPos.y * Movement.GridSize);
                Collider2D sideHit = Physics2D.OverlapCircle(sideCheckWorldPos, Movement.GridSize * 0.35f, Movement.ObstacleLayer);

                if (sideHit != null)
                {
                    // 옆 타일이 막혀있으면 진행 불가 및 반동 찌그러짐
                    Movement.TriggerBumpJuice(new Vector2Int(targetDir.x, 0));
                    return;
                }
            }

            // 3단계: 최종 목표 타일 장애물 체크 (타일 HP가 남아있거나 외벽인 경우)
            Vector2 targetCheckWorldPos = new Vector2((originGridPos.x + targetDir.x) * Movement.GridSize, (originGridPos.y + targetDir.y) * Movement.GridSize);
            Collider2D targetHit = Physics2D.OverlapCircle(targetCheckWorldPos, Movement.GridSize * 0.35f, Movement.ObstacleLayer);

            if (targetHit != null)
            {
                // 지층이 한 번에 안 부서짐 -> 이동하지 않고 제자리 반동 찌그러짐
                Movement.TriggerBumpJuice(targetDir);
            }
            else
            {
                // 지층이 부서져 빈 공간이 됨 -> 목표 칸으로 1칸 이동!
                Movement.ExecuteGridMove(targetDir);
            }
        }
    }
}