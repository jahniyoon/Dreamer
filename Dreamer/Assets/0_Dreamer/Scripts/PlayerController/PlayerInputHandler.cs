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
        [SerializeField] private InputActionProperty skill_A;
        [SerializeField] private InputActionProperty skill_B;
        [SerializeField] private InputActionProperty skill_C;

        public Vector2 RawInputDirection { get; private set; }
        public Vector2 Processed5WayDirection { get; private set; } = Vector2.down;

        // 각 스킬별 독립 이벤트
        public event Action OnSkillAInput;
        public event Action OnSkillBInput;
        public event Action OnSkillCInput;

        private void OnEnable()
        {
            if (moveAction.action != null) moveAction.action.Enable();

            if (skill_A.action != null)
            {
                skill_A.action.Enable();
                skill_A.action.performed += HandleSkillAPerformed;
            }

            if (skill_B.action != null)
            {
                skill_B.action.Enable();
                skill_B.action.performed += HandleSkillBPerformed;
            }

            if (skill_C.action != null)
            {
                skill_C.action.Enable();
                skill_C.action.performed += HandleSkillCPerformed;
            }
        }

        private void OnDisable()
        {
            if (moveAction.action != null) moveAction.action.Disable();

            if (skill_A.action != null)
            {
                skill_A.action.performed -= HandleSkillAPerformed;
                skill_A.action.Disable();
            }

            if (skill_B.action != null)
            {
                skill_B.action.performed -= HandleSkillBPerformed;
                skill_B.action.Disable();
            }

            if (skill_C.action != null)
            {
                skill_C.action.performed -= HandleSkillCPerformed;
                skill_C.action.Disable();
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

        private void HandleSkillAPerformed(InputAction.CallbackContext context)
        {
            OnSkillAInput?.Invoke();
        }

        private void HandleSkillBPerformed(InputAction.CallbackContext context)
        {
            OnSkillBInput?.Invoke();
        }

        private void HandleSkillCPerformed(InputAction.CallbackContext context)
        {
            OnSkillCInput?.Invoke();
        }
    }
}