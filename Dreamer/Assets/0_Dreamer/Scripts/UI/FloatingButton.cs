using DG.Tweening;
using Dreamer.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dreamer.UI
{
    [RequireComponent(typeof(Button))]
    public class FloatingButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("둥실둥실 연출 설정")]
        [SerializeField] private float floatDistance = 8f;   // 위아래 이동 거리 (픽셀)
        [SerializeField] private float floatDuration = 1.2f; // 위아래 이동 소요 시간
        [SerializeField] private bool useRandomOffset = true; // 버튼마다 속도/박자 살짝 다르게

        [Header("사운드 설정")]
        [SerializeField] private string clickSfxName = "Click"; // AudioManager에 등록된 SFX 이름

        private RectTransform rectTransform;
        private Vector2 originAnchoredPos;
        private Tween floatTween;
        private Vector3 originScale;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            originScale = transform.localScale;

            // 버튼 클릭 이벤트에 사운드 재생 자동 연동
            Button btn = GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(PlayClickSound);
            }
        }

        private void OnEnable()
        {
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
            originAnchoredPos = rectTransform.anchoredPosition;

            StartFloatingAnimation();
        }

        private void OnDisable()
        {
            // 비활성화 시 트윈 정리
            floatTween?.Kill();
            if (rectTransform != null) rectTransform.anchoredPosition = originAnchoredPos;
        }

        /// <summary>
        /// 위아래 둥실둥실 무한 루프 애니메이션 (DOTween)
        /// </summary>
        private void StartFloatingAnimation()
        {
            floatTween?.Kill();

            float delay = useRandomOffset ? Random.Range(0f, 0.5f) : 0f;

            // 위아래 루프 이동 (Yoyo)
            floatTween = rectTransform
                .DOAnchorPosY(originAnchoredPos.y + floatDistance, floatDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(delay);
        }

        private void PlayClickSound()
        {
            // 기존에 만든 AudioManager를 통해 클릭 사운드 재생
            AudioManager.Instance?.PlaySFX(clickSfxName);
        }

        // --- 누를 때 찰진 Scale 스퀴시(눌림) 연출 ---
        public void OnPointerDown(PointerEventData eventData)
        {
            transform.DOScale(originScale * 0.9f, 0.1f).SetUpdate(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            transform.DOScale(originScale, 0.1f).SetUpdate(true);
        }
    }
}