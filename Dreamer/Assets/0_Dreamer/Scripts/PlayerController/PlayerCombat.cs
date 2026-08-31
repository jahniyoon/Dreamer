using UnityEngine;
using Dreamer.Core;
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

            bool hitSide = false;

            // 1. 대각선 입력 시: PC 옆(가로) 위치에 대상(타일/적)이 있다면 우선 타격
            if (direction.x != 0 && direction.y != 0)
            {
                Vector2 sideWorldPos = new Vector2((originGridPos.x + direction.x) * gridSize, originGridPos.y * gridSize);
                hitSide = DamageTargetAtPosition(sideWorldPos, attackPower);
            }

            // 2. 옆 타격에 성공하지 않은 경우에만 목표 대각선 방향 타격 실행 (1회 입력당 1회 타격 보장)
            if (!hitSide)
            {
                Vector2 targetWorldPos = new Vector2((originGridPos.x + direction.x) * gridSize, (originGridPos.y + direction.y) * gridSize);
                DamageTargetAtPosition(targetWorldPos, attackPower);
            }

            Invoke(nameof(ResetAttackState), statsHandler.CurrentStats.AttackCooldown);
            return true;
        }

        /// <summary>
        /// 해당 위치의 대상(지층 타일 또는 적)을 공격하고, 실제 타격에 성공했는지 여부를 반환
        /// </summary>
        private bool DamageTargetAtPosition(Vector2 position, int damage)
        {
            LayerMask targetLayers = destructibleTileLayer | enemyLayer;
            Collider2D hit = Physics2D.OverlapCircle(position, 0.35f, targetLayers);

            if (hit != null)
            {
                // IDamageable 인터페이스 하나로 타일과 적 모두 일괄 
                if (hit.TryGetComponent<IDamageable>(out var target))
                {
                    if (!target.IsDead)
                    {
                        target.TakeDamage(damage);
                        return true;
                    }
                }
            }

            return false;
        }
        private void ResetAttackState()
        {
            IsAttacking = false;
        }
    }
}