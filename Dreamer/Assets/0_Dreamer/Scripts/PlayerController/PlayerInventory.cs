using System;
using UnityEngine;
using Dreamer.Core;

namespace Dreamer.Item
{
    /// <summary>
    /// 플레이어가 채굴 및 습득한 자원(철, 다이아, 금) 수량을 관리하는 매니저
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        // 게임 중 얻는 임시 주머니
        [field: Header("습득 재화")]
        [field: SerializeField] public int IronCount { get; private set; }
        [field: SerializeField] public int DiamondCount { get; private set; }
        [field: SerializeField] public int GoldCount { get; private set; }
        [field: SerializeField] public int MushroomCount { get; private set; }
        [field: Header("보유 아이템")]
        [field: SerializeField] public string[] ItemIDs { get; private set; }


        /// <summary>
        /// 자원 보유 수량이 변경될 때 수신받는 이벤트 (철, 다이아, 금, 버섯)
        /// </summary>
        public event Action<int, int, int, int> OnResourcesChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        public void LoadSaveData()
        {
            // 초기 UI 갱신 이벤트 통보
            OnResourcesChanged?.Invoke(IronCount, DiamondCount, GoldCount, MushroomCount);
        }

        /// <summary>
        /// 획득한 광석 자원을 카운트에 추가
        /// </summary>
        public void AddResource(OreType type, int amount)
        {
            switch (type)
            {
                case OreType.Iron:
                    IronCount += amount;
                    break;
                case OreType.Diamond:
                    DiamondCount += amount;
                    break;
                case OreType.Gold:
                    GoldCount += amount;
                    break;
                case OreType.Mushroom:
                    MushroomCount += amount;
                    break;
                case OreType.RepairKit:
                    // 소모품 수리 도구인 경우 플레이어 내구도 체력 50% 즉시 회복

                    GameFlowManager.Instance.Player.Stats.HealRatio(0.25f);
                    break;
                case OreType.SparePickaxe:
                    // 소모품 수리 도구인 경우 플레이어 내구도 체력 50% 즉시 회복
                    GameFlowManager.Instance.Player.Stats.HealRatio(0.50f);

                    break;
            }

            OnResourcesChanged?.Invoke(IronCount, DiamondCount, GoldCount, MushroomCount);
            Debug.Log($"[Resource] 💎 자원 습득! type: {type}, 현재 (철:{IronCount}, 다이아:{DiamondCount}, 금:{GoldCount}), 버섯:{MushroomCount}");
        }

        public int GetResourceCount(OreType type)
        {
            return type switch
            {
                OreType.Iron => IronCount,
                OreType.Diamond => DiamondCount,
                OreType.Gold => GoldCount,
                OreType.Mushroom => MushroomCount,
                _ => 0,
            };
        }
        public void CalcurateResource()
        {
            var save = SaveManager.Instance.Data;

            // 인게임 획득 자원을 세이브 데이터 영구 재화로 합산
            save.IronCount += IronCount;
            save.DiamondCount += DiamondCount;
            save.GoldCount += GoldCount;
            save.MushroomCount += MushroomCount;

            IronCount = 0;
            DiamondCount = 0;
            GoldCount = 0;
            MushroomCount = 0;
            // 계산 모두 하고 저장
            SaveManager.Instance.SaveGame();
            Debug.Log($"[Resource] 💎 총 자원 계산 완료! 현재 (철:{save.IronCount}, 다이아:{save.DiamondCount}, 금:{save.GoldCount}), 버섯:{save.MushroomCount}");
        }

        /// <summary>
        /// 정비소 업그레이드 시 자원 소모
        /// </summary>
        public bool ConsumeResources(int iron, int diamond, int gold)
        {
            if (IronCount < iron || DiamondCount < diamond || GoldCount < gold) return false;

            IronCount -= iron;
            DiamondCount -= diamond;
            GoldCount -= gold;

            OnResourcesChanged?.Invoke(IronCount, DiamondCount, GoldCount, MushroomCount);
            return true;
        }
    }
}