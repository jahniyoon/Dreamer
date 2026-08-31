using DG.Tweening;
using UnityEngine;

namespace Dreamer.Player
{
    /// <summary>
    /// 플레이어의 스프라이트 반전, DOTween 애니메이션 연출을 전담하는 컴포넌트
    /// </summary>
    public class PlayerVisual : MonoBehaviour
    {
        [Header("비주얼 참조")]
        [SerializeField] private Transform characterVisualTransform;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Juice 연출 설정")]
        [SerializeField] private float squashDuration = 0.15f;
        [SerializeField] private Vector3 attackSquashScale = new Vector3(1.3f, 0.7f, 1f);
        [SerializeField] private Vector3 moveSquashScale = new Vector3(0.85f, 1.15f, 1f);

        private Vector3 originalScale;

        private void Awake()
        {
            if (characterVisualTransform == null) characterVisualTransform = transform;
            if (spriteRenderer == null) spriteRenderer = characterVisualTransform.GetComponent<SpriteRenderer>();
            originalScale = characterVisualTransform.localScale;
        }

        public void UpdateFacingDirection(float horizontalInput)
        {
            if (spriteRenderer != null && horizontalInput != 0)
            {
                spriteRenderer.flipX = horizontalInput < 0;
            }
        }

        public void PlayAttackSquash()
        {
            ApplySquash(attackSquashScale);
        }

        public void PlayMoveSquash()
        {
            ApplySquash(moveSquashScale);
        }

        private void ApplySquash(Vector3 targetScale)
        {
            if (characterVisualTransform == null) return;

            characterVisualTransform.DOKill();
            characterVisualTransform.localScale = originalScale;

            characterVisualTransform.DOScale(targetScale, squashDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    characterVisualTransform.DOScale(originalScale, squashDuration * 0.5f)
                        .SetEase(Ease.InQuad);
                });
        }
    }
}