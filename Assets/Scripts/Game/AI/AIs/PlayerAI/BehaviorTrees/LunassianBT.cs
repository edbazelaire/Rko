using System.Collections.Generic;
using AI;
using AI.Checkers;
using Enums;
using Game.AI.Tasks.Resets;
using UnityEngine;

namespace Game.AI.BehaviorTrees
{
    public static class LunassianBT
    {
        public static Node LoadTree(Controller controller, EArenaDifficulty arenaDifficulty)
        {
            switch (arenaDifficulty)
            {
                case EArenaDifficulty.Normal:
                    Debug.Log("Loading (Normal Tree)");
                    return GetTree_Normal(controller);

                default:
                    Debug.Log("Loading (Hard Tree)");
                    return GetTree_Hard(controller);
            }
        }

        static Node GetTree_Normal(Controller controller)
        {
            return new Selector(new List<Node>
            {
                new TaskUseSpell(controller, ESpell.FerociousBite, delay: 10),

                // Attack 5 times every 3 seconds
                new TaskUseSpell(controller, controller.SpellHandler.AutoAttack, delay: 3f, nTimes: 5),

                // MOVE
                new TaskMove(controller, checkZones: false, checkProjectiles: false),         
                
                // Default
                new TaskAutoAttack(controller),
            });
        }

        static Node GetTree_Hard(Controller controller)
        {
            return new Selector(new List<Node>
            {
                // use Special Ability in 10 seconds
                new TaskUseSpell(controller, ESpell.FerociousBite, delay: 10),

                // Check one of Extra Spells
                new Sequence(new List<Node> {
                    new CheckTimer(controller, "TaskAttack", 3f),
                    new TaskAttack(controller),
                    new ResetTimer(controller, "TaskAttack", 3f)
                }),

                // Check if character is currently in a ZoneSpell
                new Sequence(new List<Node> {
                    new CheckInZone(controller),
                    new TaskExitZone(controller),
                }),

                // Attack 4 times every 1.5 seconds
                new TaskUseSpell(controller, controller.SpellHandler.AutoAttack, delay: 1.5f, nTimes: 4),

                // MOVE
                new TaskMove(controller, checkZones: true, checkProjectiles: false),         

                // Default - if cant move (should not be used most of the time)
                new TaskAutoAttack(controller),
            });
        }
    }
}

