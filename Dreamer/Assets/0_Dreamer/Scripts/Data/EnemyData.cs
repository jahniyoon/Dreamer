using System.Collections.Generic;
using UnityEngine;



namespace Dreamer.Data
{


    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "Data/EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [field: Header("기본 정보")]
        [field: SerializeField] public string EnemyId { get; private set; }
        [field: SerializeField] public string EnemyName { get; private set; }
        [field: SerializeField] public bool IsBoss { get; private set; } = false;
        [field: SerializeField] public Sprite EnemySprite { get; private set; }

        [field: Header("스탯")]
        [field: SerializeField] public int MaxHp { get; private set; } = 10;
        [field: SerializeField] public int AttackPower { get; private set; } = 2;
        [field: SerializeField] public float MoveSpeed { get; private set; } = 2.5f;

        [field: Header("드롭 아이템 테이블")]
        [field: SerializeField] public List<ItemData> PossibleDrops { get; private set; }
        [field: SerializeField][field: Range(0f, 1f)] public float DropRate { get; private set; } = 0.3f;

        [field: Header("피드백 연출")]
        [field: SerializeField] public AudioClip HitSound { get; private set; }
        [field: SerializeField] public AudioClip DeathSound { get; private set; }
        [field: SerializeField] public GameObject DeathVfxPrefab { get; private set; }
    }
}


