using UnityEngine;

namespace Dreamer.Data
{
    [System.Serializable]
    public struct CalculatedPlayerStats
    {
        [field: SerializeField] public int MaxHp { get; private set; }
        [field: SerializeField] public int AttackPower { get; private set; }
        [field: SerializeField] public int Defense { get; private set; }
        [field: SerializeField] public float MoveSpeed { get; private set; }
        [field: SerializeField] public float AttackCooldown { get; private set; }

        public void ResetToBase(int baseHp, int baseAtk, int baseDef, float baseSpeed, float baseAttackCooldown)
        {
            MaxHp = baseHp;
            AttackPower = baseAtk;
            Defense = baseDef;
            MoveSpeed = baseSpeed;
            AttackCooldown = baseAttackCooldown;
        }

        public void ApplyItem(ItemData item)
        {
            if (item == null) return;
            AttackPower += item.AttackBonus;
            Defense += item.DefenseBonus;
            MaxHp += item.MaxHpBonus;
            MoveSpeed += item.MoveSpeedBonus;
            // 쿨타임 감소 적용 (최소 쿨타임 0.05초 방어)
            if (item.CooldownBonus > 0f)
            {
                AttackCooldown = Mathf.Max(0.05f, AttackCooldown - item.CooldownBonus);
            }
        }
    }
}