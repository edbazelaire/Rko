using Data;
using Enums;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Tools.Helpers;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Spells
{
    public class Zone : Aoe
    {
        #region Members

        const float COLLISION_CHECK_REFRESH = 0.1f;
        protected override float DELAY_END_DURATION => 0f;

        ZoneData m_SpellData => m_BaseSpellData as ZoneData;

        /// <summary> timer before next check of collision </summary>
        float m_CollisionCheckRefreshTimer;
        /// <summary> dictionary of players touched by the spell linked to their tick timer (before re-appliance) </summary>
        Dictionary<ulong, float> m_PlayersAffected;
        /// <summary> list of players in the zone </summary>
        List<Controller> m_PlayersInZone;
        /// <summary> calculates number of players in zone right now (for OnTrigger event purpuses) </summary>
        protected int m_NPlayersInZone => m_PlayersInZone.Count();

        #endregion


        #region Init & End

        /// <summary>
        /// 
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="damage"></param>
        /// <param name="duration"></param>
        public override void Initialize(ulong clientId, Vector3 target, SpellData spellData)
        {
            m_PlayersAffected = new Dictionary<ulong, float>();
            m_PlayersInZone = new();
            m_CollisionCheckRefreshTimer = 0;

            base.Initialize(clientId, target, spellData);

            InitializeTriggerZone();
            StartCoroutine(CheckActivation());   
        }

        /// <summary>
        /// Make sure that the Trigger effects are applied if player is already in the zone 
        /// </summary>
        protected void InitializeTriggerZone()
        {
            Collider2D[] colliders = new Collider2D[10];        // Adjust size based on expected objects
            ContactFilter2D filter = new ContactFilter2D();
            filter.layerMask = TargetHelper.DEFAULT_LAYER_MASK;
            filter.useTriggers = true;
            filter.useLayerMask = true;

            int count = GetComponent<Collider2D>().Overlap(filter, colliders);

            var hitControllers = new List<Controller>();
            if (! m_SpellData.ApplyIfNotHitting)
            {
                for (int i = 0; i < count; i++)
                {
                    if (TryGetController(colliders[i], out Controller controller))
                    {
                        if ((m_SpellData.IsEnemyTarget && controller.Team != m_Caster.Team) 
                            || (m_SpellData.IsAllyTarget && controller.Team == m_Caster.Team))
                            hitControllers.Add(controller);
                    }
                }
            }
            else
            {
                var allControllers = m_SpellData.IsEnemyTarget ? GameManager.Instance.GetAllEnemies(m_Caster.Team) : GameManager.Instance.GetAllAllies(m_Caster.Team);
                hitControllers = allControllers.Where(
                    controller => hitControllers.Any(hitController => hitController.PlayerId == controller.PlayerId)
                ).ToList();
            }

            foreach (Controller controller in hitControllers)
            {
                ApplyZoneEffects(controller);
            }
        }

        IEnumerator CheckActivation()
        {
            var timer = m_SpellData.DurationTick;
            while (!m_IsOver && !gameObject.IsDestroyed())
            {
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    // call "OnActivation" event
                    CallSpellEvent(ESpellEvent.OnActivation);
                    timer = m_SpellData.DurationTick;
                }

                yield return null;
            }
            yield return null;
        }

        protected override void End()
        {
            if (m_IsOver)
                return;

            base.End();

            if (m_SpellData.PersistentStateEffects == null)
                return;

            var controllers = m_PlayersInZone.ToArray();
            foreach (Controller controller in controllers)
            {
                if (controller != null && GameManager.IsGameRunning)
                    RemovePersistentStateEffects(controller);
            }
        }

        #endregion


        #region Inherited Manipulators

        /// <summary>
        /// 
        /// </summary>
        protected override void Update()
        {
            if (m_IsOver)
                return;

            base.Update();

            if (!IsServer)
                return;

            if (m_IsOver)
                return;

            // increase size if needs to
            GrowSize();

            // check who's in the collision
            CheckCollision();

            // update time before re-appliance to each players affected
            var keys = m_PlayersAffected.Keys.ToList();
            foreach (ulong clientId in keys)
            {
                m_PlayersAffected[clientId] -= Time.deltaTime;
                if (m_PlayersAffected[clientId] <= 0f)
                    m_PlayersAffected.Remove(clientId);
            }
        }

        protected void OnTriggerEnter2D(Collider2D collider)
        {
            if (!IsServer)
                return;

            if (m_IsOver)
                return;

            // Check if collider belongs to expected layers
            if ((TargetHelper.DEFAULT_LAYER_MASK & (1 << collider.gameObject.layer)) == 0)
                return;

            if (!TryGetController(collider, out Controller controller))
                return;

            if (m_SpellData.ApplyIfNotHitting)
            {
                RemoveZoneEffects(controller);
            } 
            else
            {
                ApplyZoneEffects(controller);
            }
        }

        protected void OnTriggerExit2D(Collider2D collider)
        {
            if (m_IsOver)
                return;

            // Check if collider belongs to expected layers
            if ((TargetHelper.DEFAULT_LAYER_MASK & (1 << collider.gameObject.layer)) == 0)
                return;

            if (!TryGetController(collider, out Controller controller))
                return;

            if (m_SpellData.ApplyIfNotHitting)
            {
                ApplyZoneEffects(controller);
            }
            else
            {
                RemoveZoneEffects(controller);
            }
        }

        void ApplyZoneEffects(Controller controller)
        {
            // apply collision effect
            OnCollisionController(controller);

            // apply persistant effects
            ApplyPersistentStateEffects(controller);

            // apply force
            if (m_SpellData.ZoneForce != default && TargetHelper.IsAllowedTarget(controller, m_Caster, m_SpellData.ZoneForce.Targets))
            {
                controller.Movement.AddForce(m_SpellData.ZoneForce);
            }
        }

        void RemoveZoneEffects(Controller controller)
        {
            // remove persistant effects
            RemovePersistentStateEffects(controller);

            // remove force
            if (m_SpellData.ZoneForce != default)
            {
                controller.Movement.RemoveForce(m_SpellData.ZoneForce);
            }
        }

        #endregion


        #region Collision & OnHit

        /// <summary>
        /// When a collider stays in the zone, refresh OnCollision effect
        /// </summary>
        /// <param name="collision"></param>
        protected void CheckCollision()
        {
            if (!IsServer)
                return;

            // if collision timer not done yet : skip
            m_CollisionCheckRefreshTimer -= Time.deltaTime;
            if (m_CollisionCheckRefreshTimer > 0)
                return;

            // reset collision timer
            m_CollisionCheckRefreshTimer = COLLISION_CHECK_REFRESH;

            // create a collision circle that will apply OnCollision() to colliders found in it
            CreateCollisionCircle();
        }

        /// <summary>
        /// Behavior happening when colliding with a controller
        /// </summary>
        /// <param name="controller"></param>
        protected override void OnCollisionController(Controller controller)
        {
            TryHitController(controller);
        }

        /// <summary>
        /// Behavior happening when colliding with a controller
        /// </summary>
        /// <param name="controller"></param>
        public void TryHitController(Controller controller, bool ignoreEffects = false)
        {
            // check that players was not already affected by the AoE too recently
            if (m_PlayersAffected.ContainsKey(controller.PlayerId))
                return;

            // hit the player
            OnHitTickPlayer(controller, ignoreEffects);
        }

        protected virtual bool OnHitTickPlayer(Controller controller, bool ignoreEffects = false)
        {
             // not alive : skip
            if (!controller.Life.IsAlive)
                return false;

            // apply effects on ally or enemy : if none, skip
            if (!CheckHitEnemyTick(controller, ignoreEffects) && !CheckHitAllyTick(controller))
                return false;

            // call spell event that spell has touched something
            CallSpellEvent(ESpellEvent.OnHit, controller);

            // energy gain (if not structure)
            if (! controller.CharacterData.IsStructure)
                m_Caster.EnergyHandler.AddEnergy(m_SpellData.EnergyGain);

            // add player to affected players
            if (m_SpellData.DurationTick > 0)
                m_PlayersAffected.Add(controller.PlayerId, m_SpellData.DurationTick);

            return true;
        }

        /// <summary>
        /// Check if ability hit an enemy
        /// </summary>
        /// <param name="controller"> controller of hit target </param>
        /// <returns></returns>
        protected virtual bool CheckHitEnemyTick(Controller controller, bool ignoreEffects = false)
        {
            // Target is Ally - return
            if (controller.Team == m_Caster.Team)
                return false;

            // no base Damage, StateEffects or OnHit effects - return
            if (m_SpellData.DotDamage <= 0 && m_SpellData.EnemyStateEffects.Count == 0 && m_SpellData.OnHit.Count == 0)
                return false;

            // add bonus damage from state bonus & boosts 
            int damage = m_SpellData.DotDamage;
            if (m_SpellData.StateEffectStackFactor != EStateEffect.None)
            {
                damage *= controller.StateHandler.GetStacks(m_SpellData.StateEffectStackFactor);
            }
            damage = m_Caster.StateHandler.ApplyBonusInt(damage, EStateEffectProperty.DotDamage, controller, specialCondition: m_SpellData.Name);

            // get final damage after shields and resistances
            int finalDamage = controller.Life.Hit(damage, m_Caster.PlayerId, m_SpellData.Parent, EDamageCategory.Magical, EHitCategory.Dot);
            if (finalDamage > 0 && m_Caster.ClientAnalytics != null)
                m_Caster.ClientAnalytics.SendSpellDataClientRPC(m_SpellData.Name, EHitType.PhysicalDamage, finalDamage);

            ErrorHandler.Log(m_SpellData.Name + " : " + finalDamage, ELogTag.Spells);

            // apply lifesteal if any (remove 1 because floats values are always based on 1 as default value)
            float lifeSteal = SpellData.LifeSteal + Mathf.Max(0f, m_Caster.StateHandler.GetFloat(EStateEffectProperty.BonusLifeSteal) - 1);
            if (lifeSteal > 0 && finalDamage > 0)
            {
                m_Caster.Life.Heal((int)Mathf.Round(lifeSteal * finalDamage), m_Caster.PlayerId, m_SpellData.Name, m_SpellData.SpellCategory);
            }

            // apply state effects specifics to enemies
            if (!ignoreEffects)
                ApplyStateEffects(controller, m_SpellData.EnemyStateEffects);

            return true;
        }

        /// <summary>
        /// Check if ability hit an ally
        /// </summary>
        /// <param name="controller"> controller of hit target </param>
        /// <returns></returns>
        protected virtual bool CheckHitAllyTick(Controller controller)
        {
            if (controller.Team != m_Caster.Team)
                return false;

            if (m_SpellData.DotHeal <= 0 && m_SpellData.DotEnergy == 0 && m_SpellData.AllyStateEffects.Count == 0)
                return false;

            // add bonus heal from state bonus & boosts 
            int heal = m_SpellData.DotHeal;
            if (m_SpellData.StateEffectStackFactor != EStateEffect.None)
            {
                heal *= controller.StateHandler.GetStacks(m_SpellData.StateEffectStackFactor);
            }
            heal = m_Caster.StateHandler.ApplyBonusInt(heal, EStateEffectProperty.DotHeal, controller, specialCondition: m_SpellData.Name);

            // heal the target for the specified amount
            controller.Life.Heal(heal, m_Caster.PlayerId, m_SpellData.Name, m_SpellData.SpellCategory);

            // add energy to the target for the specified amount
            int energy = m_SpellData.DotEnergy;
            if (m_SpellData.StateEffectStackFactor != EStateEffect.None)
            {
                energy *= controller.StateHandler.GetStacks(m_SpellData.StateEffectStackFactor);
            }
            controller.EnergyHandler.AddEnergy(energy);

            if (m_Caster.ClientAnalytics != null)
                m_Caster.ClientAnalytics.SendSpellDataClientRPC(m_SpellData.Name, EHitType.Heal, heal);

            // apply ally state effects
            ApplyStateEffects(controller, m_SpellData.AllyStateEffects);

            return true;
        }

        #endregion


        #region Size Growth

        void GrowSize()
        {
            if (m_SpellData.GrowSizeFactor == 0)
                return;

            // percentage of time completion 
            float timeFactor = Mathf.Clamp(m_SpellData.Duration <= 0 || m_SpellData.MaxSizeAt <= 0 ? 1 : (m_SpellData.Duration - m_DurationTimer) / (m_SpellData.Duration * m_SpellData.MaxSizeAt), 0, 1);
            
            // value of the radius
            Radius.Value = (1 + timeFactor * m_SpellData.GrowSizeFactor) * m_SpellData.Size / 2;
        }

        #endregion


        #region Persistent Effects

        void ApplyPersistentStateEffects(Controller controller)
        {
            // check if already affected by this zone
            if (m_PlayersInZone.Contains(controller))
                return;

            // onlyc call "trigger" event for first activation
            if (m_NPlayersInZone == 0)
                CallSpellEvent(ESpellEvent.OnTriggerEnter, controller);

            // add the controller to the list of current controllers
            m_PlayersInZone.Add(controller);

            if (m_SpellData.PersistentStateEffects == null)
                return;
            
            ApplyStateEffects(controller, m_SpellData.PersistentStateEffects);
        }

        void RemovePersistentStateEffects(Controller controller)
        {
            // check if affected by this zone
            if (! m_PlayersInZone.Contains(controller))
                return;

            // remove from list of players
            m_PlayersInZone.Remove(controller);

            // reduce number of players trigerring the zone
            if (m_NPlayersInZone == 0)
                CallSpellEvent(ESpellEvent.OnTriggerExit, controller);

            // check if has persistant state effects
            if (m_SpellData.PersistentStateEffects == null)
                return;

            foreach (var stateEffectData in m_SpellData.PersistentStateEffects)
            {
                if (!controller.StateHandler.HasState(stateEffectData.StateEffect.ToString()))
                    continue;

                controller.StateHandler.RemoveStateEffect(stateEffectData.StateEffect.ToString());
            }
            
        }

        #endregion
    }
}