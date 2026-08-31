using Dreamer.Data;
using UnityEngine;

namespace Dreamer.Enemy
{
    /// <summary>
    /// 깊이별 일반 적 스폰 가중치 설정 클래스
    /// </summary>
    [System.Serializable]
    public class EnemySpawnRule
    {
        [SerializeField] private string ruleName = "New Enemy Rule";
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int minDepth = 10; // 최소 등판 깊이 (절대 깊이 M)
        [SerializeField] private AnimationCurve weightByDepth = AnimationCurve.Linear(0f, 1f, 1f, 1f); // 깊이별 정규화 가중치

        public string RuleName => ruleName;
        public EnemyData EnemyData => enemyData;
        public GameObject EnemyPrefab => enemyPrefab;
        public int MinDepth => minDepth;

        public float GetWeight(int currentDepth, int maxTargetDepth)
        {
            if (currentDepth < minDepth || enemyPrefab == null) return 0f;

            float normalizedDepth = Mathf.Clamp01((float)currentDepth / Mathf.Max(1, maxTargetDepth));
            return Mathf.Max(0f, weightByDepth.Evaluate(normalizedDepth));
        }
    }
    }