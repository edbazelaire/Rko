using System.Collections.Generic;
using AI;
using AI.Checkers;
using Enums;
using Game.AI.Tasks.Resets;
using Tools;

namespace Game.AI.BehaviorTrees
{
    public static class IceGolemBT
    {
        public static float CarapiceDelay = 10f;

        public static Node LoadTree(Controller controller, EArenaDifficulty arenaDifficulty)
        {
            switch (arenaDifficulty)
            {
                case (EArenaDifficulty.Normal):
                    return GetTree_Normal(controller);

                case (EArenaDifficulty.Hard):
                    return GetTree_Hard(controller);

                default:
                    ErrorHandler.Warning("Unhanlded Tree for difficulty " + arenaDifficulty);
                    return GetTree_Normal(controller);
            }
        }

        public static Node GetTree_Normal(Controller controller)
        {
            return new Selector(new List<Node>
            {
                // Check Has STATE : Melting
                new Sequence(new List<Node> {
                    new CheckHasState(controller, "Melting"),
                    new Selector(new List<Node>
                    {
                        new TaskUseSpell(controller, ESpell.Carapice, delay: CarapiceDelay),
                        new TaskAutoAttack(controller, true),
                        new TaskMove(controller, checkZones: false, checkProjectiles: false),
                    }),
                }),

                // Check cast : STALACMITE
                new Sequence(new List<Node> {
                    new CheckTimer(controller, "Stalacmite", 8f),
                    new TaskUseSpell(controller, ESpell.Stalacmite),
                }),

                // Check cast : IceField
                new TaskUseSpell(controller, ESpell.IceField),

                new TaskAutoAttack(controller, true),

                new TaskMove(controller, checkZones: false, checkProjectiles: false),

                new TaskAutoAttack(controller),
            });
        }

        public static Node GetTree_Hard(Controller controller)
        {
            return new Selector(new List<Node>
            {
                // Check cast : STALACMITE
                new Sequence(new List<Node> {
                    new CheckTimer(controller, "Stalacmite", 8f),
                    new CheckCanBeCasted(controller, ESpell.Stalacmite),
                    new TaskUseSpell(controller, ESpell.Stalacmite),
                }),

                // Check Has STATE : Melting
                new Sequence(new List<Node> {
                    new CheckHasState(controller, "Melting"),
                    new Selector(new List<Node>
                    {
                        new TaskUseSpell(controller, ESpell.Carapice, delay: CarapiceDelay),
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

