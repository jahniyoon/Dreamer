using System;
using UnityEngine;

namespace Dreamer.Item
{
    /// <summary>
    /// 플레이어가 채굴 및 습득한 자원(철, 다이아, 금) 수량을 관리하는 매니저
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        public static PlayerInventory Instance { get; private set; }

        public int IronCount { get; private set; }
        public int DiamondCount { get; private set; }
        public int GoldCount { get; private set; }

        /// <summary>
        /// 자원 보유 수량이 변경될 때 수신받는 이벤트 (철, 다이아, 금)
        /// </summary>
        public event Action<int, int, int> OnResourcesChanged;

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

        private void Start()
        {
            // 초기 UI 갱신 이벤트 통보
            OnResourcesChanged?.Invoke(IronCount, DiamondCount, GoldCount);
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
                case OreType.RepairKit:
                    // 소모품 수리 도구인 경우 플레이어 내구도 체력 50% 즉시 회복
                    if (TryGetComponent<Dreamer.Player.PlayerStatsHandler>(out var stats))
                    {
                        stats.Heal(Mathf.RoundToInt(stats.CurrentStats.MaxHp * 0.5f));
                    }
                    break;
            }

            OnResourcesChanged?.Invoke(IronCount, DiamondCount, GoldCount);
            Debug.Log($"[Resource] 💎 자원 습득! type: {type}, 현재 (철:{IronCount}, 다이아:{DiamondCount}, 금:{GoldCount})");
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

            OnResourcesChanged?.Invoke(IronCount, DiamondCount, GoldCount);
            return true;
        }
    }
}