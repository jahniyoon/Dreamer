using DG.Tweening;
using Dreamer.Core;
using Dreamer.Tile;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Dreamer.Enemy
{

    public class CrystalBoss : BossEnemy
    {
        [Header("크리스탈 기믹 설정")]
        [SerializeField] private GameObject crystalWallPrefab; // 크리스탈 벽 프리팹
        [SerializeField] private GameObject smallGolemPrefab;  // 소형 고렘 프리팹
        [SerializeField] private float cameraShakeIntensity = 0.5f;
        private readonly List<EnemyBase> spawnedList = new List<EnemyBase>();
        protected override void StartPattern()
        {
            StartCoroutine(PatternRoutine());
        }

        Coroutine curPattern;

        /// <summary>
        /// 보스 무작위 패턴 실행 (포위 벽 생성 / 소형 고렘 스폰 / 솟구치기)
        /// </summary>
        private IEnumerator PatternRoutine()
        {
            isAttacking = true;
            ResetScale();

            if (curPattern != null)
            {
                StopCoroutine(curPattern);
                curPattern = null;
            }

            // 1. 보스 차징 연출 (DOShakePosition)
            spriteRenderer.transform.DOShakePosition(0.5f, new Vector3(0.1f, 0f, 0f), 20, 90f);
            JuiceManager.Instance?.ShakeCamera();
            yield return new WaitForSeconds(0.6f);

            // 2. 3가지 패턴 중 무작위 선택
            int patternIndex = Random.Range(0, 3);

            switch (patternIndex)
            {
                case 0:
                    // [패턴 1] 플레이어 주변 빈 공간에 크리스탈 벽을 세워 포위
                    curPattern = StartCoroutine(BuildCrystalWallsRoutine());
                    yield return curPattern;
                    break;

                case 1:
                    // [패턴 2] 빈 공간에 소형 크리스탈 고렘 스폰
                    curPattern = StartCoroutine(SpawnSmallGolemsRoutine());
                    yield return curPattern;
                    break;

                case 2:
                    // [패턴 3] 보스 솟구치기 돌진 공격
                    curPattern = StartCoroutine(SlamPatternRoutine());
                    yield return curPattern;
                    break;
            }

            isAttacking = false;
        }

        /// <summary>
        /// [패턴 1] 플레이어 주변 빈 공간에 크리스탈 벽을 생성하여 가두는 코루틴
        /// </summary>
        private IEnumerator BuildCrystalWallsRoutine()
        {
            int wallCount = Random.Range(2, 4);
            List<Vector3> validSpawnPositions = GetAvailableEmptyPositions();

            if (validSpawnPositions.Count > 0 && crystalWallPrefab != null)
            {
                int actualWallCount = Mathf.Min(wallCount, validSpawnPositions.Count);

                for (int i = 0; i < actualWallCount; i++)
                {
                    int randomIndex = Random.Range(0, validSpawnPositions.Count);
                    Vector3 spawnPos = validSpawnPositions[randomIndex];
                    validSpawnPositions.RemoveAt(randomIndex);

                    GameObject spawnedObj = null;

                    // 풀링 시스템으로 크리스탈 벽 스폰
                    if (ObjectPoolManager.Instance != null)
                    {
                        var instance = ObjectPoolManager.Instance.SpawnFromPool(crystalWallPrefab, spawnPos, Quaternion.identity);
                        if (instance != null) spawnedObj = instance.gameObject;
                    }
                    else
                    {
                        spawnedObj = Instantiate(crystalWallPrefab, spawnPos, Quaternion.identity);
                    }

                    // 바닥 아래에서 스으윽 솟아오르는 연출 
                    if (spawnedObj != null)
                    {
                        AnimateCrystalRiseUp(spawnedObj, spawnPos, 0.35f);
                    }

                    yield return new WaitForSeconds(0.25f);
                }
            }

            ResetScale();
            curPattern = null;
        }

        /// <summary>
        /// [패턴 2] 빈 공간에 소형 고렘을 스폰하는 코루틴
        /// </summary>
        private IEnumerator SpawnSmallGolemsRoutine()
        {
            int golemCount = Random.Range(2, 3);
            List<Vector3> validSpawnPositions = GetAvailableEmptyPositions();

            if (validSpawnPositions.Count > 0 && smallGolemPrefab != null)
            {
                int actualSpawnCount = Mathf.Min(golemCount, validSpawnPositions.Count);

                for (int i = 0; i < actualSpawnCount; i++)
                {
                    int randomIndex = Random.Range(0, validSpawnPositions.Count);
                    Vector3 spawnPos = validSpawnPositions[randomIndex];
                    validSpawnPositions.RemoveAt(randomIndex);

                    GameObject spawnedObj = null;

                    if (ObjectPoolManager.Instance != null)
                    {
                        var instance = ObjectPoolManager.Instance.SpawnFromPool(smallGolemPrefab, spawnPos, Quaternion.identity);
                        if (instance != null) spawnedObj = instance.gameObject;
                    }
                    else
                    {
                        spawnedObj = Instantiate(smallGolemPrefab, spawnPos, Quaternion.identity);
                    }

                    if (spawnedObj != null)
                    {
                        AnimateCrystalRiseUp(spawnedObj, spawnPos, 0.4f);
                    }

                    yield return new WaitForSeconds(0.3f);
                }
            }

            ResetScale();
            curPattern = null;
        }

        /// <summary>
        /// [패턴 3] 보스가 위로 솟구치며 들이받는 공격
        /// </summary>
        private IEnumerator SlamPatternRoutine()
        {


            Vector3 originalPos = transform.position;

            // 1. 차징: 아래로 웅크리기
            spriteRenderer.transform.DOMoveY(originalPos.y - 0.35f, 0.25f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.3f);

            // 부딪히는 순간 카메라 셰이크
            JuiceManager.Instance?.ShakeCamera(2f);
            AudioManager.Instance.PlaySFX("BossAttack");

            // 2. 솟구치기 (플레이어 방향)
            spriteRenderer.transform.DOMoveY(originalPos.y + 0.7f, 0.25f)
                .SetEase(Ease.OutBack);
            ClearAllSpawnedEntities();
            player.Stats.TakeDamage(Data.AttackPower);
            yield return new WaitForSeconds(0.2f);

            // 3. 복귀
            spriteRenderer.transform.DOMoveY(originalPos.y, 0.25f).SetEase(Ease.InOutQuad);
            yield return new WaitForSeconds(0.3f);

            ResetScale();
            curPattern = null;
        }

        /// <summary>
        /// 플레이어 Y층 전체 라인의 기존 타일을 파괴하고 생성할 좌표 목록을 반환 (적/보스 위치 제외)
        /// </summary>
        private List<Vector3> GetAvailableEmptyPositions()
        {
            List<Vector3> emptyPositions = new List<Vector3>();

            var mapGen = TileGridMapGenerator.Instance;
            if (player == null || mapGen == null) return emptyPositions;

            // 1. 플레이어 및 적/보스 위치 정보 수집
            Vector2Int playerGridPos = mapGen.WorldToGridPos(player.transform.position);
            int playerY = playerGridPos.y;

            // 필드에 있는 모든 적(보스 포함)의 현재 그리드 위치 수집
            HashSet<Vector2Int> occupiedByEnemies = GetEnemyOccupiedGridPositions(mapGen);

            int halfWidth = mapGen.MapWidth / 2;

            // 2. 가로 라인 전체 스캔 (-halfWidth ~ halfWidth)
            for (int x = -halfWidth; x <= halfWidth; x++)
            {
                // 플레이어 본인 위치는 절대 스폰 금지
                if (x == playerGridPos.x) continue;

                Vector2Int targetPos = new Vector2Int(x, playerY);

                // 적이나 보스가 서 있는 칸이라면 건너뜀
                if (occupiedByEnemies.Contains(targetPos)) continue;

                // 🌟 기존 타일이 있든 없든 뚫어버리고 해당 위치를 스폰 후보로 지정
                mapGen.ClearTileArea(targetPos, Vector2Int.one);
                emptyPositions.Add(mapGen.GridToWorldPos(targetPos));

                // 🌟 30% 확률로 2단 생성 (위 Y+1 또는 아래 Y-1)
                if (Random.value < 0.3f)
                {
                    int offsetY = Random.value > 0.5f ? 1 : -1;
                    Vector2Int stackedPos = new Vector2Int(x, playerY + offsetY);

                    // 2단 위치도 플레이어/적 위치가 아닐 때만 파괴 후 스폰
                    if (stackedPos != playerGridPos && !occupiedByEnemies.Contains(stackedPos))
                    {
                        mapGen.ClearTileArea(stackedPos, Vector2Int.one);
                        emptyPositions.Add(mapGen.GridToWorldPos(stackedPos));
                    }
                }
            }

            return emptyPositions;
        }

        /// <summary>
        /// 필드 내 보스 및 적들이 차지하고 있는 그리드 좌표들을 반환
        /// </summary>
        private HashSet<Vector2Int> GetEnemyOccupiedGridPositions(TileGridMapGenerator mapGen)
        {
            HashSet<Vector2Int> enemyPositions = new HashSet<Vector2Int>();

            // 1. 보스 본체 차지 영역 (bossWidth, bossHeight 고려)
            Vector2Int bossGridCenter = mapGen.WorldToGridPos(transform.position);
            int halfBossW = bossWidth / 2;
            int halfBossH = bossHeight / 2;

            for (int x = -halfBossW; x <= halfBossW; x++)
            {
                for (int y = -halfBossH; y <= halfBossH; y++)
                {
                    enemyPositions.Add(bossGridCenter + new Vector2Int(x, y));
                }
            }

            // 2. 필드의 모든 적(Enemy 태그) 위치 추가
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.activeSelf && enemy != gameObject)
                {
                    Vector2Int enemyGrid = mapGen.WorldToGridPos(enemy.transform.position);
                    enemyPositions.Add(enemyGrid);
                }
            }

            return enemyPositions;
        }
        /// <summary>
        /// 스폰된 프리팹이 바닥 아래에서 위로 결정처럼 솟아오르는 연출
        /// </summary>
        private void AnimateCrystalRiseUp(GameObject spawnedObj, Vector3 targetWorldPos, float duration = 0.35f)
        {
            if (spawnedObj == null) return;
            if (spawnedObj.TryGetComponent<EnemyBase>(out var enemy))
            {
                spawnedList.Add(enemy);
                enemy.Sleep(false);
                Transform trans = spawnedObj.transform;

                // 1. 기존 진행 중인 트윈 정지
                trans.DOKill();

                // 2. 초기 상태 설정 (목표 위치보다 0.8유닛 아래, Y축 납작하게)
                Vector3 startPos = targetWorldPos + new Vector3(0f, -0.8f, 0f);
                trans.position = startPos;
                trans.localScale = new Vector3(1f, 0f, 1f); // Y축 스케일 0 (바닥에 납작한 상태)

                // 3. SpriteRenderer 알파(투명도) 연출 준비 (있을 경우)
                SpriteRenderer sr = spawnedObj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    sr.DOKill();
                    Color c = sr.color;
                    c.a = 0f;
                    sr.color = c;
                    sr.DOFade(1f, duration * 0.5f); // 솟아오르며 스르륵 나타남
                }

                // 4. 위치 솟구치기 + 스케일 복원 시퀀스
                Sequence riseSeq = DOTween.Sequence();

                // 위치: 아래에서 목표 위치로 Ease.OutBack (살짝 넘어갔다 정착해 결정 솟구침 감 살림)
                riseSeq.Join(trans.DOMoveY(targetWorldPos.y, duration).SetEase(Ease.OutBack, 1.5f));

                // 스케일: Y축이 0에서 1로 커짐
                riseSeq.Join(trans.DOScaleY(1f, duration).SetEase(Ease.OutQuad));

                // 5. 솟아오름 완료 시 카메라 셰이크 타격감 전달
                riseSeq.OnComplete(() =>
                {
                    JuiceManager.Instance?.ShakeCamera(cameraShakeIntensity * 0.7f);
                });
                enemy.transform.position = targetWorldPos;
                enemy.Sleep(true);
            }
        }

        /// <summary>
        /// 리스트에 담긴 소환물들을 일괄 파괴 및 반납하고 리스트를 비움
        /// </summary>
        private void ClearAllSpawnedEntities()
        {
            for (int i = 0; i < spawnedList.Count; i++)
            {
                var obj = spawnedList[i];

                // 이미 플레이어가 파괴했거나 null인 경우 패스
                if (obj == null || !obj.gameObject.activeSelf) continue;

                obj.Kill();
            }

            // 한 번 청소 후 리스트 초기화
            spawnedList.Clear();
        }

        // 보스가 죽었을 때도 소환물이 남아있지 않게 Cleanup
        protected override void Die()
        {
            ClearAllSpawnedEntities();
            base.Die();
        }
    }
}
