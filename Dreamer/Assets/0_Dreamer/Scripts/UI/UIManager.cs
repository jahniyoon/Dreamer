using Dreamer.Core;
using Dreamer.Player;
using UnityEngine;

namespace Dreamer.UI
{


    /// <summary>
    /// 전역 UI 상태(인게임, 일시정지, 게임오버) 전환 및 세부 HUD 연동을 관장하는 중앙 매니저
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("하위 HUD / Panel UI 참조")]
        [field: SerializeField] public UITitle TitleUI {  get; set; }
        [field: SerializeField] public UIObject UpgradeUI {  get; set; }
        [field: SerializeField] public UIInGameHUD InGameHUDUI { get; set; }
        [field: SerializeField] public UIObject GameOverUI { get; set; }

        [Header("플레이어 참조")]
        [SerializeField] private PlayerController player;


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

        }



        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }



        /// <summary>
        /// 이벤트 기반 깔끔한 연동 (Direct Update 대신 Event 활용)
        /// </summary>
        protected virtual void SubscribeEvents()
        {
            if (player != null)
            {
                player.Stats.OnHpChanged += HandleHpChanged;
            }

        }

        protected virtual void UnsubscribeEvents()
        {
            if (player != null)
            {
                player.Stats.OnHpChanged -= HandleHpChanged;
            }

        }

        #region Event Handlers

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            if (InGameHUDUI != null)
            {
                InGameHUDUI.UpdateHealthBar(currentHp, maxHp);
            }
        }



        #endregion

    }
}