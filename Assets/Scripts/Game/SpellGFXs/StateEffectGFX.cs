using Data;
using Enums;
using Game.Spells;
using Tools;
using UnityEngine;



namespace Game.SpellGFXs
{
    public class StateEffectGFX : BaseSpellGFX<EStateEffectEvent>
    {
        #region Members


        #endregion


        #region Init
        
        #endregion


        #region End

        /// <summary>
        /// Check if the graphics should end
        /// </summary>
        /// <param name="spellEvent"></param>
        protected virtual void CheckEnd(EStateEffectEvent stateEffectEvent)
        {
            if (m_PrefabSpawn.GFXLifetime.EndSpellPart == EStateEffectEvent.None || m_PrefabSpawn.GFXLifetime.EndSpellPart != stateEffectEvent)
                return;

            End();
        }

        #endregion


        #region Duration

        /// <summary>
        /// Check if SpellGFX is alive at the provided time
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <returns></returns>
        bool IsGFXAlive(EStateEffectEvent effectEvent)
        {
            return m_PrefabSpawn.GFXLifetime.StartSpellPart <= effectEvent && effectEvent < m_PrefabSpawn.GFXLifetime.EndSpellPart;
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Controller.StateHandler.StateEffectEvent += OnStateEffectEvent;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Controller.StateHandler.StateEffectEvent -= OnStateEffectEvent;
        }

        void OnStateEffectEvent(EStateEffectEvent stateEffectEvent, string stateEffectName, int stacks, int maxStacks, float duration)
        {
            if (stateEffectName != m_StateEffectName)
                return;

            CheckEnd(stateEffectEvent);
        }

        #endregion
    }
}