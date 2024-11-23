using System;
using System.Collections.Generic;
using AI;
using Enums;
using Save;
using Tools;

namespace Game.AI.BehaviorTrees
{
    public static class BTLoader
    {
        public static DefaultBT GetIBehaviorTree(Controller controller, string characterName, EArenaDifficulty arenaDifficulty = EArenaDifficulty.Normal)
        {
            if (characterName == EBoss.IceGolem.ToString())
                return new IceGolemBT(controller, arenaDifficulty);

            if (characterName == EBoss.MaiHau.ToString())
                return new MaiHauBT(controller, arenaDifficulty);

            if (characterName == EBoss.Atassut.ToString())
                return new AtassutBT(controller, arenaDifficulty);

            if (characterName == EBoss.Sikunik.ToString())
                return new SikunikBT(controller, arenaDifficulty);

            if (characterName == EBoss.Lunassian.ToString())
                return new LunassianBT(controller, arenaDifficulty);

            return new DefaultBT(controller, arenaDifficulty);
        }

        /// <summary>
        ///  Load the Behavior Tree corresponding to the situation
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="characterName"></param>
        /// <returns></returns>
        public static Node LoadTree(Controller controller, string characterName, EArenaDifficulty arenaDifficulty = EArenaDifficulty.Normal)
        {
            if (controller == null)
            {
                ErrorHandler.Error("No controller found for this CharacterBT");
                return new Node();
            }

            return GetIBehaviorTree(controller, characterName, arenaDifficulty).LoadTree();
        }

        public static Action<string> GetStateChangedCallback(Controller controller, string characterName, EArenaDifficulty arenaDifficulty = EArenaDifficulty.Normal)
        {
            return GetIBehaviorTree(controller, characterName, arenaDifficulty).OnStateChanged;
        }

        public static Node LoadBasicTree(Controller controller)
        {
            return new Selector(new List<Node>
            {
                // Check Immadiat Threats (Zones & Projectiles)
                new Sequence(new List<Node> {
                    new CheckImmediatThreat(controller),
                    new Selector(new List<Node>
                    {
                        new TaskCounter(controller),
                        new TaskJump(controller),
                    }),
                }),

                // Check if character is currently in a ZoneSpell
                new Sequence(new List<Node> {
                    new CheckInZone(controller),
                    new TaskExitZone(controller),
                }),

                new TaskAttack(controller),

                new TaskWait(controller)
            });
        }

        public static Node LoadAdvancedTree(Controller controller)
        {
            return new Selector(new List<Node>
            {
                new Sequence(new List<Node> {
                    new CheckRandom(controller),
                    new Selector(new List<Node>
                    {
                        new TaskAttack(controller),
                        new TaskCounter(controller),
                        new TaskJump(controller),
                        new TaskMove(controller),
                        new TaskUseSpell(controller, controller.SpellHandler.AutoAttack),
                    }, random: true),
                 }),

                // Check Immadiat Threats (Zones & Projectiles)
                new Sequence(new List<Node> {
                    new CheckImmediatThreat(controller),
                    new Selector(new List<Node>
                    {
                        new TaskCounter(controller),
                        new TaskJump(controller),
                    }),
                }),

                // Check if character is currently in a ZoneSpell
                new Sequence(new List<Node> {
                    new CheckInZone(controller),
                    new TaskExitZone(controller),
                }),

                // Check if character has imperative to dodge
                new Sequence(new List<Node> {
                    new CheckDodge(controller),
                    new TaskDodge(controller),
                }),

                new TaskAttack(controller),
                new TaskWait(controller),
            });
        }
    }

}
