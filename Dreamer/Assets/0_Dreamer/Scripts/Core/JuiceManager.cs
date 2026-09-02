using System.Collections;
using Unity.Cinemachine;
using UnityEngine;


namespace Dreamer.Core
{
    /// <summary>
    /// 히트스톱, 카메라 흔들림, 효과음 피치 변형 등 타격감 연출 전담 매니저
    /// </summary>
    public class JuiceManager : MonoBehaviour
    {
        public static JuiceManager Instance { get; private set; }

        [Header("카메라 연출")]
        [SerializeField] private CinemachineImpulseSource impulseSource;

        [Header("사운드 설정")]
        [SerializeField] private AudioSource sfxAudioSource;

        private Coroutine hitStopCoroutine;
        private bool isHitStopping;

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

            if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();
            if (sfxAudioSource == null) sfxAudioSource = GetComponent<AudioSource>();
        }

        /// <summary>
        /// 타격 시 순간적으로 시간을 멈추는 프레임 정지 연출
        /// </summary>
        public void DoHitStop(float duration = 0.05f, float timeScale = 0.05f)
        {
            if (isHitStopping)
            {
                StopCoroutine(hitStopCoroutine);
            }

            hitStopCoroutine = StartCoroutine(HitStopRoutine(duration, timeScale));
        }

        private IEnumerator HitStopRoutine(float duration, float targetTimeScale)
        {
            isHitStopping = true;
            Time.timeScale = targetTimeScale;

            // Time.timeScale 변경 영향을 받지 않는 Unscaled Time 사용
            yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = 1f;
            isHitStopping = false;
        }

        /// <summary>
        /// Cinemachine Impulse 기반 카메라 흔들림 연출
        /// </summary>
        public void ShakeCamera(float force = 0.3f)
        {
            if (impulseSource != null)
            {
                impulseSource.GenerateImpulse(force);
            }
        }

        /// <summary>
        /// 사운드 피치(0.9~1.1)를 랜덤 변형하여 동일 효과음 타격감 향상
        /// </summary>
        public void PlaySfxWithPitch(AudioClip clip, float baseVolume = 1f, float pitchVariance = 0.1f)
        {
            if (clip == null || sfxAudioSource == null) return;

            sfxAudioSource.pitch = Random.Range(1f - pitchVariance, 1f + pitchVariance);
            sfxAudioSource.PlayOneShot(clip, baseVolume);
        }

        /// <summary>
        /// ObjectPoolManager를 통한 이펙트 파티클 스폰 및 자동 반환
        /// </summary>
        public void SpawnVfx(GameObject vfxPrefab, Vector3 spawnPosition, float autoReturnDelay = 2f)
        {
            if (vfxPrefab == null) return;

            if (ObjectPoolManager.Instance != null)
            {
                //GameObject vfx = ObjectPoolManager.Instance.SpawnFromPool(vfxPrefab, spawnPosition, Quaternion.identity, autoReturnDelay : autoReturnDelay);
            }
            else
            {
                GameObject vfx = Instantiate(vfxPrefab, spawnPosition, Quaternion.identity);
                Destroy(vfx, autoReturnDelay);
            }
        }
    }
}