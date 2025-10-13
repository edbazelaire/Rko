using System.Collections;
using System.Collections.Generic;
using AI;
using Enums;
using UnityEngine;

namespace Game.AI.BehaviorTrees
{
    public class MaiHauBT : DefaultBT
    {
        public MaiHauBT(Controller controller, EArenaDifficulty arenaDifficulty) : base(controller, arenaDifficulty) { }

        #region Trees

        public override Node GetDefaultTree()
        {
            return new Selector(new List<Node>
            {
                // CHECK : IceBreak
                new Sequence(new List<Node> {
                    // -- Frozen state
                    new CheckHasState(m_Controller, EStateEffect.Frozen.ToString(), target: ESpellTarget.FirstEnemy),
                    new Selector(new List<Node> {
                        new TaskUseSpell(m_Controller, ESpell.Crosslice),
                        new TaskUseSpell(m_Controller, ESpell.SlIceBreaker),
                    }),
                }),

                // CHECK : Infected
                new Sequence(new List<Node> {
                    // check if has required state effects to use his special ability
                    new CheckHasState(m_Controller, EStateEffect.Poison.ToString(), nStacks: 5, target: ESpellTarget.FirstEnemy),

                    new Selector(new List<Node> {
                        new TaskUseSpell(m_Controller, ESpell.SlIceBreaker),
                        new TaskUseSpell(m_Controller, ESpell.FuryBond),
                    }),
                }),

                // ULTIMATE
                new TaskAttack(m_Controller, allowedSpellCategories: new List<ESpellTypeCategory> { ESpellTypeCategory.Ultimate }),
                
                // EXTRA SPELLS
                new TaskUseSpell(m_Controller, ESpell.RazorTempest,  delay: 5f),
                new TaskUseSpell(m_Controller, ESpell.PoisonDarts,  delay: 10f),
                new TaskUseSpell(m_Controller, ESpell.Crosslice,    delay: 15f),
                new TaskUseSpell(m_Controller, ESpell.ExtraClaws,   delay: 20f),

                // AUTO ATTACK
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack),

                // MOVE
                new TaskMove(m_Controller, checkZones: false, checkProjectiles: false),

                // Stand Still if cant move
                new TaskWait(m_Controller),
            });
        }

        #endregion

    }
}

