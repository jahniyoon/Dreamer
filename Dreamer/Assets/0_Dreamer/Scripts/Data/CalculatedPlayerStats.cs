using UnityEngine;

namespace Dreamer.Data
{
    [System.Serializable]
    public struct CalculatedPlayerStats
    {
        [field: SerializeField] public ItemData ItemData { get; private set; }
        [field: SerializeField] public int MaxHp { get; private set; }
        [field: SerializeField] public int AttackPower { get; private set; }
        [field: SerializeField] public int Defense { get; private set; }
        [field: SerializeField] public float LightRange { get; private set; }

        public void ResetToBase(int baseHp, int baseAtk, int baseDef, float baseLight)
        {
            MaxHp = baseHp;
            AttackPower = baseAtk;
            Defense = baseDef;
            LightRange = baseLight;
        }

        public void ApplyItem(ItemData item)
        {
            if (item == null) return;
            ItemData = item;
        }
    }
}