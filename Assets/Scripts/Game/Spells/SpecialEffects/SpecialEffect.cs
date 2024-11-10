using Tools;
using UnityEngine;

namespace Game.Spells.SpecialEffects
{
    public class SpecialEffect : MObject
    {
        #region Members

        protected int m_Level;

        #endregion


        #region Init & End

        public virtual void Initialize(int level)
        {
            m_Level = level;

            base.Initialize();
        }

        #endregion


        #region Listeners


        #endregion
    }
}