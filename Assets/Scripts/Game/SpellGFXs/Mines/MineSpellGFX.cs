using Data;
using Game.Spells;
using Tools;



namespace Game.SpellGFXs
{
    public class MineSpellGFX : BaseSpellGFX<EMineState>
    {
        #region Members

        protected Mine m_MineSpell => (Mine)m_Spell;
        public MineData SpellData => (MineData)m_MineSpell.SpellData;

        #endregion


        #region Init
        
        #endregion


        #region End

        /// <summary>
        /// Check if the graphics should end
        /// </summary>
        /// <param name="spellEvent"></param>
        protected virtual void CheckEnd(EMineState spellEvent)
        {
            if (m_PrefabSpawn.GFXLifetime.EndSpellPart == EMineState.None || m_PrefabSpawn.GFXLifetime.EndSpellPart > spellEvent)
                return;

            End();
        }

        #endregion


        #region Duration

        protected override void CalculateDuration(float? forcedDuration = null)
        {
            base.CalculateDuration(forcedDuration);
            if (forcedDuration != null)
                return;

            if (IsGFXAlive(EMineState.Inactive))
                m_Duration += SpellData.InactiveTimer;

            if (IsGFXAlive(EMineState.Trigerred))
                m_Duration += SpellData.TrigerredTimer;

            if (IsGFXAlive(EMineState.Activated))
                m_Duration += SpellData.ActivateTimer;

            // multiply by end percentage
            if (m_PrefabSpawn.GFXLifetime.EndAt > 0)
                m_Duration *= m_PrefabSpawn.GFXLifetime.EndAt;

            // add Persistance
            m_Duration += m_PrefabSpawn.GFXLifetime.Persistance;
        }

        /// <summary>
        /// Check if SpellGFX is alive at the provided time
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <returns></returns>
        bool IsGFXAlive(EMineState mineState)
        {
            return m_PrefabSpawn.GFXLifetime.StartSpellPart <= mineState && mineState < m_PrefabSpawn.GFXLifetime.EndSpellPart;
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            if (m_Spell == null)
            {
                ErrorHandler.Error("Unable to find spell for MineSpellGFX");
                return;
            }

            m_MineSpell.State.OnValueChanged += OnMineStateChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_Spell == null)
                return;

            m_MineSpell.State.OnValueChanged -= OnMineStateChanged;
        }


        void OnMineStateChanged(EMineState previousState, EMineState newState)
        {
            CheckEnd(newState);
        }

        #endregion
    }
}