using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamer.Core
{
    /// <summary>
    /// VFX, 지층 타일, 몬스터 등 가변 오브젝트의 생성/파괴 비용을 최소화하는 전역 오브젝트 풀 매니저
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {
        public static ObjectPoolManager Instance { get; private set; }

        private readonly Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();

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
            }
        }

        /// <summary>
        /// 특정 프리팹을 미리 풀에 생성 등록
        /// </summary>
        public void CreatePool(GameObject prefab, int initialSize = 10)
        {
            if (prefab == null) return;

            int key = prefab.GetInstanceID();

            if (!poolDictionary.ContainsKey(key))
            {
                poolDictionary.Add(key, new Queue<GameObject>());

                GameObject poolContainer = new GameObject($"Pool_{prefab.name}");
                poolContainer.transform.SetParent(transform);

                for (int i = 0; i < initialSize; i++)
                {
                    GameObject obj = Instantiate(prefab, poolContainer.transform);
                    obj.SetActive(false);
                    poolDictionary[key].Enqueue(obj);
                }
            }
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져오거나 없으면 신규 생성
        /// </summary>
        public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation, float autoReturnDelay = 0f)
        {
            if (prefab == null) return null;

            int key = prefab.GetInstanceID();

            if (!poolDictionary.ContainsKey(key))
            {
                CreatePool(prefab, 5);
            }

            GameObject objToSpawn = null;

            if (poolDictionary[key].Count > 0)
            {
                objToSpawn = poolDictionary[key].Dequeue();
            }
            else
            {
                // 풀이 모자랄 경우 동적 확장
                objToSpawn = Instantiate(prefab, transform);
            }

            objToSpawn.transform.SetPositionAndRotation(position, rotation);
            objToSpawn.SetActive(true);

            if (autoReturnDelay > 0f)
            {
                ReturnToPoolWithDelay(prefab, objToSpawn, autoReturnDelay);
            }

            return objToSpawn;
        }


        /// <summary>
        /// 사용 완료된 오브젝트를 풀로 반환
        /// </summary>
        public void ReturnToPool(GameObject prefab, GameObject instance)
        {
            if (prefab == null || instance == null) return;

            int key = prefab.GetInstanceID();
            instance.SetActive(false);

            if (poolDictionary.ContainsKey(key))
            {
                poolDictionary[key].Enqueue(instance);
            }
            else
            {
                Destroy(instance);
            }
        }

        /// <summary>
        /// 일정 시간 후 자동으로 풀에 반환하는 코루틴
        /// </summary>
        public void ReturnToPoolWithDelay(GameObject prefab, GameObject instance, float delay)
        {
            StartCoroutine(ReturnRoutine(prefab, instance, delay));
        }

        private IEnumerator ReturnRoutine(GameObject prefab, GameObject instance, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(prefab, instance);
        }
    }
}