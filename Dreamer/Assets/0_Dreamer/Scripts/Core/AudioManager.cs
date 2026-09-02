using System.Collections.Generic;
using UnityEngine;

namespace Dreamer.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Audio Clips (Resources/Audio 폴더 자동 로드 또는 인스펙터 할당)")]
        [SerializeField] private List<AudioClip> audioClips = new List<AudioClip>();

        private Dictionary<string, AudioClip> clipDict = new Dictionary<string, AudioClip>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitAudioSources();
                LoadClips();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitAudioSources()
        {
            if (bgmSource == null)
            {
                GameObject bgmObj = new GameObject("BGM_Source");
                bgmObj.transform.SetParent(transform);
                bgmSource = bgmObj.AddComponent<AudioSource>();
                bgmSource.loop = true;
            }

            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX_Source");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
            }
        }

        private void LoadClips()
        {
            clipDict.Clear();

            // 1. 인스펙터에 등록된 클립 먼저 등록
            foreach (var clip in audioClips)
            {
                if (clip != null && !clipDict.ContainsKey(clip.name))
                {
                    clipDict.Add(clip.name, clip);
                }
            }

            // 2. Resources/Audio 폴더에 있는 음원들도 자동 로드
            AudioClip[] loadedClips = Resources.LoadAll<AudioClip>("Audio");
            foreach (var clip in loadedClips)
            {
                if (clip != null && !clipDict.ContainsKey(clip.name))
                {
                    clipDict.Add(clip.name, clip);
                }
            }
        }

        #region BGM 제어

        /// <summary>
        /// BGM 재생 (클립 이름 기준)
        /// </summary>
        public void PlayBGM(string clipName, float volume = 1f, bool loop = true)
        {


            if (clipDict.TryGetValue(clipName, out AudioClip clip))
            {
                if (bgmSource.clip == clip && bgmSource.isPlaying) return;

                bgmSource.clip = clip;
                bgmSource.volume = volume;
                bgmSource.Play();
                bgmSource.loop = loop;
            }
            else
            {
                Debug.LogWarning($"⚠️ [AudioManager] BGM 클립을 찾을 수 없습니다: {clipName}");
            }
        }

        public void StopBGM()
        {
            if (bgmSource != null) bgmSource.Stop();
        }

        #endregion

        #region SFX (효과음) 제어

        /// <summary>
        /// SFX 원샷 재생 (중첩 재생 가능)
        /// </summary>
        public void PlaySFX(string clipName, float volume = 1f)
        {
            if (clipDict.TryGetValue(clipName, out AudioClip clip))
            {
                sfxSource.PlayOneShot(clip, volume);
            }
            else
            {
                Debug.LogWarning($"⚠️ [AudioManager] SFX 클립을 찾을 수 없습니다: {clipName}");
            }
        }

        /// <summary>
        /// AudioClip 직접 전달 시 원샷 재생
        /// </summary>
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        #endregion

        #region 볼륨 조절

        public void SetBGMVolume(float volume)
        {
            if (bgmSource != null) bgmSource.volume = Mathf.Clamp01(volume);
        }

        public void SetSFXVolume(float volume)
        {
            if (sfxSource != null) sfxSource.volume = Mathf.Clamp01(volume);
        }

        #endregion
    }
}