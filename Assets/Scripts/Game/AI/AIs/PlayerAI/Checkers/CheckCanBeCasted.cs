using Enums;
using Game;
using System.Collections.Generic;

namespace AI
{
    public class CheckCanBeCasted : BaseChecker
    {
        #region Init & End

        ESpell m_Spell;

        public CheckCanBeCasted(Controller controller, ESpell spell) : base(controller) 
        {
            m_Spell = spell;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            if (! m_Controller.SpellHandler.Spells.Contains(m_Spell))
            {
                m_State = NodeState.FAILURE;
                return m_State;
            }

            m_State = m_Controller.SpellHandler.CanCast(m_Spell) ? NodeState.SUCCESS : NodeState.FAILURE;
            return m_State;
        }

        #endregion
    }
}