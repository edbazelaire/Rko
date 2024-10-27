using Enums;
using Game;
using Game.Spells;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace AI
{
    public class CheckHasState : BaseChecker
    {
        #region Init & End

        List<string> m_StateEffects;
        int m_NStacks;
        ESpellTarget m_SpellTarget;

        public CheckHasState(Controller controller, string state, int nStacks = 1, ESpellTarget target = ESpellTarget.Self) : base(controller) 
        {
            m_StateEffects = new List<string> { state };
            m_NStacks = nStacks;
            m_SpellTarget = target;
        }

        public CheckHasState(Controller controller, List<string> states, int nStacks = 1, ESpellTarget target = ESpellTarget.Self) : base(controller) 
        {
            m_StateEffects = states;
            m_NStacks = nStacks;
            m_SpellTarget = target;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = NodeState.FAILURE;
            var controllerToCheck = m_SpellTarget == ESpellTarget.Self ? m_Controller : GameManager.Instance.GetFirstEnemy(m_Controller.Team);
            
            foreach (var stateEffect in m_StateEffects)
            {
                if (controllerToCheck.StateHandler.GetStacks(stateEffect) >= m_NStacks)
                {
                    m_State = NodeState.SUCCESS;
                    return m_State;
                }
            }

            return m_State;
        }

        #endregion
    }
}