using System.Collections.Generic;
using AI;
using Enums;
using Game.AI;
using Tools;

namespace Game.AI.BehaviorTrees
{
    public static class BTLoader
    {
        /// <summary>
        ///  Load the Behavior Tree corresponding to the situation
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="characterName"></param>
        /// <returns></returns>
        public static Node LoadTree(Controller controller, string characterName)
        {
            if (controller == null)
            {
                ErrorHandler.Error("No controller found for this CharacterBT");
                return new Node();
            }

            if (characterName == EBoss.IceGolem.ToString())
                return IceGolemBT.LoadTree(controller);  

            if (characterName == EBoss.MaiHau.ToString())
                return MaiHauBT.LoadTree(controller);  

            if (characterName == EBoss.Atassut.ToString())
                return AtassutBT.LoadTree(controller);  

            if (characterName == EBoss.Lunassian.ToString())
                return LunassianBT.LoadTree(controller);       
            
            return LoadBasicTree(controller);
        }

        public static Node LoadBasicTree(Controller controller)
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
                        new TaskAutoAttack(controller),
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
                new TaskAutoAttack(controller),
            });
        }
    }

}
