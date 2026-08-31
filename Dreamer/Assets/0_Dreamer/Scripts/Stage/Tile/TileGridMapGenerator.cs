using Dreamer.Core;
using Dreamer.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamer.Tile
{

    /// <summary>
    /// 플레이어의 심도(Y축 하강)에 따라 무한 그리드 지층 및 외벽 타일을 동적 스폰/회수하며,
    /// 굴착 궤적 상태를 메모리에 가볍게 영구 보존하는 매니저
    /// </summary>
    public class TileGridMapGenerator : MonoBehaviour
    {
        [Header("심도 목표 설정")]
        [SerializeField] private int maxTargetDepth = 1655;     // 최대 목표 심도 (정규화 커브 1.0 지점)

        [Header("그리드 맵 설정")]
        [SerializeField] private int mapWidth = 7;             // 지층 폭 (가로 타일 개수)
        [SerializeField] private int initialGenerateDepth = 15; // 최초 생성할 깊이(행)
        [SerializeField] private int generateThreshold = 12;    // 감지 대상 중심으로 상하 스폰할 시야 반지름 범위
        [SerializeField] private int despawnThreshold = 20;     // 감지 대상 중심으로 시야 밖 타일 디스폰 한계치
        [SerializeField] private float tileSize = 1f;           // 타일 간격 유닛

        [Header("지층 규칙 테이블 (확장형 배열)")]
        [SerializeField] private TileData defaultFallbackTile;  // 예외 상황 시 스폰할 기본 타일
        [SerializeField] private TileSpawnRule[] tileRules;     // 깊이별 스폰 가중치 규칙 배열

        [Header("프리팹 참조")]
        [SerializeField] private TileInstance tilePrefab;         // TileInstance가 붙어있는 기본 타일 프리팹
        [SerializeField] private TileInstance wallPrefab;         // 파괴 불가능한 양옆 벽 프리팹

        [Header("추적 대상 (플레이어 또는 스크롤 카메라)")]
        [SerializeField] private Transform playerTransform;

        private int lowestGeneratedY = 0; // 현재 스폰 완료된 최하단 Y 그리드 좌표

        // 1. 화면에 떠있는 실시간 게임오브젝트 관리 (상하 화면 시야 30~50개 유지)
        private readonly Dictionary<Vector2Int, TileInstance> activeTiles = new Dictionary<Vector2Int, TileInstance>();
        private readonly Dictionary<Vector2Int, TileInstance> activeWalls = new Dictionary<Vector2Int, TileInstance>();
        // 2. 전체 지층 파괴/잔여 영구 데이터 저장소 (사망 시 리플레이 스크롤 복원용 경량 데이터)
        private readonly Dictionary<Vector2Int, TileGridData> mapDataStore = new Dictionary<Vector2Int, TileGridData>();

        // 외부(EnemySpawner 등)에서 신규 행 생성 완료 시 구독 가능한 이벤트
        public event Action<int, List<Vector2Int>> OnRowGenerated;

        public int MaxTargetDepth => maxTargetDepth;
        public int MapWidth => mapWidth;
        public float TileSize => tileSize;

        private void OnEnable()
        {
            TileInstance.OnTileDestroyed += SetTileDestroyed;
        }

        private void OnDisable()
        {
            TileInstance.OnTileDestroyed -= SetTileDestroyed;
        }


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

            int targetCurrentY = Mathf.RoundToInt(playerTransform.position.y);

            // 상하 양방향 실시간 시야 유지 (하강 및 리플레이 스크롤 모두 지원)
            MaintainActiveWindow(targetCurrentY);
        }

        /// <summary>
        /// [핵심] 추적 대상(targetY)을 기준으로 상하 양방향 시야 범위를 동적 복원 및 회수
        /// </summary>
        private void MaintainActiveWindow(int targetY)
        {
            int upperY = Mathf.Min(0, targetY + generateThreshold);
            int lowerY = targetY - generateThreshold;

            // 1. 하강 중 미개척 영역이 있으면 신규 생성
            if (lowerY < lowestGeneratedY)
            {
                GenerateRows(lowestGeneratedY - 1, lowerY);
            }

            // 2. 현재 시야 범위(upperY ~ lowerY)에 있는 타일 중 비활성화된 타일 복원
            int startX = -mapWidth / 2;
            int endX = mapWidth / 2;

            for (int y = upperY; y >= lowerY; y--)
            {
                // 좌우 외벽 복원
                SpawnWallTile(new Vector2Int(startX - 1, y));
                SpawnWallTile(new Vector2Int(endX + 1, y));

                // 내부 지층 복원 (이미 뚫린 칸은 IsDestroyed 체크로 자동 건너뜀)
                for (int x = startX; x <= endX; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (!activeTiles.ContainsKey(pos))
                    {
                        SpawnGroundTile(pos, Mathf.Abs(y));
                    }
                }
            }

            // 3. 시야 범위를 멀리 벗어난 위/아래 타일 모두 풀로 회수
            CleanupOldRows(targetY);
        }

        /// <summary>
        /// [리플레이/사망 연출용] 추적 대상을 플레이어에서 카메라(또는 컷씬 타겟)로 변경
        /// </summary>
        public void SetTrackingTarget(Transform newTarget)
        {
            playerTransform = newTarget;
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
                    if (activeTiles.TryGetValue(pos, out TileInstance tileObj))
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
        /// startY부터 endY까지의 지층 행과 양옆 외벽 신규 데이터 생성
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
        /// 추적 대상(targetY)으로부터 상하 despawnThreshold 이상 멀어진 타일 및 벽을 풀로 회수
        /// </summary>
        private void CleanupOldRows(int targetY)
        {
            int upperCleanupY = targetY + despawnThreshold;
            int lowerCleanupY = targetY - despawnThreshold;

            // 1. 지층 타일 회수
            List<Vector2Int> tilesToRemove = new List<Vector2Int>();

            foreach (var kvp in activeTiles)
            {
                if (kvp.Key.y > upperCleanupY || kvp.Key.y < lowerCleanupY)
                {
                    tilesToRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < tilesToRemove.Count; i++)
            {
                Vector2Int pos = tilesToRemove[i];
                TileInstance tileObj = activeTiles[pos];

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

            // 2. 외벽 타일 회수
            List<Vector2Int> wallsToRemove = new List<Vector2Int>();

            foreach (var kvp in activeWalls)
            {
                if (kvp.Key.y > upperCleanupY || kvp.Key.y < lowerCleanupY)
                {
                    wallsToRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < wallsToRemove.Count; i++)
            {
                Vector2Int pos = wallsToRemove[i];
                TileInstance wallObj = activeWalls[pos];

                if (wallObj != null)
                {
                    if (ObjectPoolManager.Instance != null)
                    {
                        ObjectPoolManager.Instance.ReturnToPool(wallPrefab, wallObj);
                    }
                    else
                    {
                        Destroy(wallObj);
                    }
                }

                activeWalls.Remove(pos);
            }
        }

        private void SpawnGroundTile(Vector2Int gridPos, int currentDepth)
        {
            if (tilePrefab == null) return;

            // 데이터가 없으면 신규 생성 후 저장
            if (!mapDataStore.ContainsKey(gridPos))
            {
                TileData selectedTileData = SelectTileDataByDepth(currentDepth);
                mapDataStore[gridPos] = new TileGridData(selectedTileData);
            }

            TileGridData gridData = mapDataStore[gridPos];

            // 이미 부수어진 칸(뚫린 길)이면 절대 스폰하지 않음!
            if (gridData.IsDestroyed) return;

            Vector3 worldPos = new Vector3(gridPos.x * tileSize, gridPos.y * tileSize, 0f);
            TileInstance tileObj = null;

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
                tileInstance.InitTile(gridData.TileData, gridPos);
            }

            activeTiles[gridPos] = tileObj;
        }

        private void SpawnWallTile(Vector2Int gridPos)
        {
            if (wallPrefab == null) return;
            if (activeWalls.ContainsKey(gridPos)) return;

            Vector3 worldPos = new Vector3(gridPos.x * tileSize, gridPos.y * tileSize, 0f);
            TileInstance wallObj = null;

            if (ObjectPoolManager.Instance != null)
            {
                wallObj = ObjectPoolManager.Instance.SpawnFromPool(wallPrefab, worldPos, Quaternion.identity, transform);
            }
            else
            {
                wallObj = Instantiate(wallPrefab, worldPos, Quaternion.identity, transform);
            }

            activeWalls[gridPos] = wallObj;
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

