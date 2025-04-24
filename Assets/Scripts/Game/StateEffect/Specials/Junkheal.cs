using Enums;
using Game.StateEffects.Interfaces;
using System;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Junkheal", menuName = "Game/StateEffects/SpecialEffects/Junkheal")]
    public class Junkheal : StateEffect, IHealInterceptor
    {
        void IHealInterceptor.OnPreHeal(ref int heal, ulong casterId)
        {
            GameManager.Instance.GetFirstEnemy(m_Controller.Team).Life.Hit((int)Math.Round(0.5f * heal), m_Controller.PlayerId, "Junkheal", ESpellCategory.Tick, true);
        }
    }
}