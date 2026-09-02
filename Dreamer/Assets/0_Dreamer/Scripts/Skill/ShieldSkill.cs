using Dreamer.Core;
using Dreamer.Data;
using Dreamer.Player;
using System.Collections;
using UnityEngine;

namespace Dreamer.Skill
{


    /// <summary>
    /// 3. 곡괭이 보호막 스킬: 일정 시간 동안 피격 및 암석 마모 피해 무적
    /// </summary>
    public class ShieldSkill : SkillBase
    {
        public ShieldSkill(SkillData data) : base(data) { }

        public override bool Execute(PlayerSkillHandler user)
        {
            if (!IsReady) return false;
            onExcuteSkill?.Invoke();

            LastCastTime = Time.time;
            user.StartCoroutine(ExecuteShieldRoutine(user));
            return true;
        }

        private IEnumerator ExecuteShieldRoutine(PlayerSkillHandler user)
        {
            user.SetInvincible(true);
            AudioManager.Instance.PlaySFX("ShieldSkill");

            // 보호막 VFX 스폰 및 플레이어 추적 배치
            GameObject shieldVfx = null;
            if (Data.VfxPrefab != null && JuiceManager.Instance != null)
            {
                shieldVfx = ObjectPoolManager.Instance != null
                    ? ObjectPoolManager.Instance.SpawnFromPool(Data.VfxPrefab, user.transform.position, Quaternion.identity, user.transform)
                    : UnityEngine.Object.Instantiate(Data.VfxPrefab, user.transform);
            }

            if (JuiceManager.Instance != null && Data.CastSound != null)
            {
                JuiceManager.Instance.PlaySfxWithPitch(Data.CastSound, 1f, 0.1f);
            }

            yield return new WaitForSeconds(Data.Duration);

            user.SetInvincible(false);

            if (shieldVfx != null)
            {
                if (ObjectPoolManager.Instance != null)
                    ObjectPoolManager.Instance.ReturnToPool(Data.VfxPrefab, shieldVfx);
                else
                    UnityEngine.Object.Destroy(shieldVfx);
            }
        }
    }
}
