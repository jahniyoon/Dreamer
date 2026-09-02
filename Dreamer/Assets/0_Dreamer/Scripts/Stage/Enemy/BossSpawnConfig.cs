using Dreamer.Data;
using UnityEngine;
namespace Dreamer.Enemy
{
    /// <summary>
    /// 특정 목표 심도 도달 시 보스 아레나 생성 및 보스 스폰 설정
    /// </summary>
    [System.Serializable]
    public class BossSpawnConfig
    {
        [SerializeField] private string bossName = "Boss Encounter";
        [SerializeField] private EnemyData bossData;
        [SerializeField] private GameObject bossPrefab;
        [SerializeField] private int triggerDepth = 500;            // 보스가 등장할 심도 (절대 깊이 M)
        [SerializeField] private Vector2Int arenaSize = new Vector2Int(5, 4); // 보스 방을 비울 영역 (가로, 세로)

        public string BossName => bossName;
        public GameObject BossPrefab => bossPrefab;
        public int TriggerDepth => triggerDepth;
        public Vector2Int ArenaSize => arenaSize;
        public EnemyData BossData => bossData;
    }

}