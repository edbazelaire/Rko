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

        protected override void SetPersistantTimer()
        {
            m_PersistanceTimer = m_SpellData.Delay;
        }

        #endregion


        #region Post Processing

        protected override void ApplyPostProcessing() 
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            m_MovementStarted = true;
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