using Dreamer.Data;
using Dreamer.Item;
using UnityEngine;

namespace Dreamer.Core
{
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public int GetUpgradeCost(UpgradeType type, int currentLevel)
        {
            return currentLevel * 10; // 필요 수량 공식
        }
        /// <summary>
        /// 레벨에 따른 스탯 수치/보너스 계산 함수
        /// </summary>
        public float GetStatValue(UpgradeType type, int level, float baseValue)
        {
            // 1레벨(기본)일 때는 보너스 0, 레벨업 시 계수에 맞춰 보너스 제공
            return type switch
            {
                UpgradeType.PickaxePower => (level - 1) * baseValue,    // 파괴력: 레벨당 +2 (다이아)
                UpgradeType.MaxHp => (level - 1) * baseValue,         // 최대 체력: 레벨당 +25 (골드)
                UpgradeType.Defense => (level - 1) * baseValue,     // 이동 속도: 레벨당 +0.5 (철)
                UpgradeType.LightRadius => (level - 1) * baseValue,   // 암전 시야: 레벨당 +1.5 (버섯)
                _ => 0f
            };
        }
        /// <summary>
        /// 업그레이드 시도 (타입에 자동 매칭된 OreType 자원 소모 및 세이브)
        /// </summary>
        public bool TryUpgrade(UpgradeType type)
        {
            SaveData save = SaveManager.Instance?.Data;
            if (save == null) return false;

            int currentLevel = save.GetCurrentLevel(type);
            int cost = GetUpgradeCost(type, currentLevel);

            // 1. 해당 업그레이드에 매핑된 OreType 가져오기
            OreType matchingOre = save.GetMatchingOreType(type);

            // 2. 해당 광석 차감 시도
            if (PlayerInventory.Instance.ConsumeResources(matchingOre, cost));
            {
                save.SetCurrentLevel(type, currentLevel + 1);

                // 3. 변경 사항 즉시 영구 저장!
                SaveManager.Instance.SaveGame();
                JuiceManager.Instance?.ShakeCamera(0.15f);

                Debug.Log($"🎉 [{type}] 업그레이드 성공! (소모 광석: {matchingOre} x{cost} / 현재 레벨: {currentLevel + 1})");
                return true;
            }

            Debug.Log($"❌ [{type}] 자원 부족! (필요 광석: {matchingOre} x{cost} / 현재: {save.GetResourceCount(matchingOre)})");
            return false;
        }
    }



}
