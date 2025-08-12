using Data;
using Enums;
using Game.StateEffects.Interfaces;
using System;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Kill", menuName = "Game/StateEffects/SpecialEffects/Kill")]
    public class Kill : StateEffect
    {
        public override bool Initialize(Controller controller, Controller caster, SStateEffectData? stateEffectData = null, int stacks = 1)
        {
            controller.Life.Kill();
            return true;
        }
    }
}