using Dreamer.Core;
using Dreamer.UI;
using UnityEngine;

public class UITitle : UIObject
{
 
    public void StartButton()
    {
        Hide();
        GameFlowManager.Instance.StartGame();
    }
}
