using System;
using UnityEngine;


namespace Dreamer.Core
{
    /// <summary>
    /// 플레이어의 행동(이동, 채굴, 스킬 등)에 맞춰 턴 이벤트를 중계하는 매니저
    /// </summary>
    public static class TurnManager
    {
        public static Vector2Int CurrentPlayerPosition { get; set; }
        public static event Action OnPlayerTurnExecuted;

        public static void DispatchPlayerTurn()
        {
            OnPlayerTurnExecuted?.Invoke();
        }
        public static void UpdatePlayerPosition(Vector2Int newPosition)
        {
            CurrentPlayerPosition = newPosition;

        }
    }

}
