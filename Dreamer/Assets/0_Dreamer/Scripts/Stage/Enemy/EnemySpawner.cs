using DG.Tweening;
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
        Transform enemyParent;

        private readonly HashSet<int> spawnedBossDepths = new HashSet<int>();

        private void Awake()
        {
            if (mapGenerator == null)
            {
                mapGenerator = GetComponent<Tile.TileGridMapGenerator>();
            }
            enemyParent = new GameObject("EnemyParent").transform;
            enemyParent.parent = transform;
        }

        private void OnEnable()
        {
            if (mapGenerator != null)
            {
                mapGenerator.OnRowGenerated += HandleRowGenerated;
                mapGenerator.OnMapReset += HandleMapReset;
            }
        }

        private void OnDisable()
        {
            if (mapGenerator != null)
            {
                mapGenerator.OnRowGenerated -= HandleRowGenerated;
                mapGenerator.OnMapReset -= HandleMapReset;
            }
        }
        /// <summary>
        /// 맵 리셋 시 활성화되어 있던 모든 적들을 풀로 회수하고 보스 스폰 기록 초기화
        /// </summary>
        private void HandleMapReset()
        {
            if (enemyParent != null)
            {
                // enemyParent 하위의 모든 활성화된 적 개체 탐색
                EnemyBase[] activeEnemies = enemyParent.GetComponentsInChildren<EnemyBase>(true);

                for (int i = 0; i < activeEnemies.Length; i++)
                {
                    if (activeEnemies[i] != null && activeEnemies[i].gameObject.activeSelf)
                    {
                        activeEnemies[i].Kill();
                    }
                }
            }

            // 보스 출현 심도 기록 초기화
            spawnedBossDepths.Clear();
            Debug.Log("[EnemySpawner] 👾 적 및 보스 스폰 기록 완전 리셋 완료!");
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
                return;
            }

            // 2. 일반 적 스폰 확률 체크
            if (UnityEngine.Random.value > globalSpawnRate) return;

            // 3. 해당 깊이에 적합한 적 종류 추첨
            EnemySpawnRule selectedRule = SelectEnemyRuleByDepth(currentDepth);
            if (selectedRule == null || selectedRule.EnemyPrefab == null)
                return;

            // 4. 해당 행의 가로 타일 중 무작위 1개 위치 선택 후 적 스폰
            if (rowPositions != null && rowPositions.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, rowPositions.Count);
                Vector2Int spawnGridPos = rowPositions[randomIndex];

                float tileSize = mapGenerator != null ? mapGenerator.TileSize : 1f;
                Vector3 spawnWorldPos = new Vector3(spawnGridPos.x * tileSize, spawnGridPos.y * tileSize, 0f);

                SpawnEnemyInstance(selectedRule, spawnGridPos, spawnWorldPos);
            }
        }

        private bool CheckAndSpawnBoss(int currentDepth, int yCoord)
        {
            if (bossConfigs == null || bossConfigs.Length == 0) return false;

            for (int i = 0; i < bossConfigs.Length; i++)
            {
                BossSpawnConfig config = bossConfigs[i];

                if (config.TriggerDepth == currentDepth && !spawnedBossDepths.Contains(currentDepth))
                {
                    spawnedBossDepths.Add(currentDepth);

                    Vector2Int centerGridPos = new Vector2Int(0, yCoord);
                    if (mapGenerator != null) mapGenerator.ClearTileArea(centerGridPos, config.ArenaSize);

                    if (config.BossPrefab != null)
                    {
                        float tileSize = mapGenerator != null ? mapGenerator.TileSize : 1f;
                        Vector3 bossWorldPos = new Vector3(0f, yCoord * tileSize, 0f);
                        SpawnEnemyInstance(null, centerGridPos, bossWorldPos, config.BossPrefab);
                        Debug.Log($"[EnemySpawner] ⚔️ 보스 출현! [{config.BossName}] 깊이: {currentDepth}M");
                    }

                    return true;
                }
            }

            return false;
        }
        private void SpawnEnemyInstance(EnemySpawnRule rule, Vector2Int gridPos, Vector3 worldPos, GameObject overridePrefab = null)
        {
            GameObject prefab = overridePrefab != null ? overridePrefab : rule?.EnemyPrefab;
            if (prefab == null) return;

            // 스폰할 위치의 암석 타일을 먼저 비워주어 적이 암석 타일과 겹치는 현상 완전 방지
            if (mapGenerator != null)
            {
                mapGenerator.ClearTileArea(gridPos, Vector2Int.one);
            }

            GameObject enemyObj = null;

            if (ObjectPoolManager.Instance != null)
            {
                enemyObj = ObjectPoolManager.Instance.SpawnFromPool(prefab, worldPos, Quaternion.identity, enemyParent);
            }
            else
            {
                enemyObj = Instantiate(prefab, worldPos, Quaternion.identity, enemyParent);
            }

            if (enemyObj != null)
            {
                if (enemyObj.TryGetComponent<EnemyBase>(out var enemy))
                {
                    // 데이터 및 그리드 좌표 초기화
                    enemy.InitEnemy(rule != null ? rule.EnemyData : null, gridPos);
                    Debug.Log($"[EnemySpawner] 👾 적 스폰 성공! 종류: {enemyObj.name}, 위치: {gridPos}");
                }
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