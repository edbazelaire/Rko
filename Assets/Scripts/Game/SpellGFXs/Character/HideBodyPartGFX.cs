using Enums;


namespace Game.SpellGFXs
{
    public class HideBodyPartGFX : SpellGFX
    {
        #region Members



        #endregion


        #region Init

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();

            if (m_BodyPart != EBodyPart.None)
                m_Controller.GFXHandler.Hide(true, m_BodyPart.ToString());
            else 
                m_Controller.GFXHandler.HideCharacter(true);
        }

        protected virtual void RegisterAnimations() { }

        #endregion


        #region End

        protected override void ForceEnd()
        {
            if (m_BodyPart != EBodyPart.None)
                m_Controller.GFXHandler.Hide(false, m_BodyPart.ToString());
            else
                m_Controller.GFXHandler.HideCharacter(false);

            base.ForceEnd();
        }

        #endregion


        #region Update

        protected virtual void Update() { }

        #endregion
        
    }
}