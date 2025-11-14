using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Silence", menuName = "Game/StateEffects/Silence")]
    public class Silence : StateEffect
    {
        protected override bool CheckBeforeGraphicInit()
        {
            return base.CheckBeforeGraphicInit();
        }
    }
}