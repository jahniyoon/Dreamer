using Dreamer.Core;
using Dreamer.Data;
using Dreamer.Player;
using Dreamer.Tile;
using UnityEngine;

namespace Dreamer.Skill
{
    /// <summary>
    /// 충격파 스킬: 주변 3x3 영역의 지층 타일을 즉시 파괴하고 적에게 피해를 줌
    /// </summary>
    public class ShockwaveSkill : SkillBase
    {
        public ShockwaveSkill(SkillData data) : base(data) { }

        public override bool Execute(PlayerSkillHandler user)
        {
            if (!IsReady) return false;

            LastCastTime = Time.time;

            Vector2Int centerGridPos = user.Movement.CurrentGridPos;
            float gridSize = user.Movement.GridSize;
            int attackPower = Mathf.RoundToInt(user.StatsHandler.CurrentStats.AttackPower * Data.DamageMultiplier);
            AudioManager.Instance.PlaySFX("ExplosionSkill");

            // 주변 3x3 범위 타격
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2 targetWorldPos = new Vector2(
                        (centerGridPos.x + x) * gridSize,
                        (centerGridPos.y + y) * gridSize
                    );

                    // 타일 데미지 및 파괴
                    Collider2D tileCol = Physics2D.OverlapCircle(targetWorldPos, 0.4f, user.DestructibleTileLayer);
                    if (tileCol != null && tileCol.TryGetComponent<TileInstance>(out var tileInstance))
                    {
                        tileInstance.TakeDamage(attackPower);
                    }

                    // 적 데미지
                    Collider2D enemyCol = Physics2D.OverlapCircle(targetWorldPos, 0.4f, user.EnemyLayer);
                    if (enemyCol != null)
                    {
                        Debug.Log($"[Shockwave] 💥 충격파 적 타격! 피해: {attackPower}");
                    }
                }
            }

            // Juice 피드백 연출
            if (JuiceManager.Instance != null)
            {
                JuiceManager.Instance.ShakeCamera(Data.CameraShakeIntensity);
                JuiceManager.Instance.DoHitStop(0.08f, 0.1f);
                if (Data.VfxPrefab != null)
                {
                    JuiceManager.Instance.SpawnVfx(Data.VfxPrefab, user.transform.position, 1.5f);
                }
                if (Data.CastSound != null)
                {
                    JuiceManager.Instance.PlaySfxWithPitch(Data.CastSound, 1f, 0.1f);
                }
            }

            // 충격파 후 아래쪽에 뚫린 공백이 생기면 자동으로 중력 낙하 적용
            if (user.Controller != null)
            {
                user.Controller.CheckAndApplyGravity();
            }

            return true;
        }
    }
}