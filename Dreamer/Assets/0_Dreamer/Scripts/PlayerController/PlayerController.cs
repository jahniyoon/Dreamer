using UnityEngine;


namespace Dreamer.Player
{
    /// <summary>
    /// 입력 수집, 이동 및 하위 컴포넌트(Stats, Visual, Combat)를 조율하는 메인 컨트롤러
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    [RequireComponent(typeof(PlayerStatsHandler), typeof(PlayerVisual), typeof(PlayerCombat))]
    public class PlayerController : MonoBehaviour
    {
        // 컴포넌트 참조 (Facade Pattern)
        public PlayerStatsHandler Stats { get; private set; }
        public PlayerVisual Visual { get; private set; }
        public PlayerCombat Combat { get; private set; }

        private Rigidbody2D rb;
        public Vector2 LastInputDirection { get; private set; } = Vector2.down;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            Stats = GetComponent<PlayerStatsHandler>();
            Visual = GetComponent<PlayerVisual>();
            Combat = GetComponent<PlayerCombat>();
        }

        private void Update()
        {
            ProcessInput();
        }

        private void FixedUpdate()
        {
            // 이동 처리 (공격 중에는 이동 감속)
            if (!Combat.IsAttacking && LastInputDirection.x != 0)
            {
                rb.linearVelocity = new Vector2(LastInputDirection.x * Stats.CurrentStats.MoveSpeed, rb.linearVelocity.y);
            }
        }

        private void ProcessInput()
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector2 rawDir = new Vector2(horizontal, vertical);

            if (rawDir.magnitude > 0.1f)
            {
                // 5방향 정제 로직
                if (vertical < -0.3f && horizontal < -0.3f) LastInputDirection = new Vector2(-1f, -1f).normalized;
                else if (vertical < -0.3f && horizontal > 0.3f) LastInputDirection = new Vector2(1f, -1f).normalized;
                else if (vertical < -0.3f) LastInputDirection = Vector2.down;
                else if (horizontal < -0.3f) LastInputDirection = Vector2.left;
                else if (horizontal > 0.3f) LastInputDirection = Vector2.right;

                Visual.UpdateFacingDirection(horizontal);
            }

            // 공격 입력
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                Combat.TryAttack(LastInputDirection);
            }
        }
    }
}