using UnityEngine;
using Dreamer.Data;

namespace Dreamer.Tile
{
    /// <summary>
    /// 깊이에 따른 지층 타일 스폰 규칙 설정 구조체
    /// </summary>
    [System.Serializable]
    public class TileSpawnRule
    {
        [SerializeField] private string ruleName = "New Tile Rule";
        [SerializeField] private TileData tileData;
        [SerializeField] private int minDepth = 0; // 최소 등판 깊이 (절대 깊이 M)
        [SerializeField] private AnimationCurve weightByDepth = AnimationCurve.Linear(0f, 1f, 1f, 1f); // 0.0 ~ 1.0 정규화 비율 가중치 커브

        public string RuleName => ruleName;
        public TileData TileData => tileData;
        public int MinDepth => minDepth;
        public AnimationCurve WeightByDepth => weightByDepth;

        /// <summary>
        /// 현재 깊이에 해당하는 스폰 가중치 산출 (0.0 ~ 1.0 정규화)
        /// </summary>
        public float GetWeight(int currentDepth, int maxTargetDepth)
        {
            if (currentDepth < minDepth || tileData == null) return 0f;

            // 최대 목표 심도 대비 현재 깊이의 비율 (0.0 ~ 1.0)
            float normalizedDepth = Mathf.Clamp01((float)currentDepth / Mathf.Max(1, maxTargetDepth));

            return Mathf.Max(0f, weightByDepth.Evaluate(normalizedDepth));
        }
    }

}