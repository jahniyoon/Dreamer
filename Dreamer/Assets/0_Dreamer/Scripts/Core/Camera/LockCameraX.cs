using Unity.Cinemachine;
using UnityEngine;

namespace Dreamer.Core
{
    /// <summary>
    /// Cinemachine Virtual Camera의 X축 위치를 특정 값으로 고정하는 확장 스크립트
    /// </summary>
    [ExecuteInEditMode]
    [SaveDuringPlay]
    [AddComponentMenu("")] // Add Component 메뉴 감추기
    public class LockCameraX : CinemachineExtension
    {
        [Tooltip("고정할 카메라 X 좌표")]
        [SerializeField] private float targetXPosition = 0f;

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            // 카메라의 최종 Transform 결정 단계(Body)에서 X축 좌표 강제 고정
            if (stage == CinemachineCore.Stage.Body)
            {
                Vector3 pos = state.RawPosition;
                pos.x = targetXPosition;
                state.RawPosition = pos;
            }
        }
    }
}