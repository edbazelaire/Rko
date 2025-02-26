using Enums;
using Game.Spells;
using Tools;
using UnityEngine;


namespace Game.SpellGFXs
{
    public class SpellGFX : BaseSpellGFX<ESpellEvent>
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
        protected virtual void CheckEnd(ESpellEvent spellEvent)
        {
            if (m_PrefabSpawn.GFXLifetime.EndSpellPart == ESpellEvent.None || m_PrefabSpawn.GFXLifetime.EndSpellPart > spellEvent)
                return;

            End();
        }

        #endregion


        #region Duration

        protected override void CalculateDuration()
        {
            if (m_Duration != 0)
                return;

            m_Duration = Mathf.Max(0, m_PrefabSpawn.GFXLifetime.Persistance);

            if (IsGFXAlive(ESpellEvent.OnStartCast))
                m_Duration += CalculateCastTime();

            if (IsGFXAlive(ESpellEvent.OnCast))
                m_Duration += m_SpellData.Delay;

            if (m_PrefabSpawn.GFXLifetime.EndAt > 0)
                m_Duration *= m_PrefabSpawn.GFXLifetime.EndAt;
        }

        /// <summary>
        /// Check if SpellGFX is alive at the provided time
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <returns></returns>
        bool IsGFXAlive(ESpellEvent spellEvent)
        {
            return m_PrefabSpawn.GFXLifetime.StartSpellPart <= spellEvent && spellEvent < m_PrefabSpawn.GFXLifetime.EndSpellPart;
        }

        /// <summary>
        /// Anticipate how much time the spell will take to be casted
        /// </summary>
        /// <returns></returns>
        protected override float CalculateCastTime()
        {
            return m_SpellData.AnimationTimer / m_Controller.SpellHandler.GetCastSpeed(m_SpellData.Spell.ToString());
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            // Only register to SpellHandler events if this was spawn BEFORE casting
            if (m_PrefabSpawn.GFXLifetime.StartSpellPart < ESpellEvent.OnSpawn)
                m_Controller.SpellHandler.OnPreSpellEvent += OnPreSpellEvent;

            // if starts before spawn and end after spawns : link to static OnSpellSpawnEvent
            if (m_PrefabSpawn.GFXLifetime.StartSpellPart < ESpellEvent.OnSpawn && m_PrefabSpawn.GFXLifetime.EndSpellPart >= ESpellEvent.OnSpawn)
                Spell.OnSpellSpawn += OnSpellSpawn;

            if (m_Spell != null)
                m_Spell.OnSpellEvent += OnSpellEvent;

            if (m_StateEffectName != null)
                m_Controller.StateHandler.StateEffectEvent += OnStateEffectEvent;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_Controller != null)
                m_Controller.SpellHandler.OnPreSpellEvent -= OnPreSpellEvent;

            if (m_Spell != null)
                m_Spell.OnSpellEvent -= OnSpellEvent;

            if (m_StateEffectName != null)
                m_Controller.StateHandler.StateEffectEvent -= OnStateEffectEvent;

            Spell.OnSpellSpawn -= OnSpellSpawn;
        }

        protected virtual void OnSpellSpawn(Spell spell)
        {
            if (m_SpellData == null)
            {
                ErrorHandler.Warning("SpellGFX registered to OnSpellSpawn() event but has no SpellData");
                return;
            }

            if (m_Controller == null)
            {
                ErrorHandler.Warning("SpellGFX registered to OnSpellSpawn() event but has no Controller");
                return;
            }

            // check if is same name and same player
            if (m_SpellData.Name != spell.SpellData.Name || m_Controller.PlayerId != spell.Controller.PlayerId)
                return;

            // register to events of the provided spell
            m_Spell = spell;
            m_Spell.OnSpellEvent += OnSpellEvent;

            // unregister this listener
            Spell.OnSpellSpawn -= OnSpellSpawn;
        }

        protected virtual void OnPreSpellEvent(string spellName, ESpellEvent spellEvent)
        {
            if (spellName != m_SpellData.Name)
                return;

            if (spellEvent == ESpellEvent.OnCancelCast)
            {
                End();
                return;
            }    

            CheckEnd(spellEvent);
        }

        void OnSpellEvent(ESpellEvent spellEvent)
        {
            // TODO : REMOVE    =========================================================================
            Debug.Log(m_SpellData.name + " : " + spellEvent.ToString());
            // TODO : REMOVE    =========================================================================

            CheckEnd(spellEvent);
        }

        void OnStateEffectEvent(ESpellEvent spellEvent, string stateEffectName)
        {
            if (m_StateEffectName != stateEffectName)
                return;

            CheckEnd(spellEvent);
        }

        #endregion
    }
}