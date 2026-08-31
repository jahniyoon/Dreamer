using UnityEngine;

namespace Dreamer.Player
{
    /// <summary>
    /// 5방향 타격, Raycast 피격 판정 및 채굴 실행을 전담하는 컴포넌트
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("타격 및 지층 판정 설정")]
        [SerializeField] private float attackRange = 0.8f;
        [SerializeField] private LayerMask destructibleTileLayer;
        [SerializeField] private LayerMask enemyLayer;
        [SerializeField] private float attackCooldown = 0.25f;

        private PlayerStatsHandler statsHandler;
        private PlayerVisual visualHandler;
        private float lastAttackTime;

        public bool IsAttacking { get; private set; }

        private void Awake()
        {
            statsHandler = GetComponent<PlayerStatsHandler>();
            visualHandler = GetComponent<PlayerVisual>();
        }

        public bool TryAttack(Vector2 attackDirection)
        {
            if (Time.time < lastAttackTime + attackCooldown) return false;

            lastAttackTime = Time.time;
            IsAttacking = true;

            // 비주얼 스쿼시 연출 요청
            if (visualHandler != null) visualHandler.PlayAttackSquash();

            // 5방향 레이캐스트 타격 판정
            Vector2 attackOrigin = transform.position;
            RaycastHit2D enemyHit = Physics2D.Raycast(attackOrigin, attackDirection, attackRange, enemyLayer);
            RaycastHit2D tileHit = Physics2D.Raycast(attackOrigin, attackDirection, attackRange, destructibleTileLayer);

            int attackPower = statsHandler != null ? statsHandler.CurrentStats.AttackPower : 1;

            if (enemyHit.collider != null)
            {
                Debug.Log($"[Combat] 적 타격! 피해량: {attackPower}");
                // TODO: EnemyController에 데미지 전달
            }

            if (tileHit.collider != null)
            {
                Debug.Log($"[Combat] 지층 타격! 위치: {tileHit.point}, 방향: {attackDirection}");
                // TODO: TileInstance에 데미지 전달
            }

            Invoke(nameof(ResetAttackState), attackCooldown * 0.8f);
            return true;
        }

        private void ResetAttackState()
        {
            IsAttacking = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, Vector2.down * attackRange);
        }
    }
}