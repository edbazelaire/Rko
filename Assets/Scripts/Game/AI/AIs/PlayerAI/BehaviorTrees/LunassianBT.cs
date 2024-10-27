using System.Collections.Generic;
using AI;
using Enums;
using UnityEngine;

namespace Game.AI.BehaviorTrees
{
    public static class LunassianBT
    {
        public static Node LoadTree(Controller controller)
        {
            return new Selector(new List<Node>
            {
                new TaskUseSpell(controller, ESpell.FerociousBite, delay: 10),

                // Check if character is currently in a ZoneSpell
                new Sequence(new List<Node> {
                    new CheckInZone(controller),
                    new TaskExitZone(controller),
                }),

                // Attack 3 times every 3 seconds
                new TaskUseSpell(controller, controller.SpellHandler.AutoAttack, delay: 2.5f, nTimes: 3),

                // MOVE
                new TaskMove(controller, checkZones: true, checkProjectiles: false),         
                
                // Default
                new TaskAutoAttack(controller),
            });
        }
    }
}

