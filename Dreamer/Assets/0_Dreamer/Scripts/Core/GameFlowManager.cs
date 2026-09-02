using DG.Tweening;
using Dreamer.Core;
using Dreamer.Item;
using Dreamer.Player;
using Dreamer.Tile;
using Dreamer.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Dreamer.Core
{
    public enum GameState
    {
        Ready,
        Playing,
        Paused,
        GameOver,
        Clear

    }
    /// <summary>
    /// 게임 전반적인 흐름을 관장하는 매니저
    /// </summary>
    public class GameFlowManager : MonoBehaviour
    {
        public static GameFlowManager Instance { get; private set; }
        [field: Header("REF")]
        [field: SerializeField] public Transform PlayerTitlePoint { get; private set; }
        [field: SerializeField] public Transform PlayerSpawnPoint { get; private set; }
        [field: SerializeField] public PlayerController Player { get; private set; }
        [field: SerializeField] public TileGridMapGenerator Map { get; private set; }
        [field: SerializeField] public CinemachineCamera Cam { get; private set; }
        [field: Header("연출")]
        [SerializeField] private ReplaySetting replaySetting = new ReplaySetting();
        [field: SerializeField] public GameState CurrentState { get; private set; } = GameState.Ready;
        public bool IsGameRunning => CurrentState == GameState.Playing;
        public bool IsGameOver => CurrentState == GameState.GameOver;
        // 게임 상태 변경 이벤트 (UI나 타 시스템에서 구독)
        public event Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

        }
        private void OnEnable()
        {
            Player.Stats.OnPlayerDied += TriggerGameOver;
        }
        private void OnDisable()
        {
            Player.Stats.OnPlayerDied -= TriggerGameOver;
        }

        public void Start()
        {
            SetTitle();
        }


        public void SetTitle()
        {
            UIManager.Instance.TitleUI.Show();
            SetGameState(GameState.Ready);
            Time.timeScale = 1f;
            // 스폰 포지션으로 옮겨주기
            Player.transform.position = PlayerTitlePoint.position;
            Player.Movement.SyncGridPosFromTransform();            
            Player.Stats.ResetStats();
            Player.Pickaxe.ResetPickAxe();
            AudioManager.Instance.PlayBGM("BGM_Title");
        }
        public void StartGame()
        {
            Time.timeScale = 1f;

            Map.ResetAndInitializeMap();
            Sequence seq = DOTween.Sequence();
            float duration = 1.2f; // 높은 곳에서 떨어지니 1.2초 정도로 조금 더 여유있게!

            // 현재 위치와 목표 위치
            Vector3 startPos = Player.transform.position;
            Vector3 targetPos = PlayerSpawnPoint.position;

            // 자기 키(약 1.5 ~ 2 units) 고려해서 시작 높이보다 확실히 위로 뜨는 Jump Power
            float jumpHeight = 1f; // 자기 키 이상 솟구치는 높이
            AudioManager.Instance.PlaySFX("Jump");

            // 1. 점프 이동 (높은 고도차를 고려하여 jumpPower 조정)
            seq.Append(
                Player.transform
                    .DOJump(
                        targetPos,
                        jumpPower: (startPos.y - targetPos.y) + jumpHeight, // 💡 낙하 고도차 + 솟구칠 높이 합산!
                        numJumps: 1,
                        duration: duration
                    )
                    .SetEase(Ease.Linear) // 또는 Ease.OutQuad
            );

            // 2. 공중에서 역동적으로 360도 백덤블링 (공중 회전)
            seq.Join(
                Player.transform
                    .DORotate(new Vector3(0, 0, 360f), duration, RotateMode.FastBeyond360)
                    .SetEase(Ease.OutCubic) // 시작 시 빠르게 돌다가 착지 직전 자세 잡는 찰진 느낌
            );

            // 3. 착지 시 회전 각도 및 포지션 깔끔 정렬 (안전장치)
            seq.OnComplete(() =>
            {
                Player.transform.rotation = Quaternion.identity;
                Player.transform.position = targetPos;
            });
            StartCoroutine(StartGameRoutine(duration));



        }

        IEnumerator StartGameRoutine(float duration)
        {

            yield return new WaitForSeconds(duration);

            SetGameState(GameState.Playing);
            UIManager.Instance.InGameHUDUI.Show();

            // 스폰 포지션으로 옮겨주기
            Player.transform.position = PlayerSpawnPoint.position;
            Player.Movement.SyncGridPosFromTransform();


            yield break;
        }

        public void SetPause()
        {
            Time.timeScale = 0f;
            SetGameState(GameState.Paused);
        }

        public void TriggerClear()
        {
            SetGameState(GameState.Clear);
        }

        public void TriggerGameOver()
        {
            if (IsGameOver) return;
            AudioManager.Instance.PlaySFX("BrokenPickAxe");
            AudioManager.Instance.PlayBGM("GameOver", loop : false);
            Player.Pickaxe.ResetPickAxe(true);

            SetGameState(GameState.GameOver);
            JuiceManager.Instance.ZoomCamera();
            JuiceManager.Instance.DoHitStop(1, 0.1f);

            StartCoroutine(GameOverRoutine());
        }

        IEnumerator GameOverRoutine()
        {
            UIManager.Instance.InGameHUDUI.Hide();
            UIManager.Instance.GameOverUI.Show();

            // 세이브 바로하고
            PlayerInventory.Instance.CalcurateResource();

            yield return new WaitForSeconds(2f);
            JuiceManager.Instance.ResetZoom();
            while (true)
            {
                if (Input.anyKeyDown)
                {
                    GoToTitle();
                    yield break;
                }
                yield return null;

            }
        }

        private void GoToTitle()
        {
            StartCoroutine(GoToRoutine());
        }

        IEnumerator GoToRoutine()
        {
            UIManager.Instance.GameOverUI.Hide();

            if (Player == null || Cam == null) yield break;

            // 1. 플레이어의 현재 위치에 임시 타겟 생성
            Vector3 startPos = Player.transform.position;
            GameObject target = new GameObject("Cam_Reset_Target");
            target.transform.position = startPos;

            // 2. 시네머신 카메라 추적 대상을 임시 타겟으로 교체
            Cam.Follow = target.transform;
            Map.SetTrackingTarget(Cam.Follow);

            // 3. 이동할 목표 위치 
            Vector3 targetPos = PlayerTitlePoint.position;
            float distance = Vector3.Distance(startPos, targetPos);
            // 4. 거리에 비례한 이동 시간 계산 (Clamp로 최소/최대 시간 보장)
            float duration = Mathf.Clamp(distance * replaySetting.moveSpeedPerUnit, replaySetting.minDuration, replaySetting.maxDuration);

            // 5. DOTween을 이용해 임시 타겟을 지상(Y = 0)으로 부드럽게 올림
            bool isTweenFinished = false;

            target.transform.DOMove(targetPos, duration)
                .SetEase(Ease.OutCubic) // 시작 시 빠르고 도착 직전에 부드럽게 감속하는 이징
                .OnComplete(() => isTweenFinished = true);

            // Tween 연출 완료 대기
            yield return new WaitUntil(() => isTweenFinished);

            // 6. 플레이어를 원점으로 복귀시키고 카메라 추적 원복
            Player.transform.position = targetPos;
            Cam.Follow = Player.transform;
            Map.SetTrackingTarget(Cam.Follow);


            // 7. 사용이 끝난 임시 타겟 파괴
            Destroy(target);

            SetTitle();
        }






        public void SetGameState(GameState newState)
        {
            CurrentState = newState;
            OnGameStateChanged?.Invoke(newState);
        }
    }
}


[System.Serializable]
public class ReplaySetting
{
    public float moveSpeedPerUnit = 5f; // 초 단위{ }
    public float minDuration = 5f; // 초 단위{ }
    public float maxDuration = 5f; // 초 단위{ }
}
