using UnityEngine;

namespace Dreamer.Player
{
    /// <summary>
    /// 5방향 타격, Raycast 피격 판정 및 채굴 실행을 전담하는 컴포넌트
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("타격 및 지층 판정 설정")]
        [SerializeField] private LayerMask destructibleTileLayer;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float attackCooldown = 0.1f;

        private PlayerStatsHandler statsHandler;
        private PlayerVisual visualHandler;
        private float lastAttackTime;

        public bool IsAttacking { get; private set; }

        private void Awake()
        {
            statsHandler = GetComponent<PlayerStatsHandler>();
            visualHandler = GetComponent<PlayerVisual>();
        }

        /// <summary>
        /// 전달받은 정수 원점 좌표(originGridPos)와 방향(direction)을 기준으로 정확히 타격 실행
        /// </summary>
        public bool TryAttack(Vector2Int direction, Vector2Int originGridPos, float gridSize)
        {
            if (Time.time < lastAttackTime + statsHandler.CurrentStats.AttackCooldown) return false;

            lastAttackTime = Time.time;
            IsAttacking = true;

            if (visualHandler != null) visualHandler.PlayAttackSquash();

            int attackPower = statsHandler != null ? statsHandler.CurrentStats.AttackPower : 1;

            // 1. 대각선 공격 시: PC 옆(가로) 타일 부채꼴 휘두르기 데미지 적용
            if (direction.x != 0 && direction.y != 0)
            {
                Vector2 sideWorldPos = new Vector2((originGridPos.x + direction.x) * gridSize, originGridPos.y * gridSize);
                DamageTargetAtPosition(sideWorldPos, attackPower);
            }

            // 2. 최종 목표 방향 타격 처리
            Vector2 targetWorldPos = new Vector2((originGridPos.x + direction.x) * gridSize, (originGridPos.y + direction.y) * gridSize);
            DamageTargetAtPosition(targetWorldPos, attackPower);

            Invoke(nameof(ResetAttackState), statsHandler.CurrentStats.AttackCooldown);
            return true;
        }

        private void DamageTargetAtPosition(Vector2 position, int damage)
        {
            Collider2D tileCol = Physics2D.OverlapCircle(position, 0.35f, destructibleTileLayer);
            if (tileCol != null && tileCol.TryGetComponent<Tile.TileInstance>(out var tileInstance))
            {
                tileInstance.TakeDamage(damage);
            }

            Collider2D enemyCol = Physics2D.OverlapCircle(position, 0.35f, enemyLayer);
            if (enemyCol != null)
            {
                Debug.Log($"[Combat] 💥 적 타격! 피해량: {damage}");
            }
        }

        private void ResetAttackState()
        {
            IsAttacking = false;
        }
    }
}