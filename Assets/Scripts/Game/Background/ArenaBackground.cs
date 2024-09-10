using Enums;
using Network;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Background
{
    public class ArenaBackground : MObject
    {
        #region Members

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
        }

        #endregion


        #region Background Management

        public void Rescale(float scale)
        {
            if (scale <= 0)
            {
                ErrorHandler.Error("Bad scale provided : " + scale);
                return;
            }

            transform.localScale = Vector3.one * scale;
        }

        #endregion
    }
}
