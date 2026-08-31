using System.Collections.Generic;
using UnityEngine;



namespace Dreamer.Data
{
    public enum SkillType
    {
        Dash,       // 돌진
        Shockwave,  // 충격파
        Shield      // 보호막
    }

    [CreateAssetMenu(fileName = "NewSkillData", menuName = "Data/SkillData")]
    public class SkillData : ScriptableObject
    {
        [field: Header("스킬 기본 정보")]
        [field: SerializeField] public SkillType SkillType { get; private set; }
        [field: SerializeField] public string SkillName { get; private set; }
        [field: SerializeField] public Sprite SkillIcon { get; private set; }
        [field: SerializeField][ field: TextArea] public string Description { get; private set; }

        [field: Header("스탯 파라미터")]
        [field: SerializeField] public float Cooldown { get; private set; } = 5f;
        [field: SerializeField] public float Duration { get; private set; } = 1f;       // 돌진/보호막 지속시간
        [field: SerializeField] public float DamageMultiplier { get; private set; } = 1.5f; // 기본 공격력 대비 배율
        [field: SerializeField] public float RangeRadius { get; private set; } = 2f;    // 충격파/돌진 범위

        [field: Header("스킬 연출 (Juice)")]
        [field: SerializeField] public GameObject VfxPrefab { get; private set; }
        [field: SerializeField] public AudioClip CastSound { get; private set; }
        [field: SerializeField] public float CameraShakeIntensity { get; private set; } = 0.4f;
    }
}


