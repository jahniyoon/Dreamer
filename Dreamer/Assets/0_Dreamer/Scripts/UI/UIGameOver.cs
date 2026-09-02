using Dreamer.Core;
using Dreamer.Item;
using Dreamer.UI;
using TMPro;
using UnityEngine;

namespace Dreamer.UI
{
    /// <summary>
    /// 게임오버 시 표시되는 UI를 담당하는 컴포넌트
    /// </summary>
    public class UIGameOver : UIObject
    {
        [SerializeField] private TMP_Text gameOverText;
        [SerializeField] private TMP_Text bestScoreText;

        [SerializeField] private TMP_Text[] itemTexts;
        [SerializeField] private Transform replayInfo;
        private bool canReplay = false;
        public override void Show()
        {
            base.Show();
            SetGameOverText(TurnManager.CurrentPlayerPosition.y);
            Invoke(nameof(CanReplay), 2f);
        }
        private void CanReplay()
        {
            canReplay = true;
            replayInfo.gameObject.SetActive(true);
        }



        public override void Hide()
        {
            base.Hide();
            replayInfo.gameObject.SetActive(false);
            canReplay = false;
        }

        public void SetGameOverText(int depth)
        {
            var depthAbs = Mathf.Max(0, Mathf.Abs(TurnManager.CurrentPlayerPosition.y));

            gameOverText.text = $"{depthAbs}<size=60%>m</size>";

            float bestScore = SaveManager.Instance.Data.BestDeapth;
            bool newRecord = depthAbs < bestScore;
            bestScoreText.gameObject.SetActive(!newRecord);
            bestScoreText.text = $"But Your Best Depth is {bestScore}<size=7\r\n0%>m...</size> \r\n";

            if(newRecord)
                SaveManager.Instance.Data.BestDeapth = depthAbs;


            for (int i = 0; i < itemTexts.Length; i++)
            {
                var textObj = itemTexts[i];
                textObj.text = $"<size=60%>x</size>{PlayerInventory.Instance.GetResourceCount((OreType)i).ToString()}";
            }
        }
    }

}
