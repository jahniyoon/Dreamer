using System.Collections.Generic;
using UnityEngine;



namespace Dreamer.Data
{

    public enum TileType
    {
        SoftEarth = 0,   
        MediumRock = 1,  
        HardRock = 2,
        Obsidian = 3
    }
    [CreateAssetMenu(fileName = "NewTileData", menuName = "Data/TileData")]
    public class TileData : ScriptableObject
    {
        [field: Header("지층 기본 정보")]
        [field: SerializeField] public TileType TileType { get; private set; }
        [field: SerializeField] public string TileName { get; private set; }
        [field: SerializeField] public int MaxHp { get; private set; } = 1;               // 부수는데 필요한 타격 횟수
        [field: SerializeField] public Sprite TileSprite { get; private set; }
        [field: SerializeField] public Color TileColor { get; private set; } = Color.white;

        [field: Header("파괴 피드백 (Juice)")]
        [field: SerializeField] public GameObject DestroyParticlePrefab { get; private set; }
        [field: SerializeField] public AudioClip DestroySound { get; private set; }
        [field: SerializeField] public float CameraShakeIntensity { get; private set; } = 0.2f;

        [field: Header("지층 드롭 테이블")]
        [field: SerializeField] public List<ItemData> DropItems { get; private set; }
        [field: SerializeField][field: Range(0f, 1f)] public float ItemDropRate { get; private set; } = 0.05f; // 지층 파괴 시 아이템 드롭 확률
    }
}
