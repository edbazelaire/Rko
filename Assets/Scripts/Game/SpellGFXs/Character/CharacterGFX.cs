using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.SpellGFXs
{
    public class CharacterGFX : SpellGFX
    {
        #region Members



        #endregion


        #region Init

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();
        }

        protected virtual void RegisterAnimations() { }

        #endregion


        #region End

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        #endregion


        #region Update

        protected virtual void Update() { }

        #endregion
    }
}