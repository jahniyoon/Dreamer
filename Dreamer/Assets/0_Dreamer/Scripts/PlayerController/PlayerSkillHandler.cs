using Dreamer.Data;
using Dreamer.Skill;
using UnityEngine;
using UnityEngine.Events;

namespace Dreamer.Player
{

    /// <summary>
    /// 플레이어의 스킬 등록, 쿨타임 및 키 입력을 총괄 처리하는 컴포넌트
    /// </summary>
    public class PlayerSkillHandler : MonoBehaviour
    {
        [Header("레이어 설정")]
        [SerializeField] private LayerMask destructibleTileLayer;
        [SerializeField] private LayerMask enemyLayer;

        [Header("장착 스킬 목록 (SO Data)")]
        [SerializeField] private SkillData skillSlotAData;
        [SerializeField] private SkillData skillSlotBData;
        [SerializeField] private SkillData skillSlotCData;
        public UnityEvent activeASkillEvent = new();
        public UnityEvent activeBSkillEvent = new();
        public UnityEvent activeCSkillEvent = new();

        public SkillBase SkillA { get; private set; }
        public SkillBase SkillB { get; private set; }
        public SkillBase SkillC { get; private set; }


        public PlayerController Controller { get; private set; }
        public PlayerMove Movement { get; private set; }
        public PlayerInputHandler InputHandler { get; private set; }
        public PlayerStatsHandler StatsHandler { get; private set; }
        public LayerMask DestructibleTileLayer => destructibleTileLayer;
        public LayerMask EnemyLayer => enemyLayer;
        public bool IsInvincible { get; private set; }

        private void Awake()
        {
            Controller = GetComponent<PlayerController>();
            Movement = GetComponent<PlayerMove>();
            InputHandler = GetComponent<PlayerInputHandler>();
            StatsHandler = GetComponent<PlayerStatsHandler>();

            InitSkills();
        }

        private void OnEnable()
        {
            if (InputHandler == null)
            {
                InputHandler = GetComponent<PlayerInputHandler>();
            }

            if (InputHandler != null)
            {
                InputHandler.OnSkillAInput += HandleSkillA;
                InputHandler.OnSkillBInput += HandleSkillB;
                InputHandler.OnSkillCInput += HandleSkillC;
            }
        }

        private void OnDisable()
        {
            if (InputHandler != null)
            {
                InputHandler.OnSkillAInput -= HandleSkillA;
                InputHandler.OnSkillBInput -= HandleSkillB;
                InputHandler.OnSkillCInput -= HandleSkillC;
            }
        }

        public void HandleSkillA()
        {
            UseSkillA();
        }

        public void HandleSkillB()
        {
            UseSkillB();
        }
        public void HandleSkillC()
        {
            UseSkillC();
        }

        public void InitSkills()
        {
            if (skillSlotAData != null) SkillA = CreateSkillInstance(skillSlotAData);
            if (skillSlotBData != null) SkillB = CreateSkillInstance(skillSlotBData);
            if (skillSlotCData != null) SkillC = CreateSkillInstance(skillSlotCData);
        }

        public bool UseSkillA()
        {
            activeASkillEvent?.Invoke();
            return SkillA != null && SkillA.Execute(this);
        }
        public bool UseSkillB()
        {
            activeBSkillEvent?.Invoke();
         return   SkillB != null && SkillB.Execute(this);
        }
        public bool UseSkillC()
        {
            activeCSkillEvent?.Invoke();
            return SkillC != null && SkillC.Execute(this);
        }

        public void SetInvincible(bool invincible)
        {
            IsInvincible = invincible;
            Debug.Log($"[Skill] 곡괭이 보호막 상태: {IsInvincible}");
        }

        private SkillBase CreateSkillInstance(SkillData data)
        {
            return data.SkillType switch
            {
                SkillType.Shockwave => new ShockwaveSkill(data),
                SkillType.Dash => new DashSkill(data),
                SkillType.Shield => new ShieldSkill(data),
                _ => null
            };
        }
    }
}