using AI;
using Enums;
using Game.Loaders;
using System.Collections.Generic;
using Tools;

public class TaskJump : BaseTask
{
    #region Members

    List<ESpell> m_JumpSpells = new List<ESpell>();  

    #endregion


    #region Init & End

    public TaskJump(Controller controller) : base(controller) 
    { 
        foreach (ESpell spell in m_Controller.SpellHandler.Spells) 
        { 
            if (SpellLoader.GetSpellData(spell).SpellType == ESpellType.Jump)
            {
                m_JumpSpells.Add(spell); 
            }
        }

        ErrorHandler.Log("Found " + m_JumpSpells.Count + " jump spells", ELogTag.AITaskJump);
    }

    #endregion


    #region Evaluate
    public override NodeState Evaluate()
    {
        m_State = NodeState.FAILURE;

        if (m_JumpSpells.Count == 0)
        {
            ErrorHandler.Log("TaskJump FAILURE : No jump spells", ELogTag.AITaskJump);
            return m_State;
        }

        foreach (ESpell spell in m_JumpSpells)
        {
            if (m_Controller.SpellHandler.CanCast(spell))
            {
                ErrorHandler.Log("TaskJump SUCCESS", ELogTag.AITaskJump);
                m_State = NodeState.SUCCESS;
                m_Controller.SpellHandler.TryStartCastSpell(spell);
                return m_State;
            }
        }

        ErrorHandler.Log("TaskJump FAILURE : all jump spells in cooldown", ELogTag.AITaskJump);
        return m_State;
    }

    #endregion

}
