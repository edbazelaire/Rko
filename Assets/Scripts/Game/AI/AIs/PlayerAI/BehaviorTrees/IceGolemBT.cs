using System.Collections.Generic;
using AI;
using Enums;
using UnityEngine;

namespace Game.AI.BehaviorTrees
{
    public static class IceGolemBT
    {
        public static float m_CarapiceDelay = 10f;

        public static Node LoadTree(Controller controller)
        {
            return new Selector(new List<Node>
            {
                // Check Has STATE : Melting
                new Sequence(new List<Node> {
                    new CheckHasState(controller, "Melting"),
                    new Selector(new List<Node>
                    {
                        new TaskUseSpell(controller, ESpell.Carapice, delay: m_CarapiceDelay),
                        new TaskAutoAttack(controller, true),
                        new TaskMove(controller, checkZones: false, checkProjectiles: false),
                    }),
                }),

                // Check if character is currently in a ZoneSpell
                new Sequence(new List<Node> {
                    new CheckState(controller, EGlobalState.Casting, false),    
                    new CheckInZone(controller),
                    new TaskExitZone(controller),
                }),

                new TaskAutoAttack(controller, true),

                new TaskMove(controller, checkZones: true, checkProjectiles: false),

                new TaskAutoAttack(controller),
            });
        }
    }
}

