using Dreamer.Tile;
using UnityEngine;

namespace Dreamer.Enemy
{
    // ==========================================
    // 1. 가로 채굴 적 (HorizontalMinerEnemy)
    // 좌우 순찰하며 확률에 따라 지층을 파괴하고 이동
    // ==========================================
    public class DiggerEnemy : EnemyBase
    {
        [Header("가로 채굴 규칙")]
        [Range(0,1f)][SerializeField] private float miningChance = 0.4f; // 암석을 만났을 때 파낼 확률 (0.4 = 40%)

        private int moveDirection = 1; // 1: 우측, -1: 좌측

        protected override void ExecuteTurnBehavior()
        {
            Vector2Int nextPos = gridPos + new Vector2Int(moveDirection, 0);
            Vector2 checkWorldPos = new Vector2(nextPos.x * gridSize, nextPos.y * gridSize);

            Collider2D hit = Physics2D.OverlapCircle(checkWorldPos, gridSize * 0.35f, obstacleLayer | destructibleTileLayer);

            if (hit == null)
            {
                MoveToGrid(nextPos);
            }
            else
            {
                if (hit.TryGetComponent<TileInstance>(out var tile) && tile.CurrentHp > 0)
                {
                    // 특정 확률에 따라 땅을 파고 진입
                    if (UnityEngine.Random.value <= miningChance)
                    {
                        tile.TakeDamage(999); // 타일 즉시 파괴
                        MoveToGrid(nextPos);
                    }
                    else
                    {
                        // 반대 방향으로 회전
                        moveDirection *= -1;
                    }
                }
                else
                {
                    // 파괴 불가능 외벽인 경우 방향 반전
                    moveDirection *= -1;
                }
            }
        }
    }
}