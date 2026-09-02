using DG.Tweening;
using Dreamer.Core;
using Dreamer.Data;
using Dreamer.Enemy;
using System.Collections;
using UnityEngine;

namespace Dreamer.Enemy
{

    /// <summary>
    /// 가로 1행 전체를 거대한 체력 벽 형태로 가로막고, 
    /// 낙석 공격 및 플레이어 내리누르기 패턴을 사용하는 심도 보스
    /// </summary>
    public class GiantBlockBoss : EnemyBase
    {
        [Header("보스 스펙 및 아레나 설정")]
        [SerializeField] private string bossName = "Deep Earth Core";
        [SerializeField] private int bossWidth = 9; // mapWidth와 동일하게 설정 (한 행 전체)
        [SerializeField] private int bossHeight = 9; // mapHeight와 동일하게 설정 (한 행 전체)
        [SerializeField] private float attackInterval = 3f; // 공격 패턴 주기

        [Header("낙석 패턴 프리팹 및 레이어")]
        [SerializeField] private GameObject fallingRockPrefab;
        [SerializeField] private Transform[] rockSpawnPoints;

        private float attackTimer = 0f;
        private bool isAttacking = false;

        public string BossName => bossName;

        public override void InitEnemy(EnemyData data, Vector2Int gridPos)
        {
            base.InitEnemy(data, gridPos);

            // 가로 한 행 전체에 맞춰 보스 Collider 및 Visual Scale 조정
            transform.localScale = new Vector3(bossWidth, bossHeight, 1f);
            attackTimer = 0f;
            isAttacking = false;
            ClearSurroundingItems(gridPos);
            Debug.Log($"[GiantBlockBoss] 👑 거대 보스 등장! 이름: {bossName}, 위치: {gridPos}");
        }

        protected override void Update()
        {

            attackTimer += Time.deltaTime;
            if (IsDead || !GameFlowManager.Instance.IsGameRunning && CurrentState != EnemyState.Active || isDead) return;
            if (attackTimer >= attackInterval && !isAttacking)
            {
                attackTimer = 0f;
                StartCoroutine(PatternRoutine());
            }
        }
        /// <summary>
        /// 보스 아레나 영역에 존재하는 필드 아이템들을 OverlapBox로 감지하여 안전하게 회수
        /// </summary>
        private void ClearSurroundingItems(Vector2Int gridPos)
        {
            float tileSize = 1f; // 기본 타일 사이즈 (필요시 MapGenerator 참조값 활용)
            Vector2 boxSize = new Vector2(bossWidth * tileSize, bossHeight * tileSize);

            // 보스 크기 영역에 걸치는 모든 2D 콜라이더 탐색
            Collider2D[] hits = Physics2D.OverlapBoxAll(transform.position, boxSize, 0f);

            foreach (var hit in hits)
            {
                if (hit == null) continue;

                // 필드 아이템 컴포넌트가 존재하면 풀로 회수
                if (hit.TryGetComponent<Dreamer.Item.FieldItem>(out var fieldItem))
                {
                  fieldItem.Kill();
                }
            }
        }
        /// <summary>
        /// 보스 무작위 패턴 실행 (낙석 경고 후 투하 / 몸집 흔들기)
        /// </summary>
        private IEnumerator PatternRoutine()
        {
            isAttacking = true;

            // 1. 보스 차징 연출 (DOShakeScale)
            transform.DOShakePosition(0.5f, 0.2f, 20, 90f);
            yield return new WaitForSeconds(0.6f);

            // 2. 패턴 무작위 실행
            int patternIndex = Random.Range(0, 2);

            if (patternIndex == 0)
            {
                // [패턴 1] 무작위 2~3개 위치에 낙석 투하
                yield return StartCoroutine(SpawnFallingRocksRoutine());
            }
            else
            {
                // [패턴 2] 보스가 1칸 위로 살짝 들렸다가 아래로 내려찍는 진동 패턴
                yield return StartCoroutine(SlamPatternRoutine());
            }

            isAttacking = false;
        }

        private IEnumerator SpawnFallingRocksRoutine()
        {
            int rockCount = Random.Range(2, 4);
            int halfWidth = bossWidth / 2;

            for (int i = 0; i < rockCount; i++)
            {
                int randomX = Random.Range(-halfWidth, halfWidth + 1);
                Vector3 rockSpawnWorldPos = new Vector3(randomX, transform.position.y + 2f, 0f);

                if (fallingRockPrefab != null)
                {
                    if (ObjectPoolManager.Instance != null)
                    {
                        ObjectPoolManager.Instance.SpawnFromPool(fallingRockPrefab, rockSpawnWorldPos, Quaternion.identity, null, 3f);
                    }
                    else
                    {
                        Instantiate(fallingRockPrefab, rockSpawnWorldPos, Quaternion.identity);
                    }
                }
            }

            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator SlamPatternRoutine()
        {
            Vector3 originalPos = transform.position;

            // 살짝 들렸다가
            transform.DOMoveY(originalPos.y + 0.5f, 0.3f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.35f);

            // 강하게 내리찍기
            transform.DOMoveY(originalPos.y, 0.15f).SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    // 카메라 셰이크 연출 추가 가능
                });

            yield return new WaitForSeconds(0.5f);
        }

        public override void TakeDamage(int damage)
        {
            base.TakeDamage(damage);

            // 피격 시 붉은색 플래시나 펀치 스케일 연출
            transform.DOPunchScale(new Vector3(0f, 0.15f, 0f), 0.15f);

            // 보스 HP UI 업데이트 통보 가능
        }

        protected override void Die()
        {
            base.Die();
            Debug.Log($"[GiantBlockBoss] 🏆 보스 처치 완료! [{bossName}]");


        }

        protected override void ExecuteTurnBehavior()
        {
            throw new System.NotImplementedException();
        }
    }
}