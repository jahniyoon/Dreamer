using Dreamer.Core;
using Dreamer.Data;
using Dreamer.Player;
using Dreamer.Tile;
using System.Collections;
using UnityEngine;


namespace Dreamer.Skill
{
    /// <summary>
    /// 플레이어 스킬의 추상 기반 클래스
    /// </summary>
    public abstract class SkillBase
    {
        public SkillData Data { get; protected set; }
        public float LastCastTime { get; protected set; }

        public SkillBase(SkillData data)
        {
            Data = data;
            LastCastTime = -999f;
        }

        public bool IsReady => Time.time >= LastCastTime + Data.Cooldown;
        public float RemainingCooldown => Mathf.Max(0f, (LastCastTime + Data.Cooldown) - Time.time);

        public abstract bool Execute(PlayerSkillHandler user);
    }

 
}