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

        CanvasScaler m_CanvasScaler;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_CanvasScaler = Finder.FindComponent<CanvasScaler>(gameObject);
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

            //if (m_CanvasScaler == null)
            //{
            //    ErrorHandler.Error("Trying to rescale background without CanvasScaler component");
            //    return;
            //}
            //m_CanvasScaler.scaleFactor = scale;

            transform.localScale = Vector3.one * scale;
        }

        #endregion
    }
}
