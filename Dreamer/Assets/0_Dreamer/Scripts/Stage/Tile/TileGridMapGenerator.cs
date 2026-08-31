using Dreamer.Core;
using Dreamer.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamer.Tile
{

    /// <summary>
    /// 플레이어의 심도(Y축 하강)에 따라 무한 그리드 지층 및 외벽 타일을 동적 스폰 관리하는 매니저
    /// </summary>
    public class TileGridMapGenerator : MonoBehaviour
    {
        [Header("심도 목표 설정")]
        [SerializeField] private int maxTargetDepth = 1655;     // 최대 목표 심도 (정규화 커브 1.0 지점)

        [Header("그리드 맵 설정")]
        [SerializeField] private int mapWidth = 7;             // 지층 폭 (가로 타일 개수)
        [SerializeField] private int initialGenerateDepth = 15; // 최초 생성할 깊이(행)
        [SerializeField] private int generateThreshold = 5;    // 플레이어 남은 거리 한계치에 다다르면 추가 스폰
        [SerializeField] private int despawnThreshold = 15;     // 플레이어 위쪽으로 이 거리 이상 멀어지면 타일 회수(디스폰)
        [SerializeField] private float tileSize = 1f;           // 타일 간격 유닛

        [Header("지층 규칙 테이블 (확장형 배열)")]
        [SerializeField] private TileData defaultFallbackTile;  // 예외 상황 시 스폰할 기본 타일
        [SerializeField] private TileSpawnRule[] tileRules;     // 깊이별 스폰 가중치 규칙 배열

        [Header("프리팹 참조")]
        [SerializeField] private GameObject tilePrefab;         // TileInstance가 붙어있는 기본 타일 프리팹
        [SerializeField] private GameObject wallPrefab;         // 파괴 불가능한 양옆 벽 프리팹

        [Header("추적 대상")]
        [SerializeField] private Transform playerTransform;

        private int lowestGeneratedY = 0; // 현재 스폰 완료된 최하단 Y 그리드 좌표

        // 1. 화면에 떠있는 실시간 게임오브젝트 관리 (최적화용, 30~50개 유지)
        private readonly Dictionary<Vector2Int, GameObject> activeTiles = new Dictionary<Vector2Int, GameObject>();

        // 2. 전체 지층 파괴/잔여 영구 데이터 저장소 (사망 시 리플레이 스크롤 복원용 경량 데이터)
        private readonly Dictionary<Vector2Int, TileGridData> mapDataStore = new Dictionary<Vector2Int, TileGridData>();

        // 외부(EnemySpawner 등)에서 행 생성 완료 시 구독 가능한 이벤트 (행 Y좌표, 생성된 내벽 타일 좌표 목록)
        public event Action<int, List<Vector2Int>> OnRowGenerated;

        public int MaxTargetDepth => maxTargetDepth;
        public int MapWidth => mapWidth;
        public float TileSize => tileSize;

        private void Start()
        {
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }

            // 초기 맵 동적 생성 (Y = 0 부터 -initialGenerateDepth 까지)
            GenerateRows(0, -initialGenerateDepth);
        }

        private void Update()
        {
            if (playerTransform == null) return;

            int playerCurrentY = Mathf.RoundToInt(playerTransform.position.y);

            // 1. 플레이어가 아래로 내려감에 따라 추가 지층 미리 생성
            if (playerCurrentY - generateThreshold <= lowestGeneratedY)
            {
                int nextTargetY = lowestGeneratedY - 10;
                GenerateRows(lowestGeneratedY - 1, nextTargetY);
            }

            // 2. 화면 위쪽으로 멀어진 지난 지층 타일 자동 회수 (최적화 핵심)
            CleanupOldRows(playerCurrentY);
        }

        /// <summary>
        /// 타일 파괴 시 외부(TileInstance)에서 호출하여 데이터 저장소에 영구 파괴 기록
        /// </summary>
        public void SetTileDestroyed(Vector2Int gridPos)
        {
            if (mapDataStore.TryGetValue(gridPos, out var gridData))
            {
                gridData.IsDestroyed = true;
                gridData.CurrentHp = 0;
                mapDataStore[gridPos] = gridData;
            }
        }

        /// <summary>
        /// 보스 방 생성을 위해 특정 영역의 타일을 강제로 파괴(비우기) 처리
        /// </summary>
        public void ClearTileArea(Vector2Int centerGridPos, Vector2Int size)
        {
            int startX = centerGridPos.x - size.x / 2;
            int endX = centerGridPos.x + size.x / 2;
            int startY = centerGridPos.y + size.y / 2;
            int endY = centerGridPos.y - size.y / 2;

            for (int y = startY; y >= endY; y--)
            {
                for (int x = startX; x <= endX; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);

                    // 1. 영구 데이터 상에서 파괴 처리
                    if (mapDataStore.TryGetValue(pos, out var gridData))
                    {
                        gridData.IsDestroyed = true;
                        gridData.CurrentHp = 0;
                        mapDataStore[pos] = gridData;
                    }

                    // 2. 활성화된 오브젝트가 있다면 풀로 반환
                    if (activeTiles.TryGetValue(pos, out GameObject tileObj))
                    {
                        if (tileObj != null)
                        {
                            if (ObjectPoolManager.Instance != null)
                            {
                                ObjectPoolManager.Instance.ReturnToPool(tilePrefab, tileObj);
                            }
                            else
                            {
                                Destroy(tileObj);
                            }
                        }
                        activeTiles.Remove(pos);
                    }
                }
            }
        }

        /// <summary>
        /// startY부터 endY까지의 지층 행과 양옆 외벽을 스폰
        /// </summary>
        private void GenerateRows(int startY, int endY)
        {
            int startX = -mapWidth / 2;
            int endX = mapWidth / 2;

            for (int y = startY; y >= endY; y--)
            {
                List<Vector2Int> currentRowPositions = new List<Vector2Int>();

                // 1. 좌우 외벽 생성
                SpawnWallTile(new Vector2Int(startX - 1, y));
                SpawnWallTile(new Vector2Int(endX + 1, y));

                // 2. 내부 채굴 가능 지층 생성
                for (int x = startX; x <= endX; x++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    currentRowPositions.Add(gridPos);

                    if (!activeTiles.ContainsKey(gridPos))
                    {
                        SpawnGroundTile(gridPos, Mathf.Abs(y));
                    }
                }

                // 행 생성 이벤트 통보 (적 스포너 등 연동용)
                OnRowGenerated?.Invoke(y, currentRowPositions);
            }

            lowestGeneratedY = endY;
        }

        /// <summary>
        /// 플레이어 위쪽 화면 밖으로 멀어진 타일을 풀로 반환하고 activeTiles에서 제거
        /// </summary>
        private void CleanupOldRows(int playerCurrentY)
        {
            int cleanupTargetY = playerCurrentY + despawnThreshold;

            List<Vector2Int> keysToRemove = new List<Vector2Int>();

            foreach (var kvp in activeTiles)
            {
                if (kvp.Key.y > cleanupTargetY)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < keysToRemove.Count; i++)
            {
                Vector2Int pos = keysToRemove[i];
                GameObject tileObj = activeTiles[pos];

                if (tileObj != null)
                {
                    if (ObjectPoolManager.Instance != null)
                    {
                        ObjectPoolManager.Instance.ReturnToPool(tilePrefab, tileObj);
                    }
                    else
                    {
                        Destroy(tileObj);
                    }
                }

                activeTiles.Remove(pos);
            }
        }

        private void SpawnGroundTile(Vector2Int gridPos, int currentDepth)
        {
            if (tilePrefab == null) return;

            if (!mapDataStore.ContainsKey(gridPos))
            {
                TileData selectedTileData = SelectTileDataByDepth(currentDepth);
                mapDataStore[gridPos] = new TileGridData(selectedTileData);
            }

            TileGridData gridData = mapDataStore[gridPos];

            if (gridData.IsDestroyed) return;

            Vector3 worldPos = new Vector3(gridPos.x * tileSize, gridPos.y * tileSize, 0f);
            GameObject tileObj = null;

            if (ObjectPoolManager.Instance != null)
            {
                tileObj = ObjectPoolManager.Instance.SpawnFromPool(tilePrefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                tileObj = Instantiate(tilePrefab, worldPos, Quaternion.identity, transform);
            }

            if (tileObj.TryGetComponent<TileInstance>(out var tileInstance))
            {
                tileInstance.InitTile(gridData.TileData);
            }

            activeTiles[gridPos] = tileObj;
        }

        private void SpawnWallTile(Vector2Int gridPos)
        {
            if (wallPrefab == null) return;

            Vector3 worldPos = new Vector3(gridPos.x * tileSize, gridPos.y * tileSize, 0f);

            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.SpawnFromPool(wallPrefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                Instantiate(wallPrefab, worldPos, Quaternion.identity, transform);
            }
        }

        private TileData SelectTileDataByDepth(int depth)
        {
            if (tileRules == null || tileRules.Length == 0) return defaultFallbackTile;

            float totalWeight = 0f;
            float[] weights = new float[tileRules.Length];

            for (int i = 0; i < tileRules.Length; i++)
            {
                weights[i] = tileRules[i].GetWeight(depth, maxTargetDepth);
                totalWeight += weights[i];
            }

            if (totalWeight <= 0f) return defaultFallbackTile;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulativeWeight = 0f;

            for (int i = 0; i < tileRules.Length; i++)
            {
                if (weights[i] <= 0f) continue;

                cumulativeWeight += weights[i];
                if (roll <= cumulativeWeight)
                {
                    return tileRules[i].TileData;
                }
            }

            return defaultFallbackTile;
        }
    }
}
