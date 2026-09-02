using Dreamer.Core;
using Dreamer.Data;
using Dreamer.UI;
using TMPro;
using UnityEngine;

namespace JH
{
    public class UIUpgrade : UIObject
    {

        [Header("상단 자원 텍스트")]
        [SerializeField] private TextMeshProUGUI ironText;
        [SerializeField] private TextMeshProUGUI diamondText;
        [SerializeField] private TextMeshProUGUI goldText;
        [SerializeField] private TextMeshProUGUI mushroomText;

        [Header("하위 슬롯들")]
        [SerializeField] private PickaxeShopUI shopUI;

        public override void Show()
        {
            base.Show();
            RefreshAll();
        }

        public void TitleButton()
        {
            Hide();
            UIManager.Instance.TitleUI.Show();
        }

        public void RefreshAll()
        {
            SaveData save = SaveManager.Instance?.Data;
            if (save == null) return;

            // 1. 상단 재화 UI 갱신
            if (ironText != null) ironText.text = $"{save.IronCount}";
            if (diamondText != null) diamondText.text = $"{save.DiamondCount}";
            if (goldText != null) goldText.text = $"{save.GoldCount}";
            if (mushroomText != null) mushroomText.text = $"{save.MushroomCount}";

            // 2. 착용 장비 정보 조회
            ItemData equippedPickaxe = ItemDatabase.Instance?.GetItemByID(save.EquippedPickaxeId);

            shopUI.Show();
        }
    }
}