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
        private readonly Dictionary<int, Transform> containerDictionary = new Dictionary<int, Transform>();

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
                containerDictionary.Add(key, poolContainer.transform);

                for (int i = 0; i < initialSize; i++)
                {
                    GameObject obj = Instantiate(prefab, poolContainer.transform);
                    obj.SetActive(false);
                    poolDictionary[key].Enqueue(obj);
                }
            }
        }

        /// <summary>
        /// 풀에서 오브젝트를 가져오거나 없으면 신규 생성 (부모 Transform 지정 가능)
        /// </summary>
        public GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null, float autoReturnDelay = 0f)
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
                Transform container = containerDictionary.ContainsKey(key) ? containerDictionary[key] : transform;
                objToSpawn = Instantiate(prefab, container);
            }

            // 요청된 부모 Transform 계층 구조로 배치
            if (parent != null)
            {
                objToSpawn.transform.SetParent(parent);
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
        /// 사용 완료된 오브젝트를 풀로 반환 및 원래 풀 컨테이너 하위로 계층구조 원복
        /// </summary>
        public void ReturnToPool(GameObject prefab, GameObject instance)
        {
            if (prefab == null || instance == null) return;

            int key = prefab.GetInstanceID();
            instance.SetActive(false);

            // 반환 시 원래 생성되었던 Pool Container 하위로 원복
            if (containerDictionary.TryGetValue(key, out Transform container) && container != null)
            {
                instance.transform.SetParent(container);
            }
            else
            {
                instance.transform.SetParent(transform);
            }

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