using DG.Tweening;
using Dreamer.Core;
using UnityEngine;

namespace Dreamer.Enemy
{
    /// <summary>
    /// 보스 패턴으로 스폰되어 아래로 일정 속도로 낙하하며,
    /// 플레이어와 충돌 시 데미지를 주고 풀로 자동 반납되는 투사체
    /// </summary>
    public class FallingRock : MonoBehaviour
    {

        [Header("낙석 속성")]
        [SerializeField] private int damage = 15;        // 플레이어 타격 데미지
        [SerializeField] private float fallSpeed = 6f;   // 낙하 속도
        [SerializeField] private float maxLifetime = 5f;  // 화면 밖으로 지나쳤을 때 자동 파괴 시간

        [Header("시각 연출")]
        [SerializeField] private float rotateSpeed = 360f; // 떨어질 때 회전 속도

        private float lifetimeTimer = 0f;
        private bool hasCollided = false;


        private void OnEnable()
        {
            lifetimeTimer = 0f;
            hasCollided = false;

            // 떨어지면서 360도 연속 회전하는 쥬시한 연출
            transform.DORotate(new Vector3(0f, 0f, 360f), 1f, RotateMode.FastBeyond360)
                .SetLoops(-1, LoopType.Incremental)
                .SetEase(Ease.Linear);
        }

        private void OnDisable()
        {
            // DOTween 애니메이션 정지
            transform.DOKill();
        }

        private void Update()
        {
            if (!GameFlowManager.Instance.IsGameRunning) return;

            // 1. 단순 y축 하강 이동
            transform.Translate(Vector3.down * (fallSpeed * Time.deltaTime), Space.World);

            // 2. 수명 타이머 지나면 자동 회수
            lifetimeTimer += Time.deltaTime;
            if (lifetimeTimer >= maxLifetime)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (hasCollided) return;

            // 플레이어 태그 감지
            if (collision.CompareTag("Player"))
            {
                hasCollided = true;

                // 플레이어의 IDamageable 인터페이스 호출
                if (collision.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                    Debug.Log($"[FallingRock] 💥 낙석이 플레이어에게 {damage} 데미지 부여!");
                }
                gameObject.SetActive(false);


            }
        }


    }
}