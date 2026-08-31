using UnityEngine;


namespace Dreamer.Data
{
    /// <summary>
    /// 개별 그리드 좌표의 상태 기록용 구조체 (메모리 절약을 위한 경량화 데이터)
    /// </summary>
    public struct TileGridData
    {
        public TileData TileData;
        public int CurrentHp;
        public bool IsDestroyed;

        public TileGridData(TileData tileData)
        {
            TileData = tileData;
            CurrentHp = tileData != null ? tileData.MaxHp : 1;
            IsDestroyed = false;
        }
    }

}