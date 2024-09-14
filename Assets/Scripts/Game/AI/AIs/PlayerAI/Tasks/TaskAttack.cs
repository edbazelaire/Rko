using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AI;
using Data;
using Enums;
using Game;
using Game.Character;
using Game.Loaders;
using Tools;
using UnityEngine;

enum ESpellCategory
{
    Ultimate,
    Heal,
    Buff,
    ConsumeStateEffect,
    Damage,
    AutoAttack,
}

public class TaskAttack : Node
{
    #region Members

    const float ATTACK_COOLDOWN = 0.2f;

    Controller m_Controller;
    SpellHandler m_SpellHandler => m_Controller.SpellHandler;

    bool m_CanAttack = true;
    Dictionary<ESpellCategory, List<ESpell>> m_SpellCategories = new Dictionary<ESpellCategory, List<ESpell>>();
    Dictionary<ESpell, List<EStateEffect>> m_ConsumSpells = new Dictionary<ESpell, List<EStateEffect>>();

    #endregion


    #region Init & End

    public TaskAttack(Controller controller)
    {
        m_Controller = controller;

        FilterSpells();
    }

    void FilterSpells()
    {
        m_SpellCategories[ESpellCategory.Ultimate]              = new List<ESpell>() { m_SpellHandler.Ultimate };
        m_SpellCategories[ESpellCategory.Heal]                  = FilterSpellsByProperty(ESpellProperty.Heal);
        m_SpellCategories[ESpellCategory.Buff]                  = FilterSpellsByType(ESpellType.Buff);
        m_SpellCategories[ESpellCategory.ConsumeStateEffect]    = FilterSpellsWithConsumeStateEffect();
        m_SpellCategories[ESpellCategory.Damage]                = FilterSpellsByProperty(ESpellProperty.Damages);
        m_SpellCategories[ESpellCategory.AutoAttack]            = new List<ESpell>() { m_SpellHandler.AutoAttack };
    }

    #endregion


    public override NodeState Evaluate()
    {
        if (! m_CanAttack)
        {
            m_State = NodeState.FAILURE;
            return m_State;
        }

        ErrorHandler.Log("===================================================================", ELogTag.AITaskAttack);
        ErrorHandler.Log("TaskAttack.Evaluate()", ELogTag.AITaskAttack);
        if (m_Controller.SpellHandler.IsCasting)
        {
            ErrorHandler.Log("     + IsCasting     : true",     ELogTag.AITaskAttack);
            ErrorHandler.Log("     + State         : RUNNING",  ELogTag.AITaskAttack);

            m_State = NodeState.RUNNING;
            return m_State;
        }

        // check that no state is blocking the cast
        if (m_Controller.SpellHandler.HasStateBlockingCast())
        {
            ErrorHandler.Log("     + HasStateBlockingCast  : true",     ELogTag.AITaskAttack);
            ErrorHandler.Log("     + State                 : FAILURE",  ELogTag.AITaskAttack);

            m_State = NodeState.FAILURE;
            return m_State;
        }

        // check if any spell can be casted
        ESpell spell = CheckSpellToSelect();
        if (spell != ESpell.Count)
        {
            m_Controller.SpellHandler.TryStartCastSpell(spell);
                
            ErrorHandler.Log("     + Casting Spell     : " + spell, ELogTag.AITaskAttack);
            ErrorHandler.Log("     + State             : SUCCESS",  ELogTag.AITaskAttack);

            ErrorHandler.Log("TaskAttack() : Casting Spell - " + spell, ELogTag.AIFinalDecision);

            m_State = NodeState.SUCCESS;
            m_Controller.StartCoroutine(TimerAttackCoroutine());
            return m_State;
        }

        // action failed, no spell can be used
        ErrorHandler.Log("     + no spell avaliable", ELogTag.AI);
        ErrorHandler.Log("     + State                 : FAILURE", ELogTag.AITaskAttack);

        m_State = NodeState.FAILURE;
        return m_State;
    }


    #region Spell Selection

    /// <summary>
    /// Check all spells to decide which is more adequate to the situation
    /// </summary>
    /// <returns></returns>
    ESpell CheckSpellToSelect()
    {
        // init spell
        ESpell spell = ESpell.Count;

        // check : ULTIMATE
        CheckUltimate(ref spell);

        // check : HEAL
        CheckHealingSpells(ref spell);

        // check : Buffs
        CheckBuffs(ref spell);
      
        // check : IronSkin
        CheckStateEffectsConsum(ref spell);
      
        // check : Damages
        CheckDamageSpells(ref spell);

        return spell;
    }

    void CheckSpells(ref ESpell spell, List<ESpell> spellsList)
    {
        foreach (ESpell tempSpell in spellsList)
        {
            if (!m_SpellHandler.CanCast(tempSpell))
                continue;

            spell = tempSpell;
            return;
        }
    }

    /// <summary>
    /// Check if should use an Ultimate
    /// </summary>
    /// <param name="spell"></param>
    void CheckUltimate(ref ESpell spell)
    {
        // skip if a spell was already selected
        if (spell != ESpell.Count)
            return;

        ErrorHandler.Log("CheckUltimate()", ELogTag.AITaskAttack);

        // check : ULTIMATE
        if (m_SpellHandler.CanCast(m_SpellHandler.Ultimate))
            spell = m_SpellHandler.Ultimate;
    }

    /// <summary>
    /// Check if should use a Heal spell
    /// </summary>
    /// <param name="spell"></param>
    void CheckHealingSpells(ref ESpell spell)
    {
        // skip if a spell was already selected
        if (spell != ESpell.Count)
            return;

        ErrorHandler.Log("CheckHealingSpells()", ELogTag.AITaskAttack);

        if (m_Controller.Life.Hp.Value == m_Controller.Life.MaxHp.Value)
            return;

        CheckSpells(ref spell, m_SpellCategories[ESpellCategory.Heal]);
    }

    /// <summary>
    /// Check if should use a Buff spell
    /// </summary>
    /// <param name="spell"></param>
    void CheckBuffs(ref ESpell spell)
    {
        // skip if a spell was already selected
        if (spell != ESpell.Count)
            return;

        ErrorHandler.Log("CheckBuffs()", ELogTag.AITaskAttack);

        CheckSpells(ref spell, m_SpellCategories[ESpellCategory.Buff]);
    }

    /// <summary>
    /// Check if should use a spell that consumes state effects
    /// </summary>
    /// <param name="spell"></param>
    void CheckStateEffectsConsum(ref ESpell spell)
    {
        // skip if a spell was already selected
        if (spell != ESpell.Count)
            return;

        ErrorHandler.Log("CheckStateEffectsConsum()", ELogTag.AITaskAttack);

        foreach (var item in m_ConsumSpells)
        {
            bool hasState = false;
            foreach (var state in item.Value)
            {
                if (GameManager.Instance.GetFirstEnemy(m_Controller.Team).StateHandler.HasState(state))
                {
                    hasState = true;
                    break;
                }
            }

            if (!hasState)
                continue;

            if (! m_SpellHandler.CanCast(item.Key))
                continue;

            spell = item.Key;
            return;
        }
    }

    /// <summary>
    /// Check if should use damage spell
    /// </summary>
    /// <param name="spell"></param>
    void CheckDamageSpells(ref ESpell spell)
    {
        // skip if a spell was already selected
        if (spell != ESpell.Count)
            return;

        ErrorHandler.Log("CheckDamageSpells()", ELogTag.AITaskAttack);

        CheckSpells(ref spell, m_SpellCategories[ESpellCategory.Damage]);
    }

    #endregion


    #region Helpers

    /// <summary>
    /// List spells in descending order on a specific property
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    List<ESpell> FilterSpellsByProperty(ESpellProperty property, List<ESpellType> excludedTypes = null)
    {
        // by default exculte counters and jumps
        if (excludedTypes == null)
            excludedTypes = new List<ESpellType>() { ESpellType.Jump, ESpellType.Counter };

        List<(ESpell Spell, float Value)> spells = new();

        for (int i = 0; i < m_SpellHandler.Spells.Count; i++)
        {
            ESpell spell = m_SpellHandler.Spells[i];

            if (spell == m_SpellHandler.AutoAttack || spell == m_SpellHandler.Ultimate)
                continue;

            if (m_ConsumSpells.Keys.Contains(spell))
                continue;

            SpellData spellData = SpellLoader.GetSpellData(spell, m_SpellHandler.SpellLevels[i]);

            // check if is not allowed types
            if (excludedTypes.Contains(spellData.SpellType))
                continue;

            // TODO : BETTER
            var spellInfos = spellData.GetInfos();
            // try get value
            if (! spellInfos.ContainsKey(property.ToString()) || ! float.TryParse(spellInfos[property.ToString()].ToString(), out float value))
                continue;
            // TODO : BETTER

            // filter by damages
            if (value > 0)
            {
                int j = 0;
                for (j = 0; j < spells.Count; j++)
                {
                    if (spells[j].Value < value)
                        break;
                }

                spells.Insert(j, (spell, value));
            }
        }

        return spells.Select(t => t.Spell).ToList();
    }

    /// <summary> 
    /// List spells in descending order on a specific property
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    List<ESpell> FilterSpellsWithConsumeStateEffect()
    {
        List<ESpell> spells = new();

        for (int i = 0; i < m_SpellHandler.Spells.Count; i++)
        {
            ESpell spell = m_SpellHandler.Spells[i];
            SpellData spellData = SpellLoader.GetSpellData(spell);

            var stateEffects = GetConsumeStateEffects(spellData);

            // if does, add to consume state effect category
            if (stateEffects.Count > 0)
            {
                spells.Add(spell);
                m_ConsumSpells.Add(spell, stateEffects);
            }
        }

        return spells;
    }

    /// <summary>
    /// Get all consume state effects of a spell (recursive check of OnHit spells)
    /// </summary>
    /// <param name="spellData"></param>
    /// <returns></returns>
    List<EStateEffect> GetConsumeStateEffects(SpellData spellData) 
    {
        // check if has any state that consumes the state effect
        var stateEffects = new List<EStateEffect>();
        foreach (var stateEffectData in spellData.EnemyStateEffects)
        {
            var consumState = SpellLoader.GetStateEffect(stateEffectData.StateEffect.ToString()).ConsumeState;
            if (consumState != EStateEffect.None)
            {
                stateEffects.Add(consumState);
            }
        }

        foreach (SpellData onHit in spellData.OnHit)
        {
            stateEffects.Concat(GetConsumeStateEffects(onHit));
        }

        return stateEffects;
    }

    /// <summary>
    /// List spells in descending order on a specific property
    /// </summary>
    /// <param name="property"></param>
    /// <returns></returns>
    List<ESpell> FilterSpellsByType(ESpellType spellType)
    {
        List<ESpell> spells = new();

        for (int i = 0; i < m_SpellHandler.Spells.Count; i++)
        {
            ESpell spell = m_SpellHandler.Spells[i];
            SpellData spellData = SpellLoader.GetSpellData(spell);

            if (spellData.SpellType == spellType)
            {
                spells.Add(spell);
            }
        }

        return spells;
    }

    /// <summary>
    /// Set a fake cooldown to slow attacks
    /// </summary>
    /// <returns></returns>
    IEnumerator TimerAttackCoroutine()
    {
        m_CanAttack = false;

        float timer = ATTACK_COOLDOWN;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        m_CanAttack = true;
    }

    #endregion
}
