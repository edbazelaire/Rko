using Enums;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Overdose", menuName = "Game/StateEffects/SpecialEffects/Overdose")]
    public class Overdose : StateEffect
    {
        public override int RecalculateStacks(int stacks, Controller caster, Controller targetController)
        {
            Debug.Log("RecalculateStacks() : " + stacks);
            base.RecalculateStacks(stacks, caster, targetController);

            int consumedStacks = 0;                 // currently consumed stacks
            int maxStacks = m_MaxStacks - targetController.StateHandler.GetStacks("Overdose");   // maximum number of stacks to add

            // Create modifiable pool of enemy controllers
            var potentialTargets = new List<Controller>(GameManager.Instance.GetAllEnemies(caster.Team, spawnIncluded: true));

            while (consumedStacks < maxStacks && potentialTargets.Count > 0)
            {
                // Select a random enemy
                int index = Random.Range(0, potentialTargets.Count);
                var target = potentialTargets[index];
                var stateHandler = target.StateHandler;

                // Check if the enemy has at least one of the effects
                bool hasPoison = stateHandler.HasState(EStateEffect.Poison);
                bool hasInfected = stateHandler.HasState(EStateEffect.Infected);

                if (!hasPoison && !hasInfected)
                {
                    potentialTargets.RemoveAt(index);
                    continue;
                }

                // Determine how many stacks to consume this round (1 to 3, capped by remaining maxStacks)
                int toConsume = Mathf.Min(Random.Range(1, 4), maxStacks - consumedStacks);
                if (toConsume <= 0)
                    break;

                // If room left and target has infected
                if (hasInfected)
                {
                    int infectedRemoved = stateHandler.RemoveStateEffect(EStateEffect.Infected.ToString(), consume: true, maxStacks: toConsume);
                    consumedStacks += 3 * infectedRemoved;
                    toConsume -= infectedRemoved;
                }

                // Try to consume from Poison
                if (hasPoison)
                {
                    int poisonRemoved = stateHandler.RemoveStateEffect(EStateEffect.Poison.ToString(), consume: true, maxStacks: toConsume);
                    consumedStacks += poisonRemoved;
                }
            }

            Debug.Log("     + Final stacks : " + consumedStacks);
            return consumedStacks;
        }
    }
}