using System.Collections;
using System.Collections.Generic;
using AI;
using Enums;
using UnityEngine;

namespace Game.AI.BehaviorTrees
{
    public static class MaiHauBT
    {
        public static Node LoadTree(Controller controller)
        {
            return new Selector(new List<Node>
            {
                // SPECIAL ABILITY
                new Sequence(new List<Node> {
                    // check if has required state effects to use his special ability
                    new Selector(new List<Node> {
                        // -- at least 5 Poison stacks
                        new CheckHasState(controller, EStateEffect.Poison.ToString(), nStacks: 5, target: ESpellTarget.FirstEnemy),
                        // -- OR Frozen state
                        new CheckHasState(controller, EStateEffect.Frozen.ToString(), target: ESpellTarget.FirstEnemy),
                    }),
                    
                    new TaskUseSpell(controller, ESpell.SlIceBreaker),
                }),

                // ULTIMATE
                new TaskAttack(controller, allowedSpellCategories: new List<ESpellCategory> { ESpellCategory.Ultimate }),

                // MOVEMENT : dodge enemy zone spells
                new Sequence(new List<Node> {
                    new CheckInZone(controller),
                    new TaskExitZone(controller),
                }),

                // AUTO ATTACK
                new TaskAutoAttack(controller, true),

                // MOVE
                new TaskMove(controller, checkZones: true, checkProjectiles: false),

                // Stand Still if cant move
                new TaskAutoAttack(controller),
            });
        }
    }
}

