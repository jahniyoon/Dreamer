using Dreamer.Core;
using Dreamer.Data;
using Dreamer.Player;
using Dreamer.Tile;
using System.Collections;
using UnityEngine;
namespace Dreamer.Skill
{
    /// <summary>
    /// 돌진 채굴 스킬: 바라보는 방향으로 N칸 돌진하며 경로 상의 타일과 적을 일괄 파괴
    /// </summary>
    public class DashSkill : SkillBase
    {
        public DashSkill(SkillData data) : base(data) { }

        public override bool Execute(PlayerSkillHandler user)
        {
            if (!IsReady || user.Movement.IsMoving) return false;

            LastCastTime = Time.time;

            // 이동 방향 추출 (기본값: 아래)
            Vector2 rawInput = user.InputHandler != null ? user.InputHandler.RawInputDirection : Vector2.down;
            Vector2Int dashDir = Vector2Int.down;

            if (Mathf.Abs(rawInput.x) > 0.5f) dashDir = new Vector2Int(rawInput.x > 0 ? 1 : -1, 0);
            else if (rawInput.y < -0.3f) dashDir = Vector2Int.down;

            user.StartCoroutine(ExecuteDashRoutine(user, dashDir));
            return true;
        }

        private IEnumerator ExecuteDashRoutine(PlayerSkillHandler user, Vector2Int direction)
        {
            int dashDistance = 3; // 기본 돌진 칸 수
            float gridSize = user.Movement.GridSize;
            int attackPower = Mathf.RoundToInt(user.StatsHandler.CurrentStats.AttackPower * Data.DamageMultiplier);

            for (int i = 0; i < dashDistance; i++)
            {
                Vector2Int nextGridPos = user.Movement.CurrentGridPos + direction;
                Vector2 targetWorldPos = new Vector2(nextGridPos.x * gridSize, nextGridPos.y * gridSize);

                // 지층 및 적 파괴 처리
                Collider2D tileCol = Physics2D.OverlapCircle(targetWorldPos, 0.4f, user.DestructibleTileLayer);
                if (tileCol != null && tileCol.TryGetComponent<TileInstance>(out var tileInstance))
                {
                    tileInstance.TakeDamage(attackPower);
                }

                // 이동 실행
                user.Movement.ExecuteGridMove(direction);

                if (JuiceManager.Instance != null)
                {
                    JuiceManager.Instance.ShakeCamera(0.15f);
                }

                yield return new WaitForSeconds(0.05f); // 돌진 속도 간격
            }
        }
    }

}
