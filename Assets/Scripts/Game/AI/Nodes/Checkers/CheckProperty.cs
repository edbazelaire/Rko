using Enums;
using Game;
using System.Collections.Generic;
using Tools;

namespace AI
{
    public class CheckProperty : BaseChecker
    {
        #region Init & End

        EStateEffectProperty    m_StateEffectProperty;
        float                   m_Threshold;
        bool                    m_IsPerc;
        string                  m_Relation;

        public CheckProperty(Controller controller, EStateEffectProperty stateEffectProperty, float value, bool isPerc = false, string relation = ">=") : base(controller) 
        {
            m_StateEffectProperty   = stateEffectProperty;
            m_Threshold             = value;
            m_IsPerc                = isPerc;
            m_Relation              = relation;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            bool test = false;
            float valueToCheck;
            switch (m_StateEffectProperty)
            {
                case EStateEffectProperty.Hp:
                    valueToCheck = m_Controller.Life.Hp.Value;
                    if (m_IsPerc)
                        valueToCheck /= m_Controller.Life.MaxHp.Value;
                    break;

                case EStateEffectProperty.Energy:
                    valueToCheck = m_Controller.EnergyHandler.Energy.Value;
                    if (m_IsPerc) 
                        valueToCheck /= m_Controller.EnergyHandler.MaxEnergy.Value;
                    break;

                case EStateEffectProperty.Shield:
                    valueToCheck = m_Controller.Life.FinalShield.Value;
                    if (m_IsPerc)
                        ErrorHandler.Error("Unhandled [Percentage] check for property : " + m_StateEffectProperty);
                    break;

                default:
                    ErrorHandler.Error("Unhandled property : " + m_StateEffectProperty);
                    m_State = NodeState.FAILURE;
                    return m_State;
            }

            switch (m_Relation)
            {
                case "<":
                    test = valueToCheck < m_Threshold;
                    break;

                case "<=":
                    test = valueToCheck <= m_Threshold;
                    break;

                case ">":
                    test = valueToCheck > m_Threshold;
                    break;

                case ">=":
                    test = valueToCheck >= m_Threshold;
                    break;

                default:
                    ErrorHandler.Error("Unhandled relation type : " + m_Relation);
                    break;

            }

            m_State = test ? NodeState.SUCCESS : NodeState.FAILURE;
            return m_State;
        }

        #endregion
    }
}