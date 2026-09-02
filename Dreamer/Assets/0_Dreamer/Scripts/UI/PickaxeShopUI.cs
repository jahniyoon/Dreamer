using Dreamer.Core;
using Dreamer.Data;
using Dreamer.Item;
using Dreamer.Player;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamer.UI
{
    public class PickaxeShopUI : UIObject
    {
        [Header("UI 컴포넌트 참조")]
        [SerializeField] private Image pickaxeIconImage;
        [SerializeField] private TextMeshProUGUI pickaxeNameText;
        [SerializeField] private TextMeshProUGUI pickaxeStatsText;
        [SerializeField] private TextMeshProUGUI pageText; // 예: "1/33"
        [SerializeField] private TextMeshProUGUI priceText; 
        [SerializeField] private GameObject price; 

        [Header("구매 / 착용 버튼 참조")]
        [SerializeField] private Button actionButton; // 구매 또는 착용 통합 버튼
        [SerializeField] private TextMeshProUGUI actionButtonText;

        [Header("좌우 탐색 버튼")]
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;

        private List<ItemData> allPickaxes = new List<ItemData>();
        private int currentIndex = 0;

        private void Awake()
        {
            if (prevButton != null) prevButton.onClick.AddListener(OnPrevButtonClicked);
            if (nextButton != null) nextButton.onClick.AddListener(OnNextButtonClicked);
            if (actionButton != null) actionButton.onClick.AddListener(OnActionButtonClicked);
        }

        private void OnEnable()
        {
            InitPickaxeList();
            RefreshPageUI();
        }
        public override void Show()
        {
            base.Show();
            InitPickaxeList();
            RefreshPageUI();
        }

        /// <summary>
        /// ItemDatabase에서 곡괭이 전체 리스트 로드
        /// </summary>
        private void InitPickaxeList()
        {
            if (ItemDatabase.Instance != null)
            {
                allPickaxes = ItemDatabase.Instance.GetAllItems().ToList();
            }

            if (allPickaxes.Count == 0)
            {
                Debug.LogWarning("⚠️ [PickaxeShopUI] 등록된 곡괭이 아이템이 없습니다!");
            }
        }

        /// <summary>
        /// 현재 인덱스의 곡괭이 정보 UI 표시
        /// </summary>
        private void RefreshPageUI()
        {
            if (allPickaxes.Count == 0) return;

            SaveData save = SaveManager.Instance?.Data;
            if (save == null) return;

            ItemData currentItem = allPickaxes[currentIndex];
            bool isUnlocked = save.IsPickaxeUnlocked(currentItem.ItemId);
            bool isEquipped = (save.EquippedPickaxeId == currentItem.ItemId);

            // 1. 페이지 카운터 표시 (예: 1/33)
            if (pageText != null)
            {
                pageText.text = $"{currentIndex + 1} / {allPickaxes.Count}";
            }

            // 2. 곡괭이 아이콘 및 검정 실루엣(실루엣 시 검정 비활성화) 처리
            if (pickaxeIconImage != null)
            {
                pickaxeIconImage.sprite = currentItem.ItemIcon;
                // 미해금 시 검정색(Color.black), 해금 시 원본 색상(Color.white)
                pickaxeIconImage.color = isUnlocked ? Color.white : new Color(0.15f, 0.15f, 0.15f, 1f);
            }

            // 3. 이름 및 능력치 표시
            if (pickaxeNameText != null)
            {
                pickaxeNameText.text = isUnlocked ? currentItem.ItemName : "???";
            }

            if (pickaxeStatsText != null)
            {
                pickaxeStatsText.text = $"Atk: {currentItem.BaseAttack} | HP: {currentItem.BaseMaxHp} | Def: {currentItem.BaseDefense}";
            }
            if(priceText != null)
            {
                priceText.text = $"{currentItem.GetPriceString()}<size=30%>x</size>{currentItem.PriceValue}";

            }
            if (price != null)
            {
                price.gameObject.SetActive(!isUnlocked);
            }

            // 4. 구매 / 착용 / 착용중 버튼 상태 설정
            if (actionButton != null && actionButtonText != null && price != null)
            {
                if (isEquipped)
                {
                    actionButtonText.text = "Equipped";
                    actionButton.interactable = false;
                }
                else if (isUnlocked)
                {
                    actionButtonText.text = "Equip";
                    actionButton.interactable = true;
                }
                else
                {
                    // 미해금 -> 구매 버튼
                    int resourceHave = save.GetResourceCount(currentItem.PriceType);
                    bool canAfford = resourceHave >= currentItem.PriceValue;

                    actionButtonText.text = $"Purchase";
                    actionButton.interactable = canAfford;
                }
            }
        }



        #region 버튼 클릭 이벤트 처리

        // ◀ 이전 버튼 (1페이지에서 누르면 마지막 페이지로)
        private void OnPrevButtonClicked()
        {
            if (allPickaxes.Count == 0) return;
            currentIndex = (currentIndex - 1 + allPickaxes.Count) % allPickaxes.Count;
            RefreshPageUI();
        }

        // ▶ 다음 버튼 (마지막 페이지에서 누르면 1페이지로)
        private void OnNextButtonClicked()
        {
            if (allPickaxes.Count == 0) return;
            currentIndex = (currentIndex + 1) % allPickaxes.Count;
            RefreshPageUI();
        }

        // Action 버튼 (구매 또는 장착)
        private void OnActionButtonClicked()
        {
            if (allPickaxes.Count == 0) return;

            SaveData save = SaveManager.Instance?.Data;
            if (save == null) return;

            ItemData currentItem = allPickaxes[currentIndex];
            bool isUnlocked = save.IsPickaxeUnlocked(currentItem.ItemId);

            if (isUnlocked)
            {
                // 이미 해금된 경우 -> 장착
                save.EquipItem(currentItem);
                SaveManager.Instance.SaveGame();

                // 플레이어 스탯 실시간 재계산
                GameFlowManager.Instance.Player.Stats.ResetStats();
                RefreshPageUI();
            }
            else
            {
                // 미해금인 경우 -> 자원 차감 후 구매 처리
                if (PlayerInventory.Instance.ConsumeResources(currentItem.PriceType, currentItem.PriceValue))
                {
                    save.UnlockPickaxe(currentItem.ItemId);
                    save.EquipItem(currentItem); // 구매 시 자동 장착
                    SaveManager.Instance.SaveGame();

                    // 플레이어 스탯 반영 및 UI 갱신
                    GameFlowManager.Instance.Player.Stats.ResetStats();
                    RefreshPageUI();

                    Debug.Log($"🎉 [{currentItem.ItemName}] 구매 완료!");
                }
            }
        }

        #endregion
    }
}
