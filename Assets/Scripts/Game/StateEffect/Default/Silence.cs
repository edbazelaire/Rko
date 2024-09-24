using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Silence", menuName = "Game/StateEffects/Silence")]
    public class Silence : StateEffect
    {
        protected override bool CheckBeforeGraphicInit()
        {
            // if player is casting, improve the duration
            if (m_Controller.SpellHandler.IsCasting)
            {
                m_Duration *= 2.5f;
            }

            return base.CheckBeforeGraphicInit();
        }
    }
}