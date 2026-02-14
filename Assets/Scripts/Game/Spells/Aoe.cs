using Data;
using Enums;
using Game.NetworkStructures;
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
        List<Controller> m_AllControllersBuffer = new List<Controller>();

        public IReplicatedVar<float> Radius { get; protected set; }

        public virtual float Duration => 0;

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
            
            if (m_IsOver)
                return;

            transform.localScale = new Vector3(m_SpellData.Size, m_SpellData.Size, m_SpellData.Size);

            if (!IsServer)
                return;

            // setup radius and timer
            Radius.Value          = m_SpellData.Size / 2;

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
        }

        #endregion


        #region Collision Manipulators

        /// <summary>
        /// Create a collision circle of the spell's radius that will apply the spells effect for each allowed Controllers inside
        /// </summary>
        protected virtual void CreateCollisionCircle()
        {
            if (!IsServer)
                return;

            CreateCollisionCircleOnPosition(transform.position);
        }

        protected virtual void CreateCollisionCircleOnPosition(Vector3 position)
        {
            // setup layer filter 
            var filter = Physics2DQueries.BuildFilter(TargetHelper.DEFAULT_LAYER_MASK);

            // Check for collisions within a circle with variableRadius radius
            Collider2D[] hits = new Collider2D[32];
            int count = Physics2DQueries.OverlapAtPosition(m_Collider, position, m_SpellData.Size, filter, hits);

            ErrorHandler.Log(() => $"AOE at {position} hit {count} colliders.", ELogTag.Aoe);

            // Gat all controllers touched by the 2D collision circle
            var hitControllers = new List<Controller>();
            for (int i = 0; i < count; i++)
            {
                if (!CheckCollision(hits[i], out Controller controller))
                    continue;

                hitControllers.Add(controller);
                ErrorHandler.Log(() => $" → Hit {hits[i].name}", ELogTag.Aoe);
            }

            // if "ApplyIfNotHitting" : set hitControllers to be the list of ALL controllers NOT HIT
            if (m_SpellData.ApplyIfNotHitting)
            {
                if (m_SpellData.IsEnemyTarget)
                    GameManager.Instance.GetAllEnemies(m_Caster.Team, m_AllControllersBuffer);
                else
                    GameManager.Instance.GetAllAllies(m_Caster.Team, m_AllControllersBuffer);

                hitControllers = m_AllControllersBuffer
                    .Where(controller => !hitControllers.Any(hitController => hitController.PlayerId == controller.PlayerId))
                    .ToList();
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

            if (! m_SpellData.OverridesSpawnPosition)
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