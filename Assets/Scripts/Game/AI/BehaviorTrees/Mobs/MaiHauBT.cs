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
                // SPECIAL ABILITY
                new Sequence(new List<Node> {
                    // check if has required state effects to use his special ability
                    new Selector(new List<Node> {
                        // -- at least 5 Poison stacks
                        new CheckHasState(m_Controller, EStateEffect.Poison.ToString(), nStacks: 5, target: ESpellTarget.FirstEnemy),
                        // -- OR Frozen state
                        new CheckHasState(m_Controller, EStateEffect.Frozen.ToString(), target: ESpellTarget.FirstEnemy),
                    }),

                    new TaskUseSpell(m_Controller, ESpell.SlIceBreaker),
                }),

                // ULTIMATE
                new TaskAttack(m_Controller, allowedSpellCategories: new List<ESpellTypeCategory> { ESpellTypeCategory.Ultimate }),
                
                // EXTRA SPELLS
                new TaskUseSpell(m_Controller, ESpell.ExtraClaws, delay: 25f),
                new TaskUseSpell(m_Controller, ESpell.Crosslice, delay: 15f),

                // MOVEMENT : dodge enemy zone spells
                new Sequence(new List<Node> {
                    new CheckInZone(m_Controller),
                    new TaskExitZone(m_Controller),
                }),

                // AUTO ATTACK
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack),

                // MOVE
                new TaskMove(m_Controller, checkZones: true, checkProjectiles: false),

                // Stand Still if cant move
                new TaskWait(m_Controller),
            });
        }

        #endregion

    }
}

