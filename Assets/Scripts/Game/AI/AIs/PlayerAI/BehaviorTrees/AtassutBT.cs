using System.Collections.Generic;
using AI;
using Enums;

namespace Game.AI.BehaviorTrees
{
    public static class AtassutBT
    {
        public static Node LoadTree(Controller controller)
        {
            return new Selector(new List<Node>
            {
                // COUNTER STATE
                new Sequence(new List<Node> {
                    new CheckHasCounter(controller),
                    new TaskUseSpell(controller, ESpell.Scythefall),
                }),

                // ULTIMATE
                new TaskAttack(controller, allowedSpellCategories: new List<ESpellCategory> { ESpellCategory.Ultimate }),

                // USE SPECIAL ABILITY
                new TaskUseSpell(controller, ESpell.Scythefall),

                // MOVE
                new TaskMove(controller, checkZones: true, checkProjectiles: false),

                // Stand Still if cant move
                new TaskAutoAttack(controller),
            });
        }
    }
}

