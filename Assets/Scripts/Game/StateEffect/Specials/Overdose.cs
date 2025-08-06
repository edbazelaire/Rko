using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Overdose", menuName = "Game/StateEffects/SpecialEffects/Overdose")]
    public class Overdose : StateEffect
    {
        public override int RecalculateStacks(int stacks, Controller caster, Controller targetController)
        {
            base.RecalculateStacks(stacks, caster, targetController);

            int consumedStacks = 0;                 // currently consumed stacks
            int maxStacks = m_MaxStacks - caster.StateHandler.GetStacks("Overdose");   // maximum number of stacks to add

            // Create modifiable pool of enemy controllers
            var potentialTargets = new List<Controller>(GameManager.Instance.GetAllEnemies(caster.Team, spawnIncluded: true));
            // put spawn at the end
            potentialTargets = potentialTargets.OrderBy(t => t.IsSpawn).Reverse().ToList();

            foreach (Controller target in potentialTargets)
            {
                var stateHandler = target.StateHandler;

                // Check if the enemy has at least one of the effects
                bool hasPoison = stateHandler.HasState(EStateEffect.Poison);
                bool hasInfected = stateHandler.HasState(EStateEffect.Infected);

                if (!hasPoison && !hasInfected)
                    continue;

                // Determine how many stacks to consume 
                int toConsume = 0;

                // Try to consume from Poison
                if (hasPoison)
                {
                    toConsume = Mathf.Max(maxStacks - consumedStacks, 0);
                    int poisonRemoved = stateHandler.RemoveStateEffect(EStateEffect.Poison.ToString(), consume: true, maxStacks: toConsume);
                    consumedStacks += poisonRemoved;
                }

                // If room left and target has infected
                if (hasInfected)
                {
                    // Determine how many stacks to consume this round (1 to 3, capped by remaining maxStacks)
                    toConsume = (int)Math.Ceiling(Mathf.Max(maxStacks - consumedStacks, 0) / 3f);
                    int infectedRemoved = stateHandler.RemoveStateEffect(EStateEffect.Infected.ToString(), consume: true, maxStacks: toConsume);
                    consumedStacks += 3 * infectedRemoved;
                }
            }

            Debug.Log("     + Final stacks : " + consumedStacks);
            return consumedStacks;
        }
    }
}