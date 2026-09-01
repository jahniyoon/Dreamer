using DG.Tweening;
using Dreamer.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Dreamer.UI
{
    public class UIInGameHUD : UIObject
    {
        [Header("체력바 UI (이중 잔상 체력바)")]
        [SerializeField] private Transform healthBarTransform;        // 전면 체력바 (빨강 - 즉시 차감)
        [SerializeField] private Image healthBar;        // 전면 체력바 (빨강 - 즉시 차감)
        [SerializeField] private Image delayedHealthBar; // 후면 체력바 (주황 - 서서히 따라오는 잔상)
        [SerializeField] private float damageDelay = 0.35f;    // 잔상이 유지되는 대기 시간
        [SerializeField] private float catchUpDuration = 0.45f; // 잔상이 줄어드는 애니메이션 시간
        [SerializeField] private Ease catchUpEase = Ease.OutQuad;
        [Header("심도 UI")]
        [SerializeField] private TMP_Text depthText;

        private int displayedDepth = -1;
        private Tween delayedHealthTween;

        protected override void SubscribeEvents()
        {
            base.SubscribeEvents();
            TurnManager.OnPlayerTurnExecuted += OnPlayerMoved;
        }
        protected override void UnsubscribeEvents()
        {
            base.UnsubscribeEvents();
            TurnManager.OnPlayerTurnExecuted -= OnPlayerMoved;

        }

        /// <summary>
        /// UIManager에서 호출해주는 이중 잔상 체력바 연출
        /// </summary>
        public void UpdateHealthBar(int currentHp, int maxHp)
        {
            if (healthBar == null) return;

            float targetFill = Mathf.Clamp01((float)currentHp / Mathf.Max(1, maxHp));

            // 1. 피격당했을 때 (체력이 줄어듦)
            if (targetFill < healthBar.fillAmount)
            {
                // A. 전면(빨강) 바는 즉시 목표 수치로 뚝 떨어짐
                healthBar.fillAmount = targetFill;

                // B. 이전 트윈 취소 후, 후면(주황) 바는 delay 후 부드럽게 쫓아감
                delayedHealthTween?.Kill();

                if (delayedHealthBar != null)
                {
                    delayedHealthTween = delayedHealthBar.DOFillAmount(targetFill, catchUpDuration)
                        .SetDelay(damageDelay)
                        .SetEase(catchUpEase);
                }

                // C. 피격 쥬시 (UI 찌그러짐 펀치 연출)
                healthBarTransform.DOKill();
                healthBarTransform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.15f);
            }
            // 2. 회복했을 때 (체력이 늘어남)
            else
            {
                delayedHealthTween?.Kill();

                healthBar.DOKill();
                healthBar.DOFillAmount(targetFill, 0.2f);

                if (delayedHealthBar != null)
                {
                    delayedHealthBar.DOKill();
                    delayedHealthBar.DOFillAmount(targetFill, 0.2f);
                }
            }
        }

        /// <summary>
        /// 플레이어가 이동을 완료했을 때 외부(PlayerMove 등)에서 호출하여 심도 UI 갱신
        /// </summary>
        public void OnPlayerMoved()
        {
            var depth = Mathf.Max(0, Mathf.Abs(TurnManager.CurrentPlayerPosition.y));
            SetDepth(depth);
        }


        /// <summary>
        /// UIManager에서 호출해주는 심도(M) 텍스트 연출
        /// </summary>
        public void SetDepth(int depth)
        {
            if (depth == displayedDepth) return;

            displayedDepth = depth;

            if (depthText != null)
            {
                depthText.text = $"{displayedDepth}<size=60%>m</size>";

                // 텍스트 살짝 툭 튀어나오는 연출
                depthText.transform.DOKill();
                depthText.transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.1f);
            }
        }

     

        private void OnDestroy()
        {
            delayedHealthTween?.Kill();
        }
    }
}