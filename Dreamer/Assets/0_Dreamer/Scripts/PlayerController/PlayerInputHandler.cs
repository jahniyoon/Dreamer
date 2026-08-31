using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dreamer.Player
{
    /// <summary>
    /// 키보드/마우스 입력 수집, 5방향 가공 및 전투 입력 이벤트를 전담하는 컴포넌트
    /// </summary>
    public class PlayerInputHandler : MonoBehaviour
    {
        [Header("Input System Actions")]
        [SerializeField] private InputActionProperty moveAction;
        [SerializeField] private InputActionProperty attackAction;

        public Vector2 RawInputDirection { get; private set; }
        public Vector2 Processed5WayDirection { get; private set; } = Vector2.down;

        public event Action<Vector2> OnAttackInput;

        private void OnEnable()
        {
            if (moveAction.action != null)
            {
                moveAction.action.Enable();
            }

            if (attackAction.action != null)
            {
                attackAction.action.Enable();
                attackAction.action.performed += HandleAttackPerformed;
            }
        }

        private void OnDisable()
        {
            if (moveAction.action != null)
            {
                moveAction.action.Disable();
            }

            if (attackAction.action != null)
            {
                attackAction.action.performed -= HandleAttackPerformed;
                attackAction.action.Disable();
            }
        }

        private void Update()
        {
            GatherMovementInput();
        }

        private void GatherMovementInput()
        {
            if (moveAction.action != null)
            {
                RawInputDirection = moveAction.action.ReadValue<Vector2>();
            }

            if (RawInputDirection.magnitude > 0.1f)
            {
                float horizontal = RawInputDirection.x;
                float vertical = RawInputDirection.y;

                // 5방향 가공 (하단 대각선 2종, 하단, 좌, 우)
                if (vertical < -0.3f && horizontal < -0.3f)
                    Processed5WayDirection = new Vector2(-1f, -1f).normalized;
                else if (vertical < -0.3f && horizontal > 0.3f)
                    Processed5WayDirection = new Vector2(1f, -1f).normalized;
                else if (vertical < -0.3f)
                    Processed5WayDirection = Vector2.down;
                else if (horizontal < -0.3f)
                    Processed5WayDirection = Vector2.left;
                else if (horizontal > 0.3f)
                    Processed5WayDirection = Vector2.right;
            }
        }

        private void HandleAttackPerformed(InputAction.CallbackContext context)
        {
            OnAttackInput?.Invoke(Processed5WayDirection);
        }
    }
}