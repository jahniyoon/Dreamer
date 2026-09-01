using DG.Tweening;
using TMPro;
using UnityEngine;
using Dreamer.Core;

namespace Dreamer.UI
{
    /// <summary>
    /// 개별 데미지 텍스트의 팝업, 튀어오름, 페이드아웃 및 풀 반환 연출을 담당하는 컴포넌트
    /// </summary>
    public class DamageText : MonoBehaviour
    {
        [SerializeField] private TMP_Text textMesh;
        private Tween animTween;


        public void Show(int damage, bool isPlayerDamage, Vector3 position)
        {
            animTween?.Kill();

            transform.position = position;
            transform.rotation = Quaternion.identity;

            if (textMesh != null)
            {
                textMesh.text = isPlayerDamage ? $"<size=60%>-{damage}</size>" : damage.ToString();

                textMesh.color = isPlayerDamage
                    ? DamageTextManager.Instance.playerColor
                    : DamageTextManager.Instance.enemyColor;

                textMesh.alpha = 1f;
            }

            // ==========================================
            // 크기
            // ==========================================

            // 처음부터 크게
            float startScale = isPlayerDamage ? 1.25f : 1.1f;

            // 마지막에는 자연스럽게 작아짐
            float endScale = 0.65f;

            transform.localScale = Vector3.one * startScale;


            // ==========================================
            // 랜덤 방향
            // ==========================================

            // 좌우 랜덤
            float randomX = UnityEngine.Random.Range(-0.1f, 0.1f);

            // 최종 위치
            Vector3 targetPos = position + new Vector3(
                randomX,
                DamageTextManager.Instance.targetPosY,
                0f
            );


            // ==========================================
            // 튀어오르는 높이
            // ==========================================

            float jumpPower = DamageTextManager.Instance.jumpPower;

            // 전체 이동 시간
            float duration = DamageTextManager.Instance.duration;

            // ==========================================
            // 회전
            // ==========================================

            float randomAngle = UnityEngine.Random.Range(-45f, 45f);


            Sequence seq = DOTween.Sequence();


            // ------------------------------------------
            // A. 이동
            // ------------------------------------------

            seq.Append(
                transform
                    .DOJump(
                        targetPos,
                        jumpPower,
                        1,
                        duration
                    )
                    .SetEase(Ease.OutQuad)
            );


            // ------------------------------------------
            // B. 크기
            // ------------------------------------------

            // 커졌다 작아지는 게 아니라
            // 처음부터 끝까지 계속 감소
            seq.Join(
                transform
                    .DOScale(
                        Vector3.one * endScale,
                        duration
                    )
                    .SetEase(Ease.InQuad)
            );


            // ------------------------------------------
            // C. 회전
            // ------------------------------------------

            seq.Join(
                transform
                    .DORotate(
                        new Vector3(0f, 0f, randomAngle),
                        duration
                    )
                    .SetEase(Ease.OutQuad)
            );


            // ------------------------------------------
            // D. 페이드
            // ------------------------------------------

            if (textMesh != null)
            {
                // 후반부부터 사라지기 시작
                seq.Insert(
                    duration * (1 - DamageTextManager.Instance.fadeRatio),
                    textMesh
                        .DOFade(0f, duration * DamageTextManager.Instance.fadeRatio)
                        .SetEase(Ease.InQuad)
                );
            }

            animTween = seq;
        }

        private void OnDestroy()
        {
            animTween?.Kill();
        }
    }
}