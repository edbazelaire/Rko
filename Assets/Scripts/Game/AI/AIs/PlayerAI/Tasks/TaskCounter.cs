using AI;
using Enums;
using Game.Loaders;
using System.Collections.Generic;
using Tools;

public class TaskCounter : BaseNode
{
    #region Members

    List<ESpell> m_CounterSpells = new List<ESpell>();

    #endregion


    #region Init & End

    public TaskCounter(Controller controller) : base(controller) 
    {
        foreach (ESpell spell in m_Controller.SpellHandler.Spells) 
        { 
            if (SpellLoader.GetSpellData(spell).SpellType == ESpellType.Counter)
            {
                m_CounterSpells.Add(spell); 
            }
        }

        ErrorHandler.Log("Found " + m_CounterSpells.Count + " counter spells", ELogTag.AITaskCounter);
    }

    #endregion


    #region Evaluate
    public override NodeState Evaluate()
    {
        m_State = NodeState.FAILURE;

        if (m_ImmediatThreatTrigger.CheckTriggerSpellSpawn())
        {
            ErrorHandler.Log("TaskCounter : FAILURE (in spell spawn)", ELogTag.AITaskCounter);
            return m_State;
        }

        if (m_ImmediatThreatTrigger.CheckTriggerZone())
        {
            ErrorHandler.Log("TaskCounter : FAILURE (in spell spawn)", ELogTag.AITaskCounter);
            return m_State;
        }

        foreach (ESpell spell in m_CounterSpells)
        {
            if (m_Controller.SpellHandler.CanCast(spell))
            {
                ErrorHandler.Log("TaskCounter : SUCCESS", ELogTag.AITaskCounter);
                m_State = NodeState.SUCCESS;
                m_Controller.SpellHandler.TryStartCastSpell(spell);
                return m_State;
            }
        }

        ErrorHandler.Log("TaskCounter FAILURE : all spells are in cooldown", ELogTag.AITaskCounter);
        return m_State;
    }

    #endregion

}
