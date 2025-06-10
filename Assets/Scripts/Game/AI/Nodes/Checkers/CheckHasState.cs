using Enums;
using Game;
using Game.Spells;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Tools.Helpers;

namespace AI
{
    public class CheckHasState : BaseChecker
    {
        #region Init & End

        List<string> m_StateEffects;
        int m_NStacks;
        ESpellTarget m_SpellTarget;

        public CheckHasState(Controller controller, string state, int nStacks = 1, ESpellTarget target = ESpellTarget.Self, bool isReversed = false) : base(controller, isReversed) 
        {
            m_StateEffects = new List<string> { state };
            m_NStacks = nStacks;
            m_SpellTarget = target;
        }

        public CheckHasState(Controller controller, List<string> states, int nStacks = 1, ESpellTarget target = ESpellTarget.Self, bool isReversed = false) : base(controller, isReversed) 
        {
            m_StateEffects = states;
            m_NStacks = nStacks;
            m_SpellTarget = target;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            var controllerToCheck = TargetHelper.GetTargetController(m_Controller.PlayerId, m_SpellTarget);
            foreach (var stateEffect in m_StateEffects)
            {
                if (controllerToCheck.StateHandler.GetStacks(stateEffect, checkActivated: true) >= m_NStacks)
                {
                    SetNodeState(NodeState.SUCCESS);
                    ErrorHandler.Log("CheckHasState(" + m_StateEffects[0] + ")" + (m_IsReversed ? " REVERSERD" : "") + " : " + m_State, ELogTag.AIFinalDecision);
                    return m_State;
                }
            }

            SetNodeState(NodeState.FAILURE);
            ErrorHandler.Log("CheckHasState(" + m_StateEffects[0] + ")" + (m_IsReversed ? " REVERSERD" : "") + " : " + m_State, ELogTag.AIFinalDecision);
            return m_State;
        }

        #endregion
    }
}