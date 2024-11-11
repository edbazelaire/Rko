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
                }),

                // ULTIMATE
                new Sequence(new List<Node> {
                    new CheckCanBeCasted(controller, controller.SpellHandler.Ultimate),
                    new TaskUseSpell(controller, controller.SpellHandler.Ultimate),
                }),

                // USE SPECIAL ABILITY
                new TaskUseSpell(controller, ESpell.Scythefall),
            
                // USE VORTEX
                new Sequence(new List<Node> {
                    new CheckCanBeCasted(controller, ESpell.Vortex),
                    new TaskUseSpell(controller, ESpell.Vortex, delay: 15),
                }),

                // MOVE
                new TaskMove(controller, checkZones: true, checkProjectiles: false),

                // Stand Still if cant move
                new TaskAutoAttack(controller),
            });
        }
    }
}

