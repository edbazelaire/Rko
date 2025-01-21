using Data;
using Enums;
using System.Collections.Generic;
using System.Linq;
using Tools;
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

        #endregion


        #region Init & End

        /// <summary>
        /// 
        /// </summary>
        public override void OnNetworkSpawn()
        {
            m_Radius.OnValueChanged += OnRadiusChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="damage"></param>
        /// <param name="duration"></param>
        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level, string parent)
        {
            m_PlayersAffected = new Dictionary<ulong, float>();
            m_CollisionCheckRefreshTimer = 0;

            base.Initialize(clientId, target, spellName, level, parent);

            InitializeTriggerZone();
        }

        /// <summary>
        /// Make sure that the Trigger effects are applied if player is already in the zone 
        /// </summary>
        protected void InitializeTriggerZone()
        {
            Collider2D[] colliders = new Collider2D[10]; // Adjust size based on expected objects
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;

            int count = GetComponent<Collider2D>().Overlap(filter, colliders);

            var hitControllers = new List<Controller>();
            if (! m_SpellData.ApplyIfNotHitting)
            {
                for (int i = 0; i < count; i++)
                {
                    if (TryGetController(colliders[i], out Controller controller))
                    {
                        hitControllers.Add(controller);
                    }
                }
            }
            else
            {
                var allControllers = m_SpellData.IsEnemyTarget ? GameManager.Instance.GetAllEnemies(m_Controller.Team) : GameManager.Instance.GetAllAllies(m_Controller.Team);
                hitControllers = allControllers.Where(
                    controller => hitControllers.Any(hitController => hitController.PlayerId == controller.PlayerId)
                ).ToList();
            }

            foreach (Controller controller in hitControllers)
            {
                ApplyZoneEffects(controller);
            }
        }

        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        public override void OnDestroy()
        {
            m_Radius.OnValueChanged -= OnRadiusChanged;
            base.OnDestroy();
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

            GrowSize();

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
            if (m_SpellData.ZoneForce != default)
                controller.Movement.AddForce(m_SpellData.ZoneForce);
        }

        void RemoveZoneEffects(Controller controller)
        {
            // remove persistant effects
            RemovePersistentStateEffects(controller);

            // remove force
            if (m_SpellData.ZoneForce != default)
                controller.Movement.RemoveForce(m_SpellData.ZoneForce);
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
            // check that players was not already affected by the AoE too recently
            if (m_PlayersAffected.ContainsKey(controller.OwnerClientId))
                return;

            // hit the player
            OnHitTickPlayer(controller);
        }

        protected virtual void OnHitTickPlayer(Controller controller)
        {
             // not alive : skip
            if (!controller.Life.IsAlive)
                return;

            // apply effects on ally or enemy : if none, skip
            if (!CheckHitEnemyTick(controller) && !CheckHitAllyTick(controller))
                return;

            // call spell event that spell has touched something
            if (m_SpellData.HasGfxEventAt(ESpellEvent.OnHit, checkEnd: false))
                CallSpellEventClientRPC(ESpellEvent.OnHit, controller.PlayerId);

            // energy gain
            m_Controller.EnergyHandler.AddEnergy(m_SpellData.EnergyGain);

            // add player to affected players
            if (m_SpellData.DurationTick > 0)
                m_PlayersAffected.Add(controller.OwnerClientId, m_SpellData.DurationTick);
        }

        /// <summary>
        /// Check if ability hit an enemy
        /// </summary>
        /// <param name="controller"> controller of hit target </param>
        /// <returns></returns>
        protected virtual bool CheckHitEnemyTick(Controller controller)
        {
            // Target is Ally - return
            if (controller.Team == m_Controller.Team)
                return false;

            // no base Damages, StateEffects or OnHit effects - return
            if (m_SpellData.TickDamages <= 0 && m_SpellData.EnemyStateEffects.Count == 0 && m_SpellData.OnHit.Count == 0)
                return false;

            // add bonus damages from state bonus & boosts 
            int damages = m_SpellData.TickDamages;
            if (m_SpellData.StateEffectStackFactor != EStateEffect.None)
            {
                damages *= controller.StateHandler.GetStacks(m_SpellData.StateEffectStackFactor);
            }
            damages = m_Controller.StateHandler.ApplyBonusInt(damages, EStateEffectProperty.TickDamages, controller);

            // get final damages after shields and resistances
            int finalDamages = controller.Life.Hit(damages, m_Controller.PlayerId, m_SpellData.Parent, m_SpellData.SpellCategory);
            if (finalDamages > 0 && m_Controller.ClientAnalytics != null)
                m_Controller.ClientAnalytics.SendSpellDataClientRPC(m_SpellData.Name, EHitType.Damage, finalDamages);

            ErrorHandler.Log(m_SpellData.Name + " : " + finalDamages, ELogTag.Spells);

            // apply lifesteal if any (remove 1 because floats values are always based on 1 as default value)
            float lifeSteal = SpellData.LifeSteal + Mathf.Max(0f, m_Controller.StateHandler.GetFloat(EStateEffectProperty.BonusLifeSteal) - 1);
            if (lifeSteal > 0 && finalDamages > 0)
            {
                m_Controller.Life.Heal((int)Mathf.Round(lifeSteal * finalDamages), m_Controller.PlayerId, m_SpellData.Name, m_SpellData.SpellCategory);
                
                if (m_Controller.ClientAnalytics != null)
                { 
                    m_Controller.ClientAnalytics.SendSpellDataClientRPC(m_SpellData.Name, EHitType.Heal, (int)Mathf.Round(lifeSteal * finalDamages));
                    m_Controller.ClientAnalytics.SendSpellDataClientRPC(m_SpellData.Name, EHitType.LifeSteal, (int)Mathf.Round(lifeSteal * finalDamages));
                }
            }

            // apply state effects specifics to enemies
            ApplyEnemyStateEffects(controller);

            return true;
        }

        /// <summary>
        /// Check if ability hit an ally
        /// </summary>
        /// <param name="controller"> controller of hit target </param>
        /// <returns></returns>
        protected virtual bool CheckHitAllyTick(Controller controller)
        {
            if (controller.Team != m_Controller.Team)
                return false;

            if (m_SpellData.TickHeal <= 0 && m_SpellData.AllyStateEffects.Count == 0)
                return false;

            // add bonus heal from state bonus & boosts 
            int heal = m_SpellData.TickHeal;
            if (m_SpellData.StateEffectStackFactor != EStateEffect.None)
            {
                heal *= controller.StateHandler.GetStacks(m_SpellData.StateEffectStackFactor);
            }
            heal = m_Controller.StateHandler.ApplyBonusInt(heal, EStateEffectProperty.TickHeal, controller);

            // heal the target for the specified amount
            controller.Life.Heal(heal, m_Controller.PlayerId, m_SpellData.Name, m_SpellData.SpellCategory);

            if (m_Controller.ClientAnalytics != null)
                m_Controller.ClientAnalytics.SendSpellDataClientRPC(m_SpellData.Name, EHitType.Heal, heal);

            ApplyAllyStateEffects(controller);

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
            m_Radius.Value = (1 + timeFactor * m_SpellData.GrowSizeFactor) * m_SpellData.Size / 2;
        }

        #endregion


        #region Persistent Effects

        void ApplyPersistentStateEffects(Controller controller)
        {
            if (m_SpellData.PersistentStateEffects == null)
                return;
            
            ApplyStateEffects(controller, m_SpellData.PersistentStateEffects);
        }

        void RemovePersistentStateEffects(Controller controller)
        {
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