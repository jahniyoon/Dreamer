using Dreamer.Core;
using Dreamer.Data;
using Dreamer.Item;
using Dreamer.Player;
using JH;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamer.UI
{
    public class UpgradeSlotUI : MonoBehaviour
    {
        [Header("설정할 스탯 타입")]
        [SerializeField] private UpgradeType upgradeType;

        [Header("UI 컴포넌트 참조")]
        [SerializeField] private TextMeshProUGUI levelText;       // 예: Lv.1
        [SerializeField] private TextMeshProUGUI statInfoText;    // 예: 공격력 10 -> 11
        [SerializeField] private TextMeshProUGUI costText;        // 예: 💎 x10
        [SerializeField] private Button upgradeButton;            // 강화 버튼

        private void Start()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(OnClickUpgrade);
            }
        }

        /// <summary>
        /// 슬롯 정보 실시간 갱신
        /// </summary>
        public void RefreshUI(SaveData save, ItemData equippedPickaxe)
        {
            if (save == null || UpgradeManager.Instance == null) return;

            int currentLevel = save.GetCurrentLevel(upgradeType);
            int cost = UpgradeManager.Instance.GetUpgradeCost(upgradeType, currentLevel);
            OreType requiredOre = save.GetMatchingOreType(upgradeType);
            int currentResource = save.GetResourceCount(requiredOre);

            // 1. 레벨 표시
            if (levelText != null) levelText.text = $"Lv.{currentLevel}";

            // 2. 수치 변화 표시 (곡괭이 Base 수치 반영)
            float baseValue = GetBaseValueByStatType(equippedPickaxe);
            float currentStat = baseValue + UpgradeManager.Instance.GetStatValue(upgradeType, currentLevel, baseValue);
            float nextStat = baseValue + UpgradeManager.Instance.GetStatValue(upgradeType, currentLevel + 1, baseValue);

            if (statInfoText != null)
            {
                statInfoText.text = $"{currentStat} ➔ <color=#00FF00>{nextStat}</color>";
            }

            // 3. 소모 비용 표시
            if (costText != null)
            {
                costText.text = $"{requiredOre} x{cost}";
            }

            // 4. 자원 부족 여부에 따라 버튼 활성화/비활성화
            if (upgradeButton != null)
            {
                upgradeButton.interactable = (currentResource >= cost);
            }
        }

        private float GetBaseValueByStatType(ItemData pickaxe)
        {
            if (pickaxe == null) return 1f;

            return upgradeType switch
            {
                UpgradeType.PickaxePower => pickaxe.BaseAttack,
                UpgradeType.MaxHp => pickaxe.BaseMaxHp,
                UpgradeType.Defense => 0.5f,
                UpgradeType.LightRadius => pickaxe.BaseLightRange,
                _ => 1f
            };
        }

        private void OnClickUpgrade()
        {
            if (UpgradeManager.Instance.TryUpgrade(upgradeType))
            {
                // 플레이어 스탯 재계산 및 전체 UI 갱신
                FindAnyObjectByType<PlayerStatsHandler>()?.ResetStats();
                transform.parent.GetComponentInParent<UIUpgrade>()?.RefreshAll();
            }
        }
    }
}