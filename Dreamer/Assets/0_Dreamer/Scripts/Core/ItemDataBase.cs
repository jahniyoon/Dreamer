using Dreamer.Data;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Dreamer.Core
{
    /// <summary>
    /// 게임 내 모든 ItemData(곡괭이 SO) 에셋을 ItemId 기반으로 추적하고 반환하는 매니저
    /// </summary>
    public class ItemDatabase : MonoBehaviour
    {
        public static ItemDatabase Instance { get; private set; }

        [Header("아이템 데이터베이스 리스트 (수동 등록 시 사용)")]
        [SerializeField] private List<ItemData> itemList = new List<ItemData>();

        // ItemId -> ItemData 빠른 조회를 위한 딕셔너리
        private readonly Dictionary<string, ItemData> itemDict = new Dictionary<string, ItemData>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDatabase();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 데이터베이스 초기화 (프로젝트 내 아이템 등록)
        /// </summary>
        private void InitializeDatabase()
        {
            itemDict.Clear();

            // 1. [방법 A] Resources/Items 폴더 안의 모든 ItemData를 자동 로드하는 방식 (추천)
            ItemData[] loadedItems = Resources.LoadAll<ItemData>("Items");
            foreach (var item in loadedItems)
            {
                RegisterItem(item);
            }

            // 2. [방법 B] 인스펙터 리스트(itemList)에 수동으로 넣어둔 에셋 등록
            foreach (var item in itemList)
            {
                RegisterItem(item);
            }

            Debug.Log($"📦 [ItemDatabase] 총 {itemDict.Count}개의 곡괭이 아이템 데이터베이스 로드 완료!");
        }

        /// <summary>
        /// 딕셔너리에 아이템 등록 (중복 체크)
        /// </summary>
        private void RegisterItem(ItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.ItemId)) return;

            var cleanID = item.ItemId.Trim();
            if (!itemDict.ContainsKey(cleanID))
            {
                itemDict.Add(item.ItemId, item);
            }
            else
            {
                Debug.LogWarning($"⚠️ [ItemDatabase] 중복된 ItemId가 존재합니다: {item.ItemId}");
            }
        }

        /// <summary>
        /// ItemId 문자열로 실제 ScriptableObject ItemData 에셋 반환
        /// </summary>
        public ItemData GetItemByID(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                Debug.LogWarning("⚠️ [ItemDatabase] 요청한 ItemId가 빈 문자열입니다.");
                return null;
            }

            // 2. 찾지 못했거나 itemId가 비어있다면 Dict의 첫 번째 아이템 반환
            if (itemDict != null && itemDict.Count > 0)
            {
                var cleanID = itemId.Trim();
                if (itemDict.ContainsKey(cleanID))
                    return itemDict[cleanID];

                Debug.LogWarning($"⚠️ [ItemDatabase] ID({itemId})를 찾을 수 없거나 비어있어 첫 번째 아이템({itemDict.Values.First().ItemId})을 반환합니다.");
                return itemDict.Values.First();
            }

            Debug.LogError($"❌ [ItemDatabase] ID에 해당하는 아이템을 찾을 수 없습니다: {itemId}");
            return null;
        }

        /// <summary>
        /// 전체 곡괭이 아이템 목록 반환 (상점/인벤토리 UI 생성용)
        /// </summary>
        public IEnumerable<ItemData> GetAllItems()
        {
            return itemDict.Values;
        }

        /// <summary>
        /// 아직 해금되지 않은 아이템 중 무작위 1개 반환 (모두 해금 시 null 반환)
        /// </summary>
        public ItemData GetRandomLockedItem()
        {
            if (itemDict == null || itemDict.Count == 0)
            {
                Debug.LogWarning("⚠️ [ItemDatabase] 아이템 데이터베이스가 비어있습니다.");
                return null;
            }

            // 1. 현재 세이브 데이터에서 해금된 아이템 ID 목록 가져오기
            List<string> unlockedIds = SaveManager.Instance?.Data?.InventoryItemIds ?? new List<string>();

            // 2. 전체 아이템 중 '해금 안 된(미소유)' ItemData들만 추출
            List<ItemData> lockedItems = itemDict.Values
                .Where(item => item != null && !unlockedIds.Contains(item.ItemId.Trim()))
                .ToList();

            // 3. 만약 모든 아이템을 이미 다 얻었다면?
            if (lockedItems.Count == 0)
            {
                Debug.Log("🎉 [ItemDatabase] 이미 모든 아이템을 획득했습니다!");
                return null; // 또는 기본 광석/재화 아이템 반환 처리
            }

            // 4. 미해금 아이템 중 무작위 1개 선택
            int randomIndex = Random.Range(0, lockedItems.Count);
            return lockedItems[randomIndex];
        }
    }
}