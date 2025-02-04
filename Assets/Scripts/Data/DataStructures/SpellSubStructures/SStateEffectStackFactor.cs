using Enums;
using Game;
using System;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Assets.Scripts.Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public struct SStateEffectStackFactor : INetworkSerializable
    {
        public EStateEffectTarget   Target;
        public EStateEffect         StateEffect;
        public float                BaseValue;
        public float                LevelFactor;

        public SStateEffectStackFactor(EStateEffectTarget target, float baseValue, float levelScalingFactor, EStateEffect stateEffect)
        {
            Target = target;
            BaseValue = baseValue;
            LevelFactor = levelScalingFactor;
            StateEffect = stateEffect;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Target);
            serializer.SerializeValue(ref StateEffect);
            serializer.SerializeValue(ref BaseValue);
            serializer.SerializeValue(ref LevelFactor);
        }

        public float GetBonusValue(int level, Controller caster, Controller target)
        {
            var controller = GetTarget(caster, target);
            var finalValue = GetBonusValue(level, controller.StateHandler.GetStacks(StateEffect));

            ErrorHandler.Log("Found " + controller.StateHandler.GetStacks(StateEffect) + " stacks of " + StateEffect + " on target " + controller.gameObject.name + " - adding " + finalValue + " to property", ELogTag.BonusStats);

            return finalValue;
        }

        public float GetBonusValue(int level, int nStacks)
        {
            return BaseValue * Mathf.Pow(1 + LevelFactor, level - 1) * nStacks;
        }

        public Controller GetTarget(Controller caster, Controller target)
        {
            if (! GameManager.Exists)
                return null;

            switch (Target)
            {
                case EStateEffectTarget.None:
                    ErrorHandler.Error("no StateEffectTarget provided");
                    return null;

                case EStateEffectTarget.Self:
                    return caster;

                case EStateEffectTarget.Target:
                    if (target == null)
                        ErrorHandler.Error("Provided target controller is null");
                    return target;

                case EStateEffectTarget.Ally:
                    return GameManager.Instance.GetFirstAlly(caster.Team, caster.PlayerId);

                case EStateEffectTarget.Enemy:
                    return GameManager.Instance.GetFirstEnemy(caster.Team);

                default:
                    ErrorHandler.Warning("Unahandled case : " + Target);
                    return null;
            }
        }
    }
}