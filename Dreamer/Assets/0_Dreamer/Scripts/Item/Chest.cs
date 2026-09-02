using Dreamer.Core;
using Dreamer.Data;
using Dreamer.Item;
using Dreamer.UI;
using UnityEngine;
namespace Dreamer.Gameplay
{
    public class ChestItem : FieldItem
    {
        [Header("보상 곡괭이 데이터 (미지정 시 무작위)")]
        [SerializeField] private ItemData rewardItem;

        [Header("상자 스프라이트 연출 (옵션)")]
        [SerializeField] private Sprite openChestSprite;

        private bool isOpened = false;

        public override void InitItem(OreType type, int itemAmount, Vector2Int initialGridPos)
        {
            base.InitItem(type, itemAmount, initialGridPos);

            // 1. 보상 아이템 결정 (없으면 DB에서 지정된 항목 가져오기)
            if (rewardItem == null)
            {
                // 예: DB의 무작위/기본 아이템
                rewardItem = ItemDatabase.Instance?.GetRandomLockedItem();
            }
            if (rewardItem == null)
                this.gameObject.SetActive(false);
        }



        /// <summary>
        /// 플레이어가 상자를 오픈/파괴했을 때 호출
        /// </summary>
        public void OpenChest()
        {
            if (isOpened) return;
            isOpened = true;


            if (rewardItem != null)
            {
                // 2. 세이브 데이터에 아이템 해금 저장
                SaveManager.Instance?.Data.UnlockPickaxe(rewardItem.ItemId);
                SaveManager.Instance?.SaveGame();

                // 3. 아이템 반짝 팝업 연출 실행
                ItemAcquireEffect.Instance?.ShowAcquireEffect(rewardItem.ItemIcon, rewardItem.ItemName);

                // 4. 사운드 재생
                AudioManager.Instance?.PlaySFX("ChestOpen"); // 또는 아이템 획득 SFX
            }

            // 5. 상자 열린 이미지로 교체 후 파괴
            if (openChestSprite != null && spriteRenderer != null)
            {
                spriteRenderer.sprite = openChestSprite;
            }

            // 0.5초 뒤 상자 오브젝트 제거
            Invoke(nameof(Kill), 0.5f);
        }


        protected override void TriggerCollectEffect()
        {
            OpenChest();
        }
    }
}