using Enums;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.SpellGFXs
{
    public class CastIceRockGFX : AnimationQueueGFX
    {
        #region Members

        SpriteRenderer m_IceRock;

        #endregion



        #region Init & End

        protected override void FindComponents()
        {
            m_IceRock = Finder.FindComponent<SpriteRenderer>(gameObject, "IceRock");

            m_IceRock.gameObject.SetActive(false);
        }

        protected override void RegisterAnimations()
        {
            base.RegisterAnimations();

            m_AnimationQueue.Enqueue(PickUpRock());
        }

        //protected override void OnDestroy()
        //{
        //    base.OnDestroy();

        //    Destroy(m_IceRock);
        //}

        #endregion


        #region Animations

        IEnumerator PickUpRock()
        {
            m_IceRock.gameObject.SetActive(true);

            while (m_Timer > m_Duration * 0.6f) 
                yield return null;

           transform.parent = m_Controller.GFXHandler.GetBodyPart(EBodyPart.R_Hand).transform;
        }


        #endregion
    }
}