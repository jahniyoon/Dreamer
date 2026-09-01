using DG.Tweening;
using UnityEngine;
namespace Dreamer.UI
{

    public class UIObject : MonoBehaviour
    {
        protected float fadeDuration = 0.25f;
        protected Ease showEase = Ease.OutQuad;
        protected Ease hideEase = Ease.InQuad;

        protected CanvasGroup canvasGroup;
        private Tween fadeTween;

        public bool IsVisible => canvasGroup != null && canvasGroup.alpha > 0f;

        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// UI를 서서히 밝혀주며 상호작용을 활성화합니다.
        /// </summary>
        public virtual void Show()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            // 이전 실행 중인 트윈 제거
            fadeTween?.Kill();

            // 즉시 클릭 및 상호작용 허용
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // 페이드 인 (SetUpdate(true)로 일시정지 타임스케일 0에서도 연출 보장)
            fadeTween = canvasGroup.DOFade(1f, fadeDuration)
                .SetEase(showEase)
                .SetUpdate(true);
        }

        /// <summary>
        /// 상호작용을 즉시 차단하고 UI를 서서히 어둡게 숨깁니다.
        /// </summary>
        public virtual void Hide()
        {
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

            // 페이드 아웃 중 중복 클릭 방지를 위해 즉시 입력 차단
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            fadeTween?.Kill();

            // 페이드 아웃
            fadeTween = canvasGroup.DOFade(0f, fadeDuration)
                .SetEase(hideEase)
                .SetUpdate(true);
        }

        protected virtual void OnDestroy()
        {
            fadeTween?.Kill();
        }

    }
}