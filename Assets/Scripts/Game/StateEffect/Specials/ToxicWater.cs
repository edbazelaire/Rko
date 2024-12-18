using Enums;
using UnityEditor;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "ToxicWater", menuName = "Game/StateEffects/SpecialEffects/ToxicWater")]
    public class ToxicWater : StateEffect
    {
        public override void Update()
        {
            base.Update();

            if (!m_Controller.StateHandler.HasState(EStateEffect.Frozen))
                End();
        }
    }
}