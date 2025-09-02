using Data;
using Enums;
using Game.NetworkStructures;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Tools.Helpers;
using Unity.Netcode;
using UnityEngine;

namespace Game.Spells
{
    public class Aoe : Spell
    {
        #region Members

        protected virtual float DELAY_END_DURATION => 1f;

        AoeData m_SpellData => m_BaseSpellData as AoeData;

        readonly NetworkVariable<float> m_Radius = new NetworkVariable<float>();

        protected float m_DurationTimer;

        public IReplicatedVar<float> Radius { get; protected set; }

        #endregion


        #region Init & End

        /// <summary>
        /// 
        /// </summary>
        public override void OnSpawned()
        {
            base.OnSpawned();
            Radius = ReplicatedVar.Create(m_Radius, offlineInitial: 0);
            Radius.OnValueChanged += OnRadiusChanged;
        }

        /// <summary>
        /// Unsubscribe from events
        /// </summary>
        public override void OnDespawned()
        {
            base.OnDespawned();
            Radius.OnValueChanged -= OnRadiusChanged;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="damage"></param>
        /// <param name="duration"></param>
        public override void Initialize(ulong clientId, Vector3 target, SpellData spellData)
        {
            base.Initialize(clientId, target, spellData);

            transform.localScale = new Vector3(m_SpellData.Size, m_SpellData.Size, m_SpellData.Size);

            if (!IsServer)
                return;

            // setup radius and timer
            Radius.Value          = m_SpellData.Size / 2;

            m_DurationTimer = m_SpellData.Duration;

            CreateCollisionCircle();
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
        /// Create a collision circle of the spell's radius that will apply the spells effect for each allowed Controllers inside
        /// </summary>
        protected void CreateCollisionCircle()
        {
            if (!IsServer)
                return;

            // setup layer filter 
            var filter = Physics2DQueries.BuildFilter(TargetHelper.ALL_LAYER_MASK);

            // Check for collisions within a circle with variableRadius radius
            int count = Physics2DQueries.OverlapCircle(transform.position, Radius.Value, filter, out Collider2D[] hits);

            // Gat all controllers touched by the 2D collision circle
            var hitControllers = new List<Controller>();
            for (int i = 0; i < count; i++)
            {
                if (! CheckCollision(hits[i], out Controller controller))
                    continue;

                hitControllers.Add(controller);
            }

            // if "ApplyIfNotHitting" : set hitControllers to be the list of ALL controllers NOT HIT
            if (m_SpellData.ApplyIfNotHitting)
            {
                var allControllers = m_SpellData.IsEnemyTarget
                    ? GameManager.Instance.GetAllEnemies(m_Caster.Team)
                    : GameManager.Instance.GetAllAllies(m_Caster.Team);

                hitControllers = allControllers.Where(
                    controller => hitControllers.Any(hitController => hitController.PlayerId == controller.PlayerId)
                ).ToList();
            }

            // Apply OnHit effect on each Controllers
            foreach (var controller in hitControllers)
            {
                OnCollisionController(controller);
            }
        }

        /// <summary>
        /// 
        /// </summary>q
        /// <param name="collision"></param>
        protected virtual bool CheckCollision(Collider2D collision, out Controller controller)
        {
            // check that players has controller 
            controller = Finder.FindComponent<Controller>(collision.gameObject);
            if (controller == null)
            {
                ErrorHandler.Error("Controller not found for player " + collision.gameObject.name);
                return false;
            }

            return true;
        }

        protected virtual void OnCollisionController(Controller controller)
        {
            // hit the player
            OnHit(controller);
        }

        #endregion


        #region Target & Position

        protected override void SetTarget(Vector3 target)
        {
            if (m_SpellData.SpellSpawn == ESpellSpawn.Ground)
                target.y = m_SpellData.TargetOffset.Y;

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