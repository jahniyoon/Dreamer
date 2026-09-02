using Dreamer.Item;
using UnityEngine;



namespace Dreamer.Data
{

    [CreateAssetMenu(fileName = "NewItemData", menuName = "Data/ItemData")]
    public class ItemData : ScriptableObject
    {
        [field: Header("기본 정보")]
        [field: SerializeField] public string ItemId { get; private set; }
        [field: SerializeField] public string ItemName { get; private set; }
        [field: SerializeField] public Sprite ItemIcon { get; private set; }
        [field: SerializeField][field: TextArea] public string Description { get; private set; }

        [field: Header("능력치")]
        [field: SerializeField] public int BaseAttack { get; private set; }
        [field: SerializeField] public int BaseDefense { get; private set; }
        [field: SerializeField] public int BaseMaxHp { get; private set; }
        [field: SerializeField] public float BaseLightRange { get; private set; }

        [field: Header("구매 및 가격 정보")]
        [field: SerializeField] public OreType PriceType { get; private set; } = OreType.Diamond;
        [field: SerializeField] public int PriceValue { get; private set; } = 100;

        public string GetPriceString()
        {
            return PriceType switch
            {
                OreType.Iron => "<sprite name=\"resource_sprite_1\">",
                OreType.Diamond => "<sprite name=\"resource_sprite_0\">",
                OreType.Gold => "<sprite name=\"resource_sprite_2\">",
                OreType.Mushroom => "<sprite name=\"resource_sprite_3\">",
                _ => ""
            };
        }

        private string GetOreIconTag(OreType type)
        {
            return type switch
            {
                OreType.Iron => "<sprite name=\"iron\">",
                OreType.Diamond => "<sprite name=\"diamond\">",
                OreType.Gold => "<sprite name=\"gold\">",
                OreType.Mushroom => "<sprite name=\"mushroom\">",
                _ => ""
            };
        }
    }
}
