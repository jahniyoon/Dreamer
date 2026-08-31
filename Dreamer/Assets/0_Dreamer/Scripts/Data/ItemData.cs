using UnityEngine;



namespace Dreamer.Data
{

    public enum ItemType
    {
        Weapon,     // 공격력 증가
        Armor,      // 방어력/체력 증가
        Accessory   // 이동속도/스킬 쿨타임 감소 등
    }
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Data/ItemData")]
    public class ItemData : ScriptableObject
    {

        [field: Header("기본 정보")]
        [field: SerializeField] public string ItemId { get; private set; }
        [field: SerializeField] public string ItemName { get; private set; }
        [field: SerializeField] public ItemType ItemType { get; private set; }
        [field: SerializeField] public Sprite ItemIcon { get; private set; }
        [field: SerializeField][field: TextArea] public string Description { get; private set; }

        [field: Header("능력치 보너스")]
        [field: SerializeField] public int AttackBonus { get; private set; }
        [field: SerializeField] public int DefenseBonus { get; private set; }
        [field: SerializeField] public int MaxHpBonus { get; private set; }
        [field: SerializeField] public float MoveSpeedBonus { get; private set; }
        [field: SerializeField] public float CooldownBonus { get; private set; }

        [field: Header("드롭 정보")]
        [field: SerializeField][field: Range(0f, 100f)] public float DropChance { get; private set; } = 15f;
    }
}
