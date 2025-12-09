using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AI;
using Data;
using Data.DataStructures.SpellSubStructures;
using Enums;
using Game;
using Game.Character;
using Game.Loaders;
using MyBox;
using Tools;
using Unity.Loading;
using UnityEngine;

public class TaskRandomAttack : BaseTask
{
    #region Members

    SpellHandler m_SpellHandler => m_Controller.SpellHandler;

    #endregion


    #region Init & End

    public TaskRandomAttack(Controller controller, Func<float> weight = null) : base(controller, weight)
    {

    }

    #endregion


    public override NodeState Evaluate()
    {
        ErrorHandler.Log(() => "===================================================================", ELogTag.AITaskRandomAttack);
        ErrorHandler.Log(() => "TaskRandomAttack.Evaluate()", ELogTag.AITaskRandomAttack);
        if (m_Controller.SpellHandler.IsCasting)
        {
            ErrorHandler.Log(() => "     + IsCasting     : true",     ELogTag.AITaskRandomAttack);
            ErrorHandler.Log(() => "     + State         : RUNNING",  ELogTag.AITaskRandomAttack);

            m_State = NodeState.RUNNING;
            return m_State;
        }

        // check that no state is blocking the cast
        if (! m_Controller.StateHandler.CanCast)
        {
            ErrorHandler.Log(() => "     + HasStateBlockingCast  : true",     ELogTag.AITaskRandomAttack);
            ErrorHandler.Log(() => "     + State                 : FAILURE",  ELogTag.AITaskRandomAttack);

            m_State = NodeState.FAILURE;
            return m_State;
        }

        // check if any spell can be casted
        ESpell spell = SelectSpell();
        if (spell != ESpell.None)
        {
            m_Controller.SpellHandler.TryStartCastSpell(spell, m_Controller.CharacterLevel, out string _);
                
            ErrorHandler.Log(() => "     + Casting Spell     : " + spell, ELogTag.AITaskRandomAttack);
            ErrorHandler.Log(() => "     + State             : SUCCESS",  ELogTag.AITaskRandomAttack);

            ErrorHandler.Log(() => "TaskRandomAttack() : Casting Spell - " + spell, ELogTag.AIFinalDecision);

            m_State = NodeState.SUCCESS;
            return m_State;
        }

        // action failed, no spell can be used
        ErrorHandler.Log(() => "     + no spell avaliable", ELogTag.AI);
        ErrorHandler.Log(() => "     + State                 : FAILURE", ELogTag.AITaskRandomAttack);

        m_State = NodeState.FAILURE;
        return m_State;
    }


    #region Spell Selection

    /// <summary>
    /// Check all spells to decide which is more adequate to the situation
    /// </summary>
    /// <returns></returns>
    ESpell SelectSpell()
    {
        // init spell
        ESpell spell = ESpell.None;

        // check : ULTIMATE
        CheckUltimate(ref spell);

        // cast random attack
        CheckRandomAttack(ref spell);

        return spell;
    }

    /// <summary>
    /// Check if should use an Ultimate
    /// </summary>
    /// <param name="spell"></param>
    void CheckUltimate(ref ESpell spell)
    {
        // skip if a spell was already selected
        if (spell != ESpell.None)
            return;

        ErrorHandler.Log(() => "CheckUltimate()", ELogTag.AITaskRandomAttack);

        // check : ULTIMATE
        if (m_SpellHandler.CanCast(m_SpellHandler.Ultimate))
            spell = m_SpellHandler.Ultimate;
    }

    /// <summary>
    /// Check all spells in random order and cast first available
    /// </summary>
    /// <param name="spell"></param>
    /// <param name="spellsList"></param>
    void CheckRandomAttack(ref ESpell spell)
    {
        var spells = m_SpellHandler.Spells.ShuffleClone();
        foreach (ESpell tempSpell in spells)
        {
            if (tempSpell == m_SpellHandler.Ultimate)
                continue;

            if (!m_SpellHandler.CanCast(tempSpell))
                continue;

            spell = tempSpell;
            return;
        }
    }

    #endregion
}
