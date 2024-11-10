using Data;
using Enums;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    public class Buff : Spell
    {
        #region Members

        BuffData m_SpellData => m_BaseSpellData as BuffData;

        #endregion


        #region Init & End

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="spellName"></param>
        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level)
        {
            base.Initialize(clientId, target, spellName, level);

            if (!IsServer)
                return;

            OnHit(GetTargetController());
            End();
        }

        #endregion

        #region Target & Position

        protected override Controller GetTargetController()
        {
            if (! SpellData.IsAutoTarget)
                ErrorHandler.Warning("Buff spell " + SpellData.Name + " is not AutoTarget. This is currently not handled");

            switch (SpellData.SpellTarget)
            {
                case ESpellTarget.Self:
                    return m_Controller;

                case ESpellTarget.FirstEnemy:
                    return GameManager.Instance.GetFirstEnemy(m_Controller.Team);

                default:
                    ErrorHandler.Error("Unhandled case : " + SpellData.SpellTarget + " for BUFF spell " + SpellData.Name);
                    return m_Controller;
            }
        }

        #endregion


        #region Hitting

        protected override void OnHit(Controller controller) 
        {
            // add state effect specific to this spell (must have same name)
            controller.StateHandler.AddStateEffect(m_SpellData.GetStateEffect(), m_Controller);

            base.OnHit(controller);
        }

        #endregion
    }
}