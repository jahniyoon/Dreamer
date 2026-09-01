using Dreamer.Data;
using System;
using UnityEngine;

namespace Dreamer.Player
{

    /// <summary>
    /// 플레이어의 스탯 연산, 체력 관리 및 장비 스탯 합산을 전담하는 컴포넌트
    /// </summary>
    public class PlayerStatsHandler : MonoBehaviour
    {
        [Header("기본 스탯 (Base Stats)")]
        [SerializeField] private int baseMaxHp = 100;
        [SerializeField] private int baseAttack = 1;
        [SerializeField] private int baseDefense = 0;
        [SerializeField] private float baseMoveSpeed = 5f;
        [SerializeField] private float attackCooldown = 0.2f;

        [field: Header("현재 스탯 (Current Stats)")]
        [field: SerializeField] public int CurrentHp { get; private set; }
        [field: SerializeField] public CalculatedPlayerStats CurrentStats { get; private set; }
        private PlayerVisual visualHandler;

        public event Action<int, int> OnHpChanged; // (currentHp, maxHp)
        public event Action OnPlayerDied;

        private void Awake()
        {
            ResetStats();
            visualHandler = GetComponent<PlayerVisual>();
        }

        public void ResetStats()
        {
            CalculatedPlayerStats stats = new CalculatedPlayerStats();
            stats.ResetToBase(baseMaxHp, baseAttack, baseDefense, baseMoveSpeed, attackCooldown);
            CurrentStats = stats;
            CurrentHp = CurrentStats.MaxHp;
            OnHpChanged?.Invoke(CurrentHp, CurrentStats.MaxHp);
        }

        public void ApplyEquipment(ItemData item)
        {
            CalculatedPlayerStats stats = CurrentStats;
            stats.ApplyItem(item);
            CurrentStats = stats;
            OnHpChanged?.Invoke(CurrentHp, CurrentStats.MaxHp);
        }

        public void TakeDamage(int damage)
        {
            int actualDamage = Mathf.Max(1, damage - CurrentStats.Defense);
            CurrentHp = Mathf.Max(0, CurrentHp - actualDamage);

            OnHpChanged?.Invoke(CurrentHp, CurrentStats.MaxHp);

            if (CurrentHp <= 0)
            {
                OnPlayerDied?.Invoke();
            }
            visualHandler.PlayHitFlash();
        }
    }
}
