using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Game.SpellGFXs
{
    public class SoulSiffonGFX : CollectionGFX
    {
        #region Members

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Target = GameManager.Instance.GetFirstEnemy(m_Controller.Team);
        }

        #endregion


        #region Animations


        #endregion
    }       
}
