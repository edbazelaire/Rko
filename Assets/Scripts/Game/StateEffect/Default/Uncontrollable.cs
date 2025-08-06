using Enums;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Uncontrollable", menuName = "Game/StateEffects/Uncontrollable")]
    public class Uncontrollable : StateEffect
    {
        public static List<string> CC_EFFECTS => new List<string>() { 
            EStateEffect.Silence.ToString(),
            EStateEffect.Stun.ToString(),
            EStateEffect.Frost.ToString(),
            EStateEffect.Frozen.ToString(),
            EStateEffect.Scorched.ToString(),
        };

        protected override void OnStart()
        {
            foreach (var state in CC_EFFECTS)
            {
                if (!m_Controller.StateHandler.HasState(state))
                    continue;
                
                m_Controller.StateHandler.RemoveStateEffect(state, consume: false);
            }

            base.OnStart();
        }
    }
}