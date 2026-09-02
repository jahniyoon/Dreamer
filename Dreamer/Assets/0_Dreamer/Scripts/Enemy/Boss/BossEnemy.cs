using DG.Tweening;
using Dreamer.Core;
using Dreamer.Data;
using Dreamer.Enemy;
using NUnit.Framework;
using System.Collections;
using UnityEngine;

namespace Dreamer.Enemy
{

    /// <summary>
    /// 가로 1행 전체를 거대한 체력 벽 형태로 가로막고, 
    /// 낙석 공격 및 플레이어 내리누르기 패턴을 사용하는 심도 보스
    /// </summary>
    public class BossEnemy : EnemyBase
    {
        [Header("보스 스펙 및 아레나 설정")]
        [SerializeField] protected int bossWidth = 9; // mapWidth와 동일하게 설정 (한 행 전체)
        [SerializeField] protected int bossHeight = 9; // mapHeight와 동일하게 설정 (한 행 전체)
        [SerializeField] protected float attackInterval = 3f; // 공격 패턴 주기
        [SerializeField] protected float triggerDistanceY = 3f; // 3칸 거리 남았을 때 감지

        protected bool isBossActivated = false; // 보스전 시작 여부


        protected float attackTimer = 0f;
        protected bool isAttacking = false;


        public override void InitEnemy(EnemyData data, Vector2Int gridPos, int originID = -1)
        {
            base.InitEnemy(data, gridPos, originID);

            ResetScale();
            attackTimer = 0f;
            isAttacking = false;
            isBossActivated = false;


            spriteRenderer.material.SetInt("_Darker", 1);


            ClearSurroundingItems(gridPos);
            Debug.Log($"[GiantBlockBoss] 👑 거대 보스 생성됨! (대기 상태) 위치: {gridPos}");
        }
        protected void ResetScale()
        {
            transform.localScale = new Vector3(bossWidth, bossHeight, 1f);

        }
        protected override void Update()
        {
            if (IsDead || !GameFlowManager.Instance.IsGameRunning) return;

            // 1. 보스가 아직 활성화되지 않았다면 거리 감지
            if (!isBossActivated)
            {
                // 보스 높이(bossHeight) 기준 최상단 Y 좌표 계산
                float bossTopY = transform.position.y + (bossHeight * 0.5f);
                float distanceToPlayerY = TurnManager.CurrentPlayerPosition.y - bossTopY;

                // 플레이어가 보스 위 3칸 이하 범위에 들어오면 보스전 트리거!
                if (distanceToPlayerY <= triggerDistanceY && distanceToPlayerY > -2f)
                {
                    ActivateBoss();
                }

                return;
            }

            // 2. 활성화된 후 기존 공격 타이머 진행
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackInterval && !isAttacking)
            {
                attackTimer = 0f;
                StartPattern();
            }
        }

        // 보스의 공격 패턴
        protected virtual void StartPattern()
        {

        }

        protected override void TurnUpdate()
        {
            base.TurnUpdate();
            if (player == null || isBossActivated) return;

            // 1. 보스 최상단 Y 좌표 및 플레이어와의 Y축 거리 계산
            float bossTopY = transform.position.y + (bossHeight * 0.5f);
            float distanceToPlayerY = Mathf.Abs(TurnManager.CurrentPlayerPosition.y - bossTopY);
            // 2. 거리에 따른 Darker 값 계산 (0칸/3칸 -> 0.0 밝음 / 10칸 -> 1.0 어두움)
            // 0(또는 3)이 들어가면 0 반환, 10이 들어가면 1 반환
            float darkerValue = Mathf.InverseLerp(0, 4, distanceToPlayerY);

            // 3. 쉐이더 파라미터 실시간 업데이트 (DOTween 적용된 SetDarker)
            SetDarker(darkerValue, 0.15f);
        }
        /// <summary>
        /// 3칸 이내로 좁혀졌을 때 보스 출현 알림 및 기상 연출
        /// </summary>
        private void ActivateBoss()
        {
            isBossActivated = true;
            attackTimer = 0f;
            SetDarker(0);

            // 보스 등장 및 경고 알림 연출 (DOShake/DOPunch)
            transform.DOShakePosition(0.8f, 0.3f, 25, 90f);

            // UI 경고 문구 띄우기 예시 (GameFlowManager 등 연동)
            Debug.Log($"[GiantBlockBoss] ⚠️ 보스 경고! 플레이어 접근 감지 공격 시작!");
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
       

        public override void TakeDamage(int damage)
        {
            base.TakeDamage(damage);

            // 피격 시 붉은색 플래시나 펀치 스케일 연출
            transform.DOPunchScale(new Vector3(0f, 0.15f, 0f), 0.15f);

            // 보스 HP UI 업데이트 통보 가능
        }

        protected override void Die()
        {
            Debug.Log($"[GiantBlockBoss] 🏆 보스 처치 완료!");
            isDead = true;
            CurrentState = EnemyState.Dead;

            if (enemyCollider != null) enemyCollider.enabled = false;

            if (enemyData != null && enemyData.DeathVfxPrefab != null && JuiceManager.Instance != null)
            {
                JuiceManager.Instance.SpawnVfx(enemyData.DeathVfxPrefab, transform.position, 1.5f);
            }

            // 1. 타격감 유지를 위한 순간 히트스탑
            JuiceManager.Instance?.DoHitStop(1f, 0.02f);
            JuiceManager.Instance?.ShakeCamera(1);

            // 기존 진행 중인 트윈 정지
            transform.DOKill();

            // 2. 짜부(Squash) 연출 시퀀스
            Sequence dieSequence = DOTween.Sequence();

            // [Step 1] 순간적으로 Y축은 0.2로 찌그러지고 X축은 1.3배로 양옆으로 늘어남 (0.15초)
            dieSequence.Append(transform.DOScale(new Vector3(bossWidth * 1.3f, bossHeight * 0.2f, 1f), 0.15f).SetEase(Ease.OutQuad));

            // [Step 2] 찌그러진 상태에서 살짝 반동하며 완전히 꺼지듯 작아지면서 사라짐 (0.25초)
            dieSequence.Append(transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack));

            // [Step 3] 연출 완료 후 풀 반납 처리
            dieSequence.OnComplete(() =>
            {
                if (ObjectPoolManager.Instance != null)
                {
                    ObjectPoolManager.Instance.ReturnToPool(originInstanceID, gameObject);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            });
        }

        protected override void ExecuteTurnBehavior()
        {
            // Nothing
        }
    }
}