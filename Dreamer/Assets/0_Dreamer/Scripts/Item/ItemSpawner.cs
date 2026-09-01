using Dreamer.Core;
using Dreamer.Enemy;
using Dreamer.Tile;
using System.Collections.Generic;
using UnityEngine;
namespace Dreamer.Item
{
    /// <summary>
    /// 깊이(M)에 따른 아이템/소모품 스폰 가중치 설정 클래스
    /// </summary>
    [System.Serializable]
    public class ItemSpawnRule
    {
        [SerializeField] private string ruleName = "New Item Rule";
        [SerializeField] private OreType itemType = OreType.Iron;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private int minDepth = 0;
        [SerializeField] private AnimationCurve weightByDepth = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        public string RuleName => ruleName;
        public OreType ItemType => itemType;

        public GameObject ItemPrefab => itemPrefab;
        public int MinDepth => minDepth;

        public float GetWeight(int currentDepth, int maxTargetDepth)
        {
            if (currentDepth < minDepth || itemPrefab == null) return 0f;

            float normalizedDepth = Mathf.Clamp01((float)currentDepth / Mathf.Max(1, maxTargetDepth));
            return Mathf.Max(0.01f, weightByDepth.Evaluate(normalizedDepth));
        }
    }

    public class ItemSpawner : MonoBehaviour
    {
        [Header("맵 생성기 참조")]
        [SerializeField] private TileGridMapGenerator mapGenerator;

        [Header("스폰 확률 및 제한 설정")]
        [SerializeField, Range(0f, 1f)] private float globalItemSpawnRate = 0.3f;
        [SerializeField, Range(1, 3)] private int maxItemsPerRow = 1;

        [Header("깊이별 아이템 스폰 테이블")]
        [SerializeField] private ItemSpawnRule[] itemRules;

        [Header("중복 스폰 방지 레이어 설정")]
        [SerializeField] private LayerMask occupiedLayers;


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

        private void HandleRowGenerated(int yCoord, List<Vector2Int> rowPositions)
        {
            if (rowPositions == null || rowPositions.Count == 0) return;

            // 1. 스폰 확률 검사 (0.05 등 낮은 수치 방지)
            if (Random.Range(0f, 1f) > globalItemSpawnRate)
            {
                return;
            }

            int currentDepth = Mathf.Abs(yCoord);
            int maxDepth = mapGenerator != null ? mapGenerator.MaxTargetDepth : 1655;
            float tileSize = mapGenerator != null ? mapGenerator.TileSize : 1f;

            List<Vector2Int> availablePositions = new List<Vector2Int>(rowPositions);
            FilterOccupiedPositions(availablePositions, tileSize);

            if (availablePositions.Count == 0)
            {
                Debug.LogWarning($"[ItemSpawner] Y:{yCoord} 행의 모든 타일 위치가 점유되어 스폰 불가!");
                return;
            }

            int spawnCount = Mathf.Min(Random.Range(1, maxItemsPerRow + 1), availablePositions.Count);

            for (int i = 0; i < spawnCount; i++)
            {
                if (availablePositions.Count == 0) break;

                ItemSpawnRule selectedRule = SelectItemRuleByDepth(currentDepth, maxDepth);
                if (selectedRule == null || selectedRule.ItemPrefab == null)
                {
                    Debug.LogWarning($"[ItemSpawner] 심도 {currentDepth}M에서 가중치 추첨 실패!");
                    continue;
                }

                int randomIndex = Random.Range(0, availablePositions.Count);
                Vector2Int spawnGridPos = availablePositions[randomIndex];
                availablePositions.RemoveAt(randomIndex);

                // 지층 타일 깔끔히 제거
                if (mapGenerator != null)
                {
                    mapGenerator.ClearTileArea(spawnGridPos, Vector2Int.one);
                }

                Vector3 spawnWorldPos = new Vector3(spawnGridPos.x * tileSize, spawnGridPos.y * tileSize, 0f);
                SpawnItemInstance(selectedRule, spawnGridPos, spawnWorldPos);
            }
        }

        private void FilterOccupiedPositions(List<Vector2Int> positions, float tileSize)
        {
            for (int i = positions.Count - 1; i >= 0; i--)
            {
                Vector3 checkWorldPos = new Vector3(positions[i].x * tileSize, positions[i].y * tileSize, 0f);
                if (IsPositionOccupiedByEntity(checkWorldPos, tileSize))
                {
                    positions.RemoveAt(i);
                }
            }
        }

        private bool IsPositionOccupiedByEntity(Vector3 worldPos, float tileSize)
        {
            // 인스펙터 설정 레이어(Enemy)만 타격 검사 (반지름을 타일 크기의 0.2배로 축소해 정확도 향상)
            if (occupiedLayers != 0)
            {
                Collider2D hit = Physics2D.OverlapCircle(worldPos, tileSize * 0.2f, occupiedLayers);
                if (hit != null) return true;
            }

            // 적/아이템 컴포넌트 직접 검사
            Collider2D[] hits = Physics2D.OverlapCircleAll(worldPos, tileSize * 0.2f);
            foreach (var col in hits)
            {
                if (col.TryGetComponent<EnemyBase>(out var enemy) && !enemy.IsDead) return true;
                if (col.TryGetComponent<FieldItem>(out _)) return true;
            }

            return false;
        }

        private void SpawnItemInstance(ItemSpawnRule rule, Vector2Int gridPos, Vector3 worldPos)
        {
            if (rule == null || rule.ItemPrefab == null) return;

            GameObject itemObj = null;

            // 1. 오브젝트 풀을 이용한 스폰
            if (ObjectPoolManager.Instance != null)
            {
                // 원본 프리팹(rule.ItemPrefab)을 인자로 전달하여 복사본 인스턴스를 스폰
                itemObj = ObjectPoolManager.Instance.SpawnFromPool(rule.ItemPrefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                // 백업: 풀 매니저가 없을 경우 Instantiate로 안전한 복사본 생성
                itemObj = Instantiate(rule.ItemPrefab, worldPos, Quaternion.identity, transform);
            }

            // 2. 스폰된 인스턴스 초기화
            if (itemObj != null)
            {
                if (itemObj.TryGetComponent<FieldItem>(out var fieldItem))
                {
                    fieldItem.InitItem(rule.ItemType, 1, gridPos);
                    Debug.Log($"[ItemSpawner] 💎 아이템 스폰 성공! 종류: {rule.ItemType}, 위치: {gridPos}");
                }
            }
        }
        private ItemSpawnRule SelectItemRuleByDepth(int currentDepth, int maxDepth)
        {
            if (itemRules == null || itemRules.Length == 0) return null;

            float totalWeight = 0f;
            float[] weights = new float[itemRules.Length];

            for (int i = 0; i < itemRules.Length; i++)
            {
                weights[i] = itemRules[i].GetWeight(currentDepth, maxDepth);
                totalWeight += weights[i];
            }

            if (totalWeight <= 0f) return null;

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            for (int i = 0; i < itemRules.Length; i++)
            {
                if (weights[i] <= 0f) continue;
                cumulative += weights[i];
                if (roll <= cumulative) return itemRules[i];
            }

            return null;
        }
    }
}