using DG.Tweening;
using Dreamer.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamer.UI
{
    public class ItemAcquireEffect : MonoBehaviour
    {
        public static ItemAcquireEffect Instance { get; private set; }

        [Header("UI Component References")]
        [SerializeField] private RectTransform container;   // 연출 대상 부모 Transform
        [SerializeField] private Image itemIconImage;       // 아이템 이미지
        [SerializeField] private TextMeshProUGUI nameText;   // 아이템 이름 (옵션)
        [SerializeField] private CanvasGroup canvasGroup;     // 전체 페이드용 CanvasGroup

        private Vector3 initialPos;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            if (container != null) initialPos = container.anchoredPosition;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 아이템 획득 연출 실행
        /// </summary>
        public void ShowAcquireEffect(Sprite icon, string itemName = "")
        {
            if (icon == null) return;

            AudioManager.Instance.PlaySFX("ChestOpen");
            // 1. 초기화
            gameObject.SetActive(true);
            itemIconImage.sprite = icon;
            if (nameText != null) nameText.text = itemName;

            container.anchoredPosition = initialPos;
            container.localScale = Vector3.zero; // 0 크기에서 시작
            canvasGroup.alpha = 1f;

            // 2. DOTween 애니메이션 연출 (Pop-up -> Floating -> Fade out)
            Sequence seq = DOTween.Sequence();

            // [1단계] 반짝! 뿅 나타나면서 살짝 커졌다가 정사이즈 (Pop & Flash)
            seq.Append(container.DOScale(1.3f, 0.6f).SetEase(Ease.OutBack));
            seq.Append(container.DOScale(1.0f, 0.3f));

            // [2단계] 위로 두둥실 떠오름
            seq.Append(container.DOAnchorPosY(initialPos.y + 80f, 1.5f).SetEase(Ease.OutQuad));

            // [3단계] 위로 떠오르면서 자연스럽게 사라짐 (Fade Out)
            seq.Join(canvasGroup.DOFade(0f, 1f).SetDelay(0.3f));

            // [4단계] 완료 후 비활성화
            seq.OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
        }
    }
}