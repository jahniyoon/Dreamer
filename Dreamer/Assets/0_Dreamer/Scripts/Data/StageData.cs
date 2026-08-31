using UnityEngine;

namespace Dreamer.Data
{
    [CreateAssetMenu(fileName = "NewStageData", menuName = "Data/StageData")]
    public class StageData : ScriptableObject
    {
        [field: Header("심도 목표")]
        [field: SerializeField] public int TargetDepth { get; private set; } = 1655;    // 타겟 심도 1655M

        [field: Header("스폰 비율 설정")]
        [field: SerializeField] public TileData SoftTile { get; private set; }
        [field: SerializeField] public TileData MediumTile { get; private set; }
        [field: SerializeField] public TileData HardTile { get; private set; }

        [field: Header("깊이별 난이도 가중치")]
        [field: SerializeField] public AnimationCurve HardTileSpawnCurve { get; private set; } // 깊어질수록 단단한 지층 등장 비율 증가
        [field: SerializeField] public AnimationCurve EnemySpawnCurve { get; private set; }    // 깊어질수록 적 등장 비율 증가
    }

}