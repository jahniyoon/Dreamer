using DG.Tweening;
using Dreamer.Core;
using Dreamer.Data;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Dreamer.UI
{
    public enum SkillType
    {
        A,
        B,
        C
    }
    public class SkillButtonUI : MonoBehaviour
    {
        [Header("UI 요소 연결")]
        [SerializeField] private Image skillIconImage;      // 스킬 아이콘
        [SerializeField] private Image cooldownOverlay;    // 쿨타임 덮개 (Image Type: Filled)
        [SerializeField] private TextMeshProUGUI cooldownText; // 남은 쿨타임 초 표시
        [SerializeField] private Button skillButton;        // 스킬 버튼 (터치/클릭용)
        [SerializeField] private SkillData skillData;
        public SkillType SkillType;
        private float cooldownDuration = 0f;
        private float currentCooldown = 0f;
        private bool isCoolingDown = false;

        private void Awake()
        {
            if (skillButton != null)
            {
                skillButton.onClick.AddListener(OnClickSkillButton);
            }


        }
        
        private void Update()
        {
            if (!isCoolingDown) return;

            currentCooldown -= Time.deltaTime;

            if (currentCooldown <= 0f)
            {
                // 쿨타임 완료 처리
                isCoolingDown = false;
                currentCooldown = 0f;
                if (cooldownOverlay != null) cooldownOverlay.fillAmount = 0f;
                if (cooldownText != null) cooldownText.text = "";
                if (skillButton != null) skillButton.interactable = true;

                // 쿨타임 완료 시 살짝 튀는 반짝 연출 (Punch Scale)
                transform.DOPunchScale(Vector3.one * 0.15f, 0.2f);
                AudioManager.Instance?.PlaySFX("SkillReady"); // 준비 완료 SFX (옵션)
            }
            else
            {
                // 쿨타임 진행 중 UI 갱신
                if (cooldownOverlay != null) cooldownOverlay.fillAmount = currentCooldown / cooldownDuration;
                if (cooldownText != null) cooldownText.text = Mathf.CeilToInt(currentCooldown).ToString();
            }
        }

        /// <summary>
        /// 스킬 실행 시 호출 (쿨타임 시작)
        /// </summary>
        public void UseSkill()
        {
            
            if (isCoolingDown) return;

            cooldownDuration = skillData.Cooldown;
            currentCooldown = skillData.Cooldown;
            isCoolingDown = true;

            if (skillButton != null) skillButton.interactable = false;

            // 눌렸을 때 살짝 눌리는 연출 + 사운드
            transform.DOPunchScale(Vector3.one * -0.1f, 0.1f);
            AudioManager.Instance?.PlaySFX("SkillUse");
        }

   

        private void OnClickSkillButton()
        {
            if (isCoolingDown) return;

            var skill = GameFlowManager.Instance.Player.Skill;

            switch (SkillType)
            {
                case SkillType.A:
                    skill.HandleSkillA();
                    break;
                case SkillType.B:
                    skill.HandleSkillB();

                    break;
                case SkillType.C:
                    skill.HandleSkillC();

                    break;
                default:
                    break;
            }

      
        }
    }
}