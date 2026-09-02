using DG.Tweening;
using Dreamer.Core;
using Dreamer.Tile;
using UnityEngine;

namespace Dreamer.Enemy
{
    // ==========================================
    // 3. 자폭 적 (ExplosiveEnemy)
    // HP는 1이나 Hardness가 극도로 높음. 파괴 시 주변 3x3 지층 파괴
    // ==========================================
    public class ExplosiveEnemy : EnemyBase
    {
        [Header("자폭 적 스탯 설정")]
        [SerializeField] private int highHardness = 8; // 높은 단단함 (곡괭이에 큰 마모 발생)
        [SerializeField] private int fuseTurns = 2;    // 근접 감지 후 폭발까지 남은 턴 수
        [SerializeField] private int explosiveDamage = 50;    // 근접 감지 후 폭발까지 남은 턴 수

        private bool isArmed = false;
        private int currentFuse;

        public override int Hardness => highHardness;
        public bool IsArmed => isArmed;
        public int CurrentFuse => currentFuse;

        protected override void OnActivated()
        {
            base.OnActivated();
            isArmed = false;
            currentFuse = fuseTurns;
        }

        protected override void ExecuteTurnBehavior()
        {
            if (player == null) return;

            int targetGridX = Mathf.RoundToInt(player.Movement.CurrentGridPos.x / gridSize);
            int targetGridY = Mathf.RoundToInt(player.Movement.CurrentGridPos.y / gridSize);

            // 플레이어와의 맨해튼 그리드 거리 계산
            int dist = Mathf.Abs(targetGridX - gridPos.x) + Mathf.Abs(targetGridY - gridPos.y);

            // 1. 근접 시 (1칸 거리) 폭발 카운트다운 가동
            if (!isArmed && dist <= 1)
            {
                isArmed = true;
                currentFuse = fuseTurns;

                // 경고 차징 연출 (부풀어 오름)
                transform.DOKill();
                transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.15f);
                return;
            }

            // 2. 이미 카운트다운이 시작된 경우
            if (isArmed)
            {
                currentFuse--;

                // 매 턴 부풀어 오르며 틱 피드백 연출
                transform.DOKill();
                transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0f), 0.15f);

                if (currentFuse <= 0)
                {
                    Die(); // 카운트다운 종료 시 Die() 호출 -> 내부에서 TriggerExplosion() 실행
                    return;
                }
            }
            else
            {
                // 3. 아직 카운트다운 전이면 플레이어 방향으로 1칸 접근
                Vector2Int moveDir = Vector2Int.zero;

                if (targetGridY < gridPos.y) moveDir = Vector2Int.down;
                else if (targetGridX != gridPos.x) moveDir = new Vector2Int(targetGridX > gridPos.x ? 1 : -1, 0);

                if (moveDir == Vector2Int.zero) return;

                Vector2Int nextPos = gridPos + moveDir;
                Vector2 checkWorldPos = new Vector2(nextPos.x * gridSize, nextPos.y * gridSize);

                Collider2D hit = Physics2D.OverlapCircle(checkWorldPos, gridSize * 0.35f, obstacleLayer | destructibleTileLayer);

                if (hit == null)
                {
                    MoveToGrid(nextPos);
                }
            }
        }

        protected override void Die()
        {
            // 사망 시 3x3 폭발 범위 연쇄 파괴
            TriggerExplosion();
            base.Die();
        }

        private void TriggerExplosion()
        {
            if (JuiceManager.Instance != null)
            {
                JuiceManager.Instance.ShakeCamera(0.6f);
                JuiceManager.Instance.DoHitStop(0.1f, 0.05f);
            }

            // 주변 3x3 범위 타일 및 개체 파괴
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2 targetWorldPos = new Vector2((gridPos.x + x) * gridSize, (gridPos.y + y) * gridSize);
                    Collider2D hit = Physics2D.OverlapCircle(targetWorldPos, gridSize * 0.4f, obstacleLayer | destructibleTileLayer);
                    if (hit != null && hit.TryGetComponent<IDamageable>(out var damageable))
                    {
                        // 적들끼리는 폭발하지 않도록 하게한다.
                        if (hit.CompareTag("Enemy")) continue;
                        damageable.TakeDamage(explosiveDamage);
                    }
                }
            }
        }
    }
}
