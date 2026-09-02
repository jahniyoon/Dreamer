using Dreamer.Core;
using Dreamer.Data;
using Dreamer.UI;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

namespace Dreamer.Player
{

    /// <summary>
    /// 플레이어의 스탯 연산, 체력 관리 및 장비 스탯 합산을 전담하는 컴포넌트
    /// </summary>
    public class PlayerStatsHandler : MonoBehaviour, IDamageable
    {
        [Header("기본 스탯 (Base Stats)")]
        [SerializeField] private int baseMaxHp = 100;
        [SerializeField] private int baseAttack = 1;
        [SerializeField] private int baseDefense = 0;
        [SerializeField] private float attackCooldown = 0.2f;

        [field: Header("현재 스탯 (Current Stats)")]
        [field: SerializeField] public int CurrentHp { get; private set; }
        [field: SerializeField] public CalculatedPlayerStats CurrentStats { get; private set; }
        private PlayerVisual visualHandler;
        public bool IsDead => CurrentHp <= 0;
        public int Hardness => 0; // 플레이어는 곡괭이로 데미지를 받지 않으므로 0으로 설정 
        public event Action<int, int> OnHpChanged; // (currentHp, maxHp)
        public event Action<ItemData> OnPickaxeChanged;
        public event Action OnPlayerDied;

        private void Awake()
        {
            visualHandler = GetComponent<PlayerVisual>();
        }


        public void ResetStats()
        {
            CalculatedPlayerStats stats = new CalculatedPlayerStats();

            SaveData save = SaveManager.Instance != null ? SaveManager.Instance.Data : null;
            UpgradeManager um = UpgradeManager.Instance;

            int finalMaxHp = baseMaxHp;
            int finalAttack = baseAttack;
            int finalDefense = baseDefense;
            ItemData currentPickaxe = ItemDatabase.Instance?.GetItemByID(save.EquippedPickaxeId);
            
            if (currentPickaxe != null)
            {
                finalMaxHp = currentPickaxe.BaseMaxHp;
                finalAttack = currentPickaxe.BaseAttack;
                finalDefense = currentPickaxe.BaseDefense;
            }

            if (save != null && um != null)
            {

                // 각 스탯의 baseValue(기본 증가 단위 수치)를 전달
                int hpBonus = Mathf.RoundToInt(um.GetStatValue(UpgradeType.MaxHp, save.MaxHpLevel, currentPickaxe.BaseMaxHp));         // 예: 레벨당 +20
                int attackBonus = Mathf.RoundToInt(um.GetStatValue(UpgradeType.PickaxePower, save.PickaxePowerLevel, currentPickaxe.BaseAttack)); // 예: 레벨당 +1
                int defenseBonus = Mathf.RoundToInt(um.GetStatValue(UpgradeType.Defense, save.DefenseLevel, currentPickaxe.BaseDefense));            // 예: 레벨당 +0.5

                finalMaxHp += hpBonus;
                finalAttack += attackBonus;
                finalDefense += defenseBonus;
            }

            stats.ResetToBase(finalMaxHp, finalAttack, finalDefense, attackCooldown);
            CurrentStats = stats;
            CurrentHp = CurrentStats.MaxHp;

            OnHpChanged?.Invoke(CurrentHp, CurrentStats.MaxHp);
            OnPickaxeChanged?.Invoke(currentPickaxe);
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
            if (IsDead)
                return;
            int actualDamage = Mathf.Max(1, damage - CurrentStats.Defense);
            CurrentHp = Mathf.Max(0, CurrentHp - actualDamage);

            OnHpChanged?.Invoke(CurrentHp, CurrentStats.MaxHp);

            if (CurrentHp <= 0)
            {
                OnPlayerDied?.Invoke();
            }
            visualHandler.PlayHitFlash();

            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnDamageText(transform.position, damage, isPlayerDamage: true);
            }
        }
        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(CurrentStats.MaxHp, CurrentHp + amount);
            OnHpChanged?.Invoke(CurrentHp, CurrentStats.MaxHp);
        }
        public void HealRatio(float ratio)
        {
            var amount = Mathf.RoundToInt(CurrentStats.MaxHp * ratio);
            CurrentHp = Mathf.Min(CurrentStats.MaxHp, CurrentHp + amount);
            OnHpChanged?.Invoke(CurrentHp, CurrentStats.MaxHp);
        }
    }
}
