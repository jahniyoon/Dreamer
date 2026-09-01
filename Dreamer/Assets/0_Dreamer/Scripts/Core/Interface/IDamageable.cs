using UnityEngine;

namespace Dreamer.Core
{
    /// <summary>
    /// 지층 타일, 일반 적, 보스 등 데미지를 받을 수 있는 모든 개체가 공유하는 인터페이스
    /// </summary>
    public interface IDamageable
    {
        int CurrentHp { get; }
        bool IsDead { get; }
        int Hardness { get; }
        void TakeDamage(int damage);
    }
}