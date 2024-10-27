using Tools;
using UnityEngine;

namespace Game.Spells
{
    public class PreviewFading : PreviewTimer
    {
        #region Members

        GameObject  m_FadingArea;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_FadingArea = Finder.Find(gameObject, "FadingArea");
        }

        #endregion


        #region Animation Methods

        protected override void OnAnimationStarting()
        {
            base.OnAnimationStarting();
        }


        #endregion
    }
}