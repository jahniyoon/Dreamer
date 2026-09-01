using Dreamer.Core;
using Dreamer.Player;
using UnityEngine;

namespace Dreamer.UI
{
    public enum UIState
    {
        InGame,
        Pause,
        GameOver,
        Shop
    }

    /// <summary>
    /// 전역 UI 상태(인게임, 일시정지, 게임오버) 전환 및 세부 HUD 연동을 관장하는 중앙 매니저
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("하위 HUD / Panel UI 참조")]
        [SerializeField] private InGameHUDUI inGameHUDUI;
        [SerializeField] private GameObject pausePanel;

        [Header("플레이어 참조")]
        [SerializeField] private PlayerController player;

        public UIState CurrentState { get; private set; } = UIState.InGame;

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

            FindReferences();
        }

        private void Start()
        {
            SetUIState(UIState.InGame);
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void FindReferences()
        {
            if (inGameHUDUI == null) inGameHUDUI = FindFirstObjectByType<InGameHUDUI>();
        }

        /// <summary>
        /// 이벤트 기반 깔끔한 연동 (Direct Update 대신 Event 활용)
        /// </summary>
        private void SubscribeEvents()
        {
            if (player != null)
            {
                player.Stats.OnHpChanged += HandleHpChanged;
                player.Stats.OnPlayerDied += HandlePlayerDied;
            }
            TurnManager.OnPlayerTurnExecuted += OnPlayerMoved;

        }

        private void UnsubscribeEvents()
        {
            if (player != null)
            {
                player.Stats.OnHpChanged -= HandleHpChanged;
                player.Stats.OnPlayerDied -= HandlePlayerDied;
            }
            TurnManager.OnPlayerTurnExecuted -= OnPlayerMoved;

        }

        #region Event Handlers

        private void HandleHpChanged(int currentHp, int maxHp)
        {
            if (inGameHUDUI != null)
            {
                inGameHUDUI.UpdateHealthBar(currentHp, maxHp);
            }
        }

        private void HandlePlayerDied()
        {
            SetUIState(UIState.GameOver);
        }

        /// <summary>
        /// 플레이어가 이동을 완료했을 때 외부(PlayerMove 등)에서 호출하여 심도 UI 갱신
        /// </summary>
        public void OnPlayerMoved()
        {
            if (player == null)
                return;

            int depthM = Mathf.Max(0, Mathf.Abs(player.Movement.CurrentGridPos.y));
            if (inGameHUDUI != null)
            {
                inGameHUDUI.SetDepth(depthM);
            }
        }

        #endregion

        #region UI State Management

        /// <summary>
        /// UI 상태 전환 (인게임 HUD, 일시정지, 게임오버 창 스위칭)
        /// </summary>
        public void SetUIState(UIState newState)
        {
            CurrentState = newState;

            if (pausePanel != null) pausePanel.SetActive(newState == UIState.Pause);

            switch (newState)
            {
                case UIState.InGame:
                    Time.timeScale = 1f;
                    break;

                case UIState.Pause:
                    Time.timeScale = 0f;
                    break;

                case UIState.GameOver:
                    Time.timeScale = 1f; // 필요시 히트스톱 연출 후 딜레이 처리
                    break;
            }
        }

        public void TogglePause()
        {
            if (CurrentState == UIState.InGame)
            {
                SetUIState(UIState.Pause);
            }
            else if (CurrentState == UIState.Pause)
            {
                SetUIState(UIState.InGame);
            }
        }

        #endregion
    }
}