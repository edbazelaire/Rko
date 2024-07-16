using System.Collections;
using UnityEngine;

namespace Game.SpellGFXs
{
    public class HightCastGFX : SpellGFX
    {
        #region Members

        [SerializeField] protected float m_Speed = 10f;

        protected bool m_MovementStarted = false;

        #endregion


        #region End

        public override void End()
        {
            // start moving towards the target
            m_MovementStarted = true;

            base.End();
        }

        protected override void SetPersistantTimer()
        {
            m_PersistanceTimer = m_SpellData.Delay;
        }

        #endregion


        #region Movement

        void Update()
        {
            if (m_MovementStarted)
                transform.Translate(m_Speed * Time.deltaTime, 0, 0);
        }

        #endregion
    }
}