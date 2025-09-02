using Data;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Kill", menuName = "Game/StateEffects/SpecialEffects/Kill")]
    public class Kill : StateEffect
    {
        public override bool Initialize(Controller controller, Controller caster, SStateEffectData? stateEffectData = null, int? stacks = null)
        {
            controller.Life.Kill();
            return true;
        }
    }
}