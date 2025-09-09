using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Hammered", menuName = "Game/StateEffects/SpecialEffects/Hammered")]
    public class Hammered : StateEffect
    {
        #region Members


        #endregion


        #region Init & End

        protected override void OnStart()
        {
            base.OnStart();
        }

        public override void End()
        {
            // set stacks to 0
            m_Stacks = 0;

            // this effect does not end, it deactivates / activates
            Deactivate();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Controller.Life.OnHittedEvent += OnHit;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Controller.Life.OnHittedEvent -= OnHit;
        }

        void OnHit(int damage, ulong casterId, EHitCategory spellCategory)
        {
            if (spellCategory != EHitCategory.Direct)
                return;

            if (m_Stacks == 0)
                Activate();

            Refresh(stacks: 1);
        }

        #endregion
    }
}