using UnityEngine;


namespace Dreamer.Player
{
    /// <summary>
    /// 입력 수집, 이동 및 하위 컴포넌트(Stats, Visual, Combat)를 조율하는 메인 컨트롤러
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    [RequireComponent(typeof(PlayerInputHandler), typeof(PlayerMove))]
    [RequireComponent(typeof(PlayerCombat), typeof(PlayerVisual))]
    public class PlayerController : MonoBehaviour
    {
        public PlayerInputHandler InputHandler { get; private set; }
        public PlayerMove Movement { get; private set; }
        public PlayerCombat Combat { get; private set; }
        public PlayerVisual Visual { get; private set; }
        public PlayerStatsHandler Stats { get; private set; }

        private void Awake()
        {
            InputHandler = GetComponent<PlayerInputHandler>();
            Movement = GetComponent<PlayerMove>();
            Combat = GetComponent<PlayerCombat>();
            Visual = GetComponent<PlayerVisual>();
            Stats = GetComponent<PlayerStatsHandler>();
        }
        private void OnEnable()
        {
            if (InputHandler != null)
            {
                InputHandler.OnAttackInput += HandleAttack;
            }
        }

        private void OnDisable()
        {
            if (InputHandler != null)
            {
                InputHandler.OnAttackInput -= HandleAttack;
            }
        }

        private void Update()
        {
            ProcessGridMovement();
        }

        private void ProcessGridMovement()
        {
            if (InputHandler == null || Movement == null) return;

            // 이동 중이거나 공격 중일 때는 새로운 이동 입력을 차단
            if (Movement.IsMoving || (Combat != null && Combat.IsAttacking)) return;

            Vector2 rawDir = InputHandler.RawInputDirection;

            // 수평 이동 시도 (우선순위: 수평)
            if (Mathf.Abs(rawDir.x) > 0.5f)
            {
                Vector2 targetDir = rawDir.x > 0 ? Vector2.right : Vector2.left;
                Movement.TryGridMove(targetDir);
            }
            // 수직 이동 시도 (하단 이동)
            else if (rawDir.y < -0.5f)
            {
                Movement.TryGridMove(Vector2.down);
            }
        }

        private void HandleAttack(Vector2 attackDirection)
        {
            // 이동 중일 때는 공격 실행 차단
            if (Movement != null && Movement.IsMoving) return;

            if (Combat != null)
            {
                Combat.TryAttack(attackDirection);              
            }
        }
    }
}