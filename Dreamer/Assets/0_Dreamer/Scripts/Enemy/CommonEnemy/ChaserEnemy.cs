using Dreamer.Tile;
using UnityEngine;

namespace Dreamer.Enemy
{
    // ==========================================
    // 2. X축 추적 채굴 적
    // 플레이어 X축을 추적하며 전방 지층을 굴착함 (위로 오르지 않음)
    // ==========================================
    public class ChaserEnemy : EnemyBase
    {
        [Header("추적 턴 딜레이 설정")]
        [SerializeField] private int moveIntervalTurns = 2; // 몇 턴마다 1칸 이동할지 (기본 2턴당 1칸)

        private int currentTurnCount = 0;
        protected override void OnActivated()
        {
            base.OnActivated();
            currentTurnCount = 0;
        }

        protected override void ExecuteTurnBehavior()
        {
            if (player == null) return;

            currentTurnCount++;

            // 지정된 턴 수에 도달할 때까지 대기
            if (currentTurnCount < moveIntervalTurns)
            {
                return;
            }

            // 이동 턴 도달 시 카운터 리셋
            currentTurnCount = 0;

            int targetGridX = Mathf.RoundToInt(player.transform.position.x / gridSize);
            int currentGridX = gridPos.x;

            if (targetGridX == currentGridX) return; // 이미 X축 일치 시 대기

            int dirX = targetGridX > currentGridX ? 1 : -1;
            Vector2Int nextPos = gridPos + new Vector2Int(dirX, 0);
            Vector2 checkWorldPos = new Vector2(nextPos.x * gridSize, nextPos.y * gridSize);

            Collider2D hit = Physics2D.OverlapCircle(checkWorldPos, gridSize * 0.35f, obstacleLayer | destructibleTileLayer);

            if (hit != null && hit.TryGetComponent<TileInstance>(out var tile) && tile.CurrentHp > 0)
            {
                // 장애물이 땅 타일이면 굴착하여 파괴 후 전진
                tile.TakeDamage(999);
            }

            MoveToGrid(nextPos);
        }
    }

}
