using Data;
using System.Collections.Generic;
using System.Linq;
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
        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level)
        {
            m_PlayersAffected = new Dictionary<ulong, float>();
            m_CollisionCheckRefreshTimer = 0;

            base.Initialize(clientId, target, spellName, level);
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

            // apply collision effect
            OnCollisionController(controller);

            // apply persistant effects
            ApplyPersistentStateEffects(controller);
        }

        protected void OnTriggerExit2D(Collider2D collider)
        {
            if (!TryGetController(collider, out Controller controller))
                return;

            // remove persistant effects
            RemovePersistentStateEffects(controller);
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
            OnHitPlayer(controller);
        }

        /// <summary>
        /// When a player is hit, remove it from list of hitted players (because there is no such limit for zones)
        /// </summary>
        /// <param name="controller"></param>
        protected override void OnHitPlayer(Controller controller)
        {
            base.OnHitPlayer(controller);

            // zone can affect the same player multiple times
            m_HittedPlayerId.Clear();

            // add player to affected players
            if (m_SpellData.DurationTick > 0)
                m_PlayersAffected.Add(controller.OwnerClientId, m_SpellData.DurationTick);
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