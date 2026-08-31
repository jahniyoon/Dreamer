using Dreamer.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamer.Enemy
{
    /// <summary>
    /// TileGridMapGenerator와 연동하여 깊이별 일반 적 동적 스폰 및
    /// 특정 심도 마일스톤에서의 보스 아레나 개척/보스 스폰을 관장하는 매니저
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("맵 생성기 참조")]
        [SerializeField] private Tile.TileGridMapGenerator mapGenerator;

        [Header("일반 적 스폰 설정")]
        [SerializeField, Range(0f, 1f)] private float globalSpawnRate = 0.15f; // 행당 적이 스폰될 기본 확률
        [SerializeField] private EnemySpawnRule[] enemySpawnRules;

        [Header("보스 스폰 마일스톤 설정")]
        [SerializeField] private BossSpawnConfig[] bossConfigs;

        private readonly HashSet<int> spawnedBossDepths = new HashSet<int>();

        private void Awake()
        {
            if (mapGenerator == null)
            {
                mapGenerator = FindObjectOfType<Tile.TileGridMapGenerator>();
            }
        }

        private void OnEnable()
        {
            if (mapGenerator != null)
            {
                mapGenerator.OnRowGenerated += HandleRowGenerated;
            }
        }

        private void OnDisable()
        {
            if (mapGenerator != null)
            {
                mapGenerator.OnRowGenerated -= HandleRowGenerated;
            }
        }

        /// <summary>
        /// TileGridMapGenerator에서 새로운 행이 스폰될 때 호출되는 이벤트 핸들러
        /// </summary>
        private void HandleRowGenerated(int yCoord, List<Vector2Int> rowPositions)
        {
            int currentDepth = Mathf.Abs(yCoord);

            // 1. 보스 등장 심도 체크
            if (CheckAndSpawnBoss(currentDepth, yCoord))
            {
                return; // 보스 방 영역에는 일반 적 스폰 중단
            }

            // 2. 일반 적 스폰 확률 체크
            if (UnityEngine.Random.value > globalSpawnRate) return;

            // 3. 해당 깊이에 적합한 적 종류 추첨
            EnemySpawnRule selectedRule = SelectEnemyRuleByDepth(currentDepth);
            if (selectedRule == null || selectedRule.EnemyPrefab == null) return;

            // 4. 해당 행의 가로 타일 중 무작위 1개 위치 선택 후 적 스폰
            if (rowPositions != null && rowPositions.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, rowPositions.Count);
                Vector2Int spawnGridPos = rowPositions[randomIndex];
                Vector3 spawnWorldPos = new Vector3(spawnGridPos.x * mapGenerator.TileSize, spawnGridPos.y * mapGenerator.TileSize, 0f);

                SpawnEnemyInstance(selectedRule.EnemyPrefab, spawnWorldPos);
            }
        }

        /// <summary>
        /// 특정 심도에 도달했는지 확인하고 보스 방을 비운 후 보스 스폰
        /// </summary>
        private bool CheckAndSpawnBoss(int currentDepth, int yCoord)
        {
            if (bossConfigs == null || bossConfigs.Length == 0) return false;

            for (int i = 0; i < bossConfigs.Length; i++)
            {
                BossSpawnConfig config = bossConfigs[i];

                if (config.TriggerDepth == currentDepth && !spawnedBossDepths.Contains(currentDepth))
                {
                    spawnedBossDepths.Add(currentDepth);

                    // A. 보스 아레나 타일 강제 제거 (공간 확보)
                    Vector2Int centerGridPos = new Vector2Int(0, yCoord);
                    mapGenerator.ClearTileArea(centerGridPos, config.ArenaSize);

                    // B. 보스 인스턴스 스폰
                    if (config.BossPrefab != null)
                    {
                        Vector3 bossWorldPos = new Vector3(0f, yCoord * mapGenerator.TileSize, 0f);
                        SpawnEnemyInstance(config.BossPrefab, bossWorldPos);
                        Debug.Log($"[EnemySpawner] ⚔️ 보스 출현! [{config.BossName}] 깊이: {currentDepth}M");
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ObjectPoolManager를 활용한 적 인스턴스 스폰
        /// </summary>
        private void SpawnEnemyInstance(GameObject prefab, Vector3 worldPos)
        {
            if (prefab == null) return;

            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.SpawnFromPool(prefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                Instantiate(prefab, worldPos, Quaternion.identity, transform);
            }
        }

        /// <summary>
        /// 깊이 가중치 무작위 추첨 알고리즘
        /// </summary>
        private EnemySpawnRule SelectEnemyRuleByDepth(int currentDepth)
        {
            if (enemySpawnRules == null || enemySpawnRules.Length == 0) return null;

            int maxTargetDepth = mapGenerator != null ? mapGenerator.MaxTargetDepth : 1655;
            float totalWeight = 0f;
            float[] weights = new float[enemySpawnRules.Length];

            for (int i = 0; i < enemySpawnRules.Length; i++)
            {
                weights[i] = enemySpawnRules[i].GetWeight(currentDepth, maxTargetDepth);
                totalWeight += weights[i];
            }

            if (totalWeight <= 0f) return null;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            for (int i = 0; i < enemySpawnRules.Length; i++)
            {
                if (weights[i] <= 0f) continue;

                cumulativeWeight += weights[i];
                if (roll <= cumulativeWeight)
                {
                    return enemySpawnRules[i];
                }
            }

            return null;
        }
    }
}