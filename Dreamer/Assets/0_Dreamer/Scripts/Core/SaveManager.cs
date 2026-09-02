using Dreamer.Data;
using Dreamer.Player;
using System.IO;
using UnityEngine;

namespace Dreamer.Core
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        public SaveData Data { get; private set; }
        private const string SAVE_KEY = "SaveData_Json";
        private string saveFilePath;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                saveFilePath = Path.Combine(Application.persistentDataPath, "save_data.json");
                LoadGame();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        private void Update()
        {
            // 💡 [치트/테스트용] F12 키를 누르면 세이브 데이터 즉시 초기화!
            if (Input.GetKeyDown(KeyCode.F12))
            {
                ResetSaveData();
            }
       
        }
        /// <summary>
        /// 세이브 데이터 전체 삭제 및 최초 상태 초기화
        /// </summary>
        public void ResetSaveData()
        {
            // 1. 저장된 PlayerPrefs 데이터 완전 삭제
            PlayerPrefs.DeleteKey(SAVE_KEY);
            PlayerPrefs.DeleteAll(); // 필요 시 전체 삭제
            PlayerPrefs.Save();

            // 2. 세이브 데이터 객체 새로 생성 (기본값으로 리셋)
            Data = new SaveData();

            // 3. 플레이어 스탯 즉시 재계산 및 풀피 초기화
            GameFlowManager.Instance.Player.Stats.ResetStats();

            // 4. 씬 내 UI가 있다면 재갱신 처리 (옵션)
            // FindObjectOfType<PickaxeShopUI>()?.RefreshPageUI();

            Debug.Log("🧹 [SaveManager] 세이브 데이터가 완전히 초기화되었습니다! (F12)");
        }

        public void SaveGame()
        {
            if (Data == null) Data = new SaveData();
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"💾 세이브 완료: {saveFilePath}");
        }

        public void LoadGame()
        {
            if (File.Exists(saveFilePath))
            {
                string json = File.ReadAllText(saveFilePath);
                Data = JsonUtility.FromJson<SaveData>(json);
            }
            else
            {
                Data = new SaveData();
                SaveGame();
            }
        }
    }
}