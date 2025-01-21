using Data;
using Enums;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level, string parent)
        {
            base.Initialize(clientId, target, spellName, level, parent);

            transform.localScale = new Vector3(m_SpellData.Size, m_SpellData.Size, m_SpellData.Size);

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
        /// </summary>q
        /// <param name="collision"></param>
        protected virtual bool CheckCollision(Collider2D collision, out Controller controller)
        {
            controller = null;

            if (!IsServer)
                return false;

            if (collision.gameObject.layer != LayerMask.NameToLayer("Player") && collision.gameObject.layer != LayerMask.NameToLayer("Structure"))
                return false;

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

        protected void CreateCollisionCircle()
        {
            // Check for collisions within a circle with variableRadius radius
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, m_Radius.Value);

            // Gat all controllers touched by the 2D collision circle
            var hitControllers = new List<Controller>();
            foreach (Collider2D collider in colliders)
            {
                if (!CheckCollision(collider, out Controller controller))
                    continue;
                hitControllers.Add(controller);
            }

            // if "ApplyIfNotHitting" : set hitControllers to be the list of ALL controllers NOT HIT
            if (m_SpellData.ApplyIfNotHitting)
            {
                var allControllers = m_SpellData.IsEnemyTarget ? GameManager.Instance.GetAllEnemies(m_Controller.Team) : GameManager.Instance.GetAllAllies(m_Controller.Team);
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