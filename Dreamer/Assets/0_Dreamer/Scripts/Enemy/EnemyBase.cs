using DG.Tweening;
using Dreamer.Core;
using Dreamer.Data;
using Dreamer.Player;
using Dreamer.Tile;
using Dreamer.UI;
using System.Collections;
using UnityEngine;


namespace Dreamer.Enemy
{
    /// <summary>
    /// 모든 적 AI의 기반 추상 클래스 (FSM + 턴 수신 + IDamageable)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] protected SpriteRenderer spriteRenderer;

        [Header("▶ 기본 적 데이터")]

        [SerializeField] protected EnemyData enemyData;
        [SerializeField] protected float gridSize = 1f;
        [SerializeField] protected float activationDistance = 7f; // 플레이어와의 시야 감지 거리
        [SerializeField] protected bool isFlying = false; // 공중 비행 적 여부 (true면 공중에 떠있어도 낙하하지 않음)
        [SerializeField] protected LayerMask obstacleLayer;
        [SerializeField] protected LayerMask destructibleTileLayer;
        [Header("▶ 피격 연출 설정")]
        [SerializeField] protected Color hitFlashColor = new Color(2.5f, 0.3f, 0.3f, 1f); // 피격 순간 강렬하게 튀는 반짝임 색상



        protected PlayerController player;
        protected Collider2D enemyCollider;


        protected int currentHp;
        protected bool isDead;
        protected Vector2Int gridPos;

        public EnemyState CurrentState { get; protected set; } = EnemyState.Idle;
        public EnemyData Data => enemyData;
        public int CurrentHp => currentHp;
        public bool IsDead => isDead;
        public virtual int Hardness => 1; // 기본 단단함 (자폭 적 등에서 오버라이드)
        public virtual bool IsFlying => isFlying; // 비행 여부
        public Vector2Int GridPos => gridPos;

        protected virtual void Awake()
        {
            enemyCollider = GetComponent<Collider2D>();

        }

        protected virtual void OnEnable()
        {
            TurnManager.OnPlayerTurnExecuted += HandlePlayerTurn;
        }

        protected virtual void OnDisable()
        {
            TurnManager.OnPlayerTurnExecuted -= HandlePlayerTurn;
        }

        public void InitEnemy(EnemyData data, Vector2Int initialGridPos)
        {
            enemyData = data;
            gridPos = initialGridPos;
            transform.position = new Vector3(gridPos.x * gridSize, gridPos.y * gridSize, 0f);

            if (enemyData != null)
            {
                currentHp = enemyData.MaxHp;
                if (spriteRenderer != null && enemyData.EnemySprite != null)
                {
                    spriteRenderer.sprite = enemyData.EnemySprite;
                }
            }
            else
            {
                currentHp = 3;
            }

            isDead = false;
            CurrentState = EnemyState.Idle;
            if (enemyCollider != null) enemyCollider.enabled = true;

            FindPlayer();
        }

        protected void FindPlayer()
        {
            if (player == null)
            {
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) player = playerObj.GetComponent<PlayerController>();
            }
        }

        protected virtual void Update()
        {
            CheckStateTransition();
        }

        /// <summary>
        /// 시화(화면 내) 감지 FSM 상태 전환 체크
        /// </summary>
        protected virtual void CheckStateTransition()
        {
            if (isDead) return;

            if (player == null)
            {
                FindPlayer();
                if (player == null) return;
            }

            float distanceToPlayer = Vector2.Distance(transform.position, player.Movement.CurrentGridPos);

            if (CurrentState == EnemyState.Idle && distanceToPlayer <= activationDistance)
            {
                CurrentState = EnemyState.Active;
                OnActivated();
            }
            else if (CurrentState == EnemyState.Active && distanceToPlayer > activationDistance + 4f)
            {
                CurrentState = EnemyState.Idle;
            }
        }

        protected virtual void OnActivated()
        {
            // 활성화 순간 visual 쥬시 연출 등
        }

        /// <summary>
        /// 플레이어가 턴을 수행했을 때 활성화 상태인 적만 행동 수행
        /// </summary>
        private void HandlePlayerTurn()
        {
            if (CurrentState != EnemyState.Active || isDead) return;

            // 1. 공중에 떠있으면 아래 지층 바닥까지 낙하 (비행 적 제외)
            CheckAndApplyGravity();

            // 2. 개별 AI 턴 행동 수행
            ExecuteTurnBehavior();
        }

        /// <summary>
        /// 비행 적(IsFlying == true)이 아닌 경우, 발아래가 비어있으면 바닥까지 가속 낙하
        /// </summary>
        public virtual void CheckAndApplyGravity()
        {
            if (IsFlying || isDead) return;

            int fallDistance = 0;

            while (true)
            {
                Vector2Int checkGridPos = gridPos + (Vector2Int.down * (fallDistance + 1));
                Vector2 checkWorldPos = new Vector2(checkGridPos.x * gridSize, checkGridPos.y * gridSize);

                Collider2D hit = Physics2D.OverlapCircle(checkWorldPos, gridSize * 0.35f, obstacleLayer | destructibleTileLayer);

                if (hit != null)
                {
                    // 체력이 있는 지층 타일이 있으면 낙하 중단
                    if (hit.TryGetComponent<TileInstance>(out var tile) && tile.CurrentHp > 0)
                    {
                        break;
                    }
                    // 파괴 불가능 외벽이나 기타 장애물이 있으면 낙하 중단
                    if (!hit.isTrigger && !hit.CompareTag("Enemy") && !hit.CompareTag("Player"))
                    {
                        break;
                    }
                }

                fallDistance++;

                // 안전장치: 최대 30칸 이상 연쇄 낙하 검사 제한
                if (fallDistance >= 30) break;
            }

            // 낙하할 빈 공간이 1칸 이상 존재하면 아래쪽 바닥으로 이동
            if (fallDistance > 0)
            {
                MoveToGrid(gridPos + (Vector2Int.down * fallDistance));
            }
        }

        /// <summary>
        /// 파생 적 클래스에서 구현할 1턴 개별 행동
        /// </summary>
        protected abstract void ExecuteTurnBehavior();

        /// <summary>
        /// 해당 그리드 칸에 플레이어가 있는지 검사
        /// </summary>
        protected bool IsPlayerAtGrid(Vector2Int targetPos)
        {
            if (player == null) return false;

            int playerGridX = Mathf.RoundToInt(player.Movement.CurrentGridPos.x / gridSize);
            int playerGridY = Mathf.RoundToInt(player.Movement.CurrentGridPos.y / gridSize);

            return targetPos.x == playerGridX && targetPos.y == playerGridY;
        }

        /// <summary>
        /// 플레이어에게 공격 실행 (내구도 마모 및 피격 연출)
        /// </summary>
        protected void AttackPlayer()
        {
            if (player == null) return;

            // 제자리 타격 펀치 연출
            transform.DOKill();
            transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.12f);

            if (player.TryGetComponent<Dreamer.Player.PlayerStatsHandler>(out var stats))
            {
                int attackDmg = enemyData != null ? enemyData.AttackPower : 1;
                stats.TakeDamage(attackDmg);
            }

            if (JuiceManager.Instance != null)
            {
                JuiceManager.Instance.ShakeCamera(0.2f);
            }

            Debug.Log($"[Enemy] ⚔️ 적({gameObject.name})이 플레이어를 공격했습니다!");

            
        }

        /// <summary>
        /// 그리드 단위 이동 실행 (플레이어나 다른 적이 있으면 이동 중단)
        /// </summary>
        protected bool MoveToGrid(Vector2Int targetPos)
        {
            // 목표 칸에 플레이어가 있는 경우 진입하지 않고 공격!
            if (IsPlayerAtGrid(targetPos))
            {
                AttackPlayer();
                return false;
            }

            gridPos = targetPos;
            Vector3 worldTargetPos = new Vector3(gridPos.x * gridSize, gridPos.y * gridSize, 0f);

            transform.DOKill();
            transform.DOMove(worldTargetPos, 0.1f).SetEase(Ease.OutQuad);

            if (spriteRenderer != null && player != null)
            {
                spriteRenderer.flipX = player.Movement.CurrentGridPos.x < transform.position.x;
            }

            return true;
        }

        public virtual void TakeDamage(int damage)
        {
            if (isDead) return;

            currentHp -= damage;

            // 히트 피드백 (스케일 펀치)
            transform.DOKill();
            transform.DOPunchScale(new Vector3(-0.15f, 0.15f, 0f), 0.1f);

            // 피격 백색 플래시(White Flash) 연출
            TriggerHitFlash();


            if (enemyData != null && enemyData.HitSound != null && JuiceManager.Instance != null)
            {
                JuiceManager.Instance.PlaySfxWithPitch(enemyData.HitSound, 1f, 0.1f);
            }

            if (currentHp <= 0)
            {
                Die();
            }

            if (DamageTextManager.Instance != null)
            {
                DamageTextManager.Instance.SpawnDamageText(transform.position, damage, isPlayerDamage: false);
            }
        }



        protected void TriggerHitFlash()
        {
            DOTween.Kill(this);
            spriteRenderer.material.SetInt("_Flash", 1);

            // 0.08초 후 플래시 끄기 (_FlashAmount = 0) 
            DOVirtual.DelayedCall(0.08f, () =>
            {
                spriteRenderer.material.SetInt("_Flash", 0);

            }).SetTarget(this);
        }
        public void Kill()
        {
            currentHp = 0;
            Die();
        }

        protected virtual void Die()
        {
            isDead = true;
            CurrentState = EnemyState.Dead;

            if (enemyCollider != null) enemyCollider.enabled = false;

            if (enemyData != null && enemyData.DeathVfxPrefab != null && JuiceManager.Instance != null)
            {
                JuiceManager.Instance.SpawnVfx(enemyData.DeathVfxPrefab, transform.position, 1.5f);
            }

            if (ObjectPoolManager.Instance != null)
            {
                ObjectPoolManager.Instance.ReturnToPool(gameObject, gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 적 FSM 상태 정의
    /// </summary>
    public enum EnemyState
    {
        Idle,   // 화면 밖 대기 상태 
        Active, // 화면 진입 후 턴 기반 동작 상태
        Dead    // 사망
    }

}
