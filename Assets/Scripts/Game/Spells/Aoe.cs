using Data;
using Enums;
using System.Collections;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Game.Spells
{
    public class Aoe : Spell
    {
        #region Members

        protected virtual float DELAY_END_DURATION => 1f;

        AoeData m_SpellData => m_BaseSpellData as AoeData;

        protected NetworkVariable<float>  m_Radius    = new NetworkVariable<float>(0);

        protected float m_DurationTimer;

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
            base.Initialize(clientId, target, spellName, level);

            transform.localScale = new Vector3(m_SpellData.Size, m_SpellData.Size, 1f);

            if (!IsServer)
                return;

            // setup radius and timer
            m_Radius.Value      = m_SpellData.Size / 2;
            m_DurationTimer     = m_SpellData.Duration;

            CreateCollisionCircle();
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


        #region Update

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

            // if inifite zone, do nothing
            if (m_SpellData.Duration <= -1f)
                return;

            m_DurationTimer -= Time.deltaTime;
            if (m_DurationTimer <= 0f)
            {
                End();
                return;
            }
        }

        #endregion


        #region Collision Manipulators

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collision"></param>
        protected virtual void OnCollision(Collider2D collision)
        {
            if (!IsServer)
                return;

            if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                // check that players has controller 
                var controller = Finder.FindComponent<Controller>(collision.gameObject);
                if (controller == null)
                {
                    ErrorHandler.Error("Controller not found for player " + collision.gameObject.name);
                    return;
                }

                OnCollisionController(controller);
            }
        }

        protected virtual void OnCollisionController(Controller controller)
        {
            // hit the player
            OnHitPlayer(controller);
        }

        protected void CreateCollisionCircle()
        {
            // Check for collisions within a circle with variableRadius radius
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, m_Radius.Value);

            // Iterate through all colliders found
            foreach (Collider2D collider in colliders)
            {
                OnCollision(collider);
            }
        }

        #endregion


        #region Target & Position

        protected override void SetTarget(Vector3 target)
        {
            if (m_SpellData.SpellSpawn == ESpellSpawn.Ground)
                target.y = 0f;

            transform.position = target;
            base.SetTarget(target);
        }

        #endregion


        #region Listeners

        protected virtual void OnRadiusChanged(float oldRadius, float newRadius)
        {
            transform.localScale = new Vector3(newRadius * 2, newRadius * 2, 1f);
        }

        #endregion
    }
}