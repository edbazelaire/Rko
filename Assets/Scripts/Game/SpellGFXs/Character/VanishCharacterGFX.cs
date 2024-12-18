using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.SpellGFXs
{
    public class VanishCharacterGFX : SpellGFX
    {
        #region Members



        #endregion


        #region Init

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();

            m_Controller.GFXHandler.HideCharacter(true);
        }

        protected virtual void RegisterAnimations() { }

        #endregion


        #region End

        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_Controller.GFXHandler.HideCharacter(false);
        }

        #endregion


        #region Update

        protected virtual void Update() { }

        #endregion
        
    }
}