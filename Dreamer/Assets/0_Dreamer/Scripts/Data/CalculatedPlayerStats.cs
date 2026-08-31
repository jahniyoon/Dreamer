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

        public void ResetToBase(int baseHp, int baseAtk, int baseDef, float baseSpeed)
        {
            MaxHp = baseHp;
            AttackPower = baseAtk;
            Defense = baseDef;
            MoveSpeed = baseSpeed;
        }

        public void ApplyItem(ItemData item)
        {
            if (item == null) return;
            AttackPower += item.AttackBonus;
            Defense += item.DefenseBonus;
            MaxHp += item.MaxHpBonus;
            MoveSpeed += item.MoveSpeedBonus;
        }
    }
}