using Dreamer.Item;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamer.Data
{
    [System.Serializable]
    public class SaveData
    {
        [field: SerializeField] public int IronCount = 0;
        [field: SerializeField] public int DiamondCount = 0;
        [field: SerializeField] public int GoldCount = 0;
        [field: SerializeField] public int MushroomCount = 0;
        [field: SerializeField] public int BestDeapth = 0;

        // 업그레이드 스탯 레벨 (기본 1레벨)
        [field: SerializeField] public int PickaxePowerLevel { get; private set; } = 1;  // 곡괭이 파괴력
        [field: SerializeField] public int MaxHpLevel { get; private set; } = 1;         // 최대 체력
        [field: SerializeField] public int MoveSpeedLevel { get; private set; } = 1;     // 이동 속도
        [field: SerializeField] public int LightRadiusLevel { get; private set; } = 1;   // 암전 해제/시야 범위

        // 3. 착용 중인 장비 아이템 ID (기본값 설정 가능)
        [field: SerializeField] public string EquippedPickaxeId { get; set; } = "pickaxe_01_wood";


        // 4. 소유 중인 인벤토리 아이템 ID 리스트
        [field: SerializeField] public List<string> InventoryItemIds { get; set; } = new List<string>();



        public void EquipItem(ItemData item)
        {
            if (item == null || string.Equals(EquippedPickaxeId, item.ItemId)) 
                return;
            EquippedPickaxeId = item.ItemId;      
        }
        public bool IsPickaxeUnlocked(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            // 기본 곡괭이이거나 인벤토리 리스트에 포함되어 있다면 true
            return itemId == EquippedPickaxeId || InventoryItemIds.Contains(itemId);
        }

        public void UnlockPickaxe(string itemId)
        {
            if (!InventoryItemIds.Contains(itemId))
            {
                InventoryItemIds.Add(itemId);
            }
        }

        #region 광석 자원 관리 (OreType 자동 매칭)

        /// <summary>
        /// 습득한 OreType에 맞춰 자원을 즉시 세이브 데이터에 누적 추가
        /// </summary>
        public void AddResource(OreType type, int amount)
        {
            switch (type)
            {
                case OreType.Iron: IronCount += amount; break;
                case OreType.Diamond: DiamondCount += amount; break;
                case OreType.Gold: GoldCount += amount; break;
                case OreType.Mushroom: MushroomCount += amount; break;
            }
        }

        /// <summary>
        /// 특정 OreType의 현재 보유 수량 반환
        /// </summary>
        public int GetResourceCount(OreType type)
        {
            return type switch
            {
                OreType.Iron => IronCount,
                OreType.Diamond => DiamondCount,
                OreType.Gold => GoldCount,
                OreType.Mushroom => MushroomCount,
                _ => 0
            };
        }

        /// <summary>
        /// 특정 OreType 자원 차감 (충분할 경우 차감 후 true 반환)
        /// </summary>
        public bool ConsumeResource(OreType type, int amount)
        {
            if (GetResourceCount(type) < amount) return false;

            switch (type)
            {
                case OreType.Iron: IronCount -= amount; break;
                case OreType.Diamond: DiamondCount -= amount; break;
                case OreType.Gold: GoldCount -= amount; break;
                case OreType.Mushroom: MushroomCount -= amount; break;
            }
            return true;
        }

        #endregion

        #region UpgradeType ↔ OreType 매핑 헬퍼

        /// <summary>
        /// 업그레이드 스탯 종류에 매핑되는 전용 OreType 반환
        /// </summary>
        public OreType GetMatchingOreType(UpgradeType upgradeType)
        {
            return upgradeType switch
            {
                UpgradeType.PickaxePower => OreType.Diamond,
                UpgradeType.MaxHp => OreType.Gold,
                UpgradeType.MoveSpeed => OreType.Iron,
                UpgradeType.LightRadius => OreType.Mushroom,
                _ => OreType.Iron
            };
        }

        public int GetCurrentLevel(UpgradeType type)
        {
            return type switch
            {
                UpgradeType.PickaxePower => PickaxePowerLevel,
                UpgradeType.MaxHp => MaxHpLevel,
                UpgradeType.MoveSpeed => MoveSpeedLevel,
                UpgradeType.LightRadius => LightRadiusLevel,
                _ => 1
            };
        }

        public void SetCurrentLevel(UpgradeType type, int newLevel)
        {
            switch (type)
            {
                case UpgradeType.PickaxePower: PickaxePowerLevel = newLevel; break;
                case UpgradeType.MaxHp: MaxHpLevel = newLevel; break;
                case UpgradeType.MoveSpeed: MoveSpeedLevel = newLevel; break;
                case UpgradeType.LightRadius: LightRadiusLevel = newLevel; break;
            }
        }

        #endregion
    }

    public enum UpgradeType
    {
        PickaxePower,
        MaxHp,
        MoveSpeed,
        LightRadius
    }
}

