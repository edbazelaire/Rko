using System;
using System.Collections.Generic;
using AI;
using Enums;

namespace Game.AI.BehaviorTrees
{
    public class ZorgBT : DefaultBT
    {
        public ZorgBT(Controller controller, EArenaDifficulty arenaDifficulty) : base(controller, arenaDifficulty) { }


        #region Trees

        public override Node GetDefaultTree()
        {
            return new Selector(new List<Node>
            {
                //// TEST ================================================================
                //new TaskUseSpell(m_Controller, ESpell.ImminentDoom),
                //new TaskWait(m_Controller),
                //// TEST ================================================================

                // CHECK : Ultimate
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.Ultimate),

                new Selector(new List<Node>
                {
                    // Madness Whispers : at least 20 Cursed stacks
                    new Sequence(new List<Node>()
                    {
                        new CheckHasState(m_Controller, EStateEffect.Cursed.ToString(), nStacks: 20, target: ESpellTarget.FirstEnemy),
                        new TaskUseSpell(m_Controller, ESpell.MadnessWhispers, spellEvent: ESpellEvent.OnEnd, globalCooldown: 2f),
                    }, saveCurrentNode: true),

                    new Selector(new()
                    {
                        new TaskUseSpell(m_Controller, ESpell.AbyssalStrike, globalCooldown: 1f),
                        new TaskUseSpell(m_Controller, ESpell.ImminentDoom, globalCooldown: 3f),
                        new TaskUseSpell(m_Controller, ESpell.PandemoniumNova, globalCooldown: 3f),
                        new TaskUseSpell(m_Controller, ESpell.Tormentations, globalCooldown: 5f),
                    }, saveCurrentNode: true, random: true),
                    
                    // AutoAttack
                    new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack, ignoreGlobalCooldown: true),

                }, saveCurrentNode: true),

                // Wait
                new TaskWait(m_Controller),
            });
        }

        #endregion



        #region State

        public override void OnStateChanged(string state)
        {
            base.OnStateChanged(state);
        }

        #endregion
    }
}

