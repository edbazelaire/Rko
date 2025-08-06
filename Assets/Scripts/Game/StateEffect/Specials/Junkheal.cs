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
            // convert heal to damages
            GameManager.Instance.GetFirstEnemy(m_Controller.Team).Life.Hit(
                damage:         (int)Math.Round(0.5f * heal), 
                casterId:       m_Controller.PlayerId, 
                source:         "Junkheal", 
                spellCategory:  ESpellCategory.Tick, 
                ignoreRes:      true
            );

            // reduce healing by 50%
            heal = (int)Math.Ceiling(0.5f * heal);
        }
    }
}