using System.Collections;
using Tools;
using UnityEngine;

namespace AI
{
    public class CheckImmediatThreat : BaseChecker
    {
        #region Members

        #endregion


        #region Init & End

        public CheckImmediatThreat(Controller controller) : base(controller) { }


        #endregion


        #region Evaluation

        public override NodeState Evaluate()
        {
            base.Evaluate();

            if (m_State != NodeState.FAILURE)
                return m_State;

            // checks if is on a spell preview
            if (m_ImmediatThreatTrigger.CheckTriggerSpellSpawn(0.2f))
            {
                ErrorHandler.Log("Checker ZONE detected", Enums.ELogTag.AI);
                m_State = NodeState.SUCCESS;
                return m_State;
            }

            // checks that is not in the trajectory of projectile
            if (m_ImmediatThreatTrigger.CheckTriggerProjectile(true))
            {
                ErrorHandler.Log("Checker PROJECTILE detected", Enums.ELogTag.AI);
                m_State = NodeState.SUCCESS;
                return m_State;
            }

            return m_State;
        }

        #endregion
    }
}