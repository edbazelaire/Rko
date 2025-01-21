using Assets.Scripts.Game;
using Assets.Scripts.Managers.Sound;
using Data;
using Enums;
using Game.Loaders;
using Game.Spells.SpecialEffects;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Spells
{
    public class Spell : NetworkBehaviour
    {
        #region Members

        // ========================================================================================================
        // Constants
        const string c_GraphicsContainer = "GraphicsContainer";

        // ========================================================================================================
        // Actions
        /// <summary> static action allowing SpellGFX instantiated on client side from the SpellHandler to make the connection the sell on spawn </summary>
        public static Action<Spell>         OnSpellSpawn;
        /// <summary> action allowing SpellGFX to react at spell events </summary>
        public Action<ESpellEvent>          OnSpellEvent;

        // ========================================================================================================
        // Data
        protected SpellData         m_BaseSpellData;
        SpellData m_SpellData  => m_BaseSpellData;
        protected Controller        m_Controller;
        protected Vector3           m_Target;
        protected GameObject        m_GraphicsContainer;
        protected GameObject        m_Graphics;

        /// <summary> in case of persistance of graphisme, allows to stop spell behavior </summary>
        protected bool              m_IsOver = false;   
        /// <summary> timer delaying the end of spell graphismes after end of spell </summary>
        protected float             m_PersistanceTimer;
        /// <summary> counter of remaining number of target that this spell can hit </summary>
        protected List<ulong>       m_HittedPlayerId;
        /// <summary> on cast prefabs (spawn on cast) </summary>
        protected List<GameObject>  m_OnCastPrefabs;

        // ========================================================================================================
        // Events
        public Action OnSpellEndedEvent;

        // ========================================================================================================
        // Public Accessors
        public SpellData    SpellData   => m_SpellData;
        public Controller   Controller  => m_Controller;
        public Vector3      Target      => m_Target;
        public GameObject   Graphics    => m_Graphics;

        public bool IsAutoAttack => m_SpellData.Name == m_Controller.SpellHandler.AutoAttack.ToString();

        #endregion


        #region Init & End

        public override void OnNetworkSpawn() { }

        public override void OnDestroy()
        {
            base.OnDestroy();

            // call an end on client side (this method happens localy so no need to get throught RPC)
            CallSpellEvent(ESpellEvent.OnEnd);

            // destroy data (to avoid charging memory)
            Destroy(m_SpellData);

            // unregister from any listeners
            UnRegisterListeners();
        }

        protected virtual void SetSpellData(string spellName, int level, string parent = null)
        {
            m_BaseSpellData = SpellLoader.GetSpellData(spellName, level);

            if (! parent.IsNullOrEmpty())
                m_BaseSpellData.SetParent(parent);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="spellName"></param>
        public virtual void Initialize(ulong clientId, Vector3 target, string spellName, int level, string parent)
        {
            m_Controller = GameManager.Instance.GetPlayer(clientId);
            SetSpellData(spellName, level, parent);
            m_HittedPlayerId    = new List<ulong>();

            // add extra effects (damages bonus, on hit effects, ...) that the controller has at time of casting
            AddExtraEffects();

            // re-order the effects by Priority
            m_SpellData.AllyStateEffects = m_SpellData.ReOrderStateEffects(m_SpellData.AllyStateEffects);
            m_SpellData.EnemyStateEffects = m_SpellData.ReOrderStateEffects(m_SpellData.EnemyStateEffects);

            // set the target
            SetTarget(target);

            // post-processing for childrens
            ApplyPostProcessing();

            // Add special component
            if (IsServer)
                AddSpecialComponent();

            // initialize graphics of the spell (with delay if has any)
            InitGraphics();

            // register listeners
            RegisterListeners();

            // call event that spell has spawn
            OnSpellSpawn?.Invoke(this);
            CallSpellEvent(ESpellEvent.OnSpawn);
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void End()
        {
            if (m_IsOver)
                return;

            // set is over to true (in case of persistance of graphisme, to stop spell behavior)
            m_IsOver = true;

            // ending effect
            SpawnOnHitPrefab();

            // destroy the spell game object
            StartCoroutine(DestroySpell());

            ErrorHandler.Log("End of spell : " + m_SpellData, ELogTag.Spells);
        }

        public virtual bool TryEnd()
        {
            if (m_IsOver)
                return false;

            End();
            return true;
        }

        public virtual IEnumerator DestroySpell()
        {
            // delay destruction of spell graphismes for visual purpuses
            m_PersistanceTimer = m_SpellData.PersistanceAfterEnd;

            while (m_PersistanceTimer > 0) 
            {
                m_PersistanceTimer -= Time.deltaTime;
                yield return null;
            }

            // destroy the spell
            Destroy(gameObject);
        }

        #endregion


        #region Client RPCs

        /// <summary>
        /// Client is initialized with same data as server to create a previsualisation of the spell
        /// </summary>
        /// <param name="target"></param>
        /// <param name="spellType"></param>
        [ClientRpc]
        public void InitializeClientRpc(ulong clientId, Vector3 target, string spellName, int level)
        {
            if (IsHost)
                return;
            Initialize(clientId, target, spellName, level, "");
        }

        #endregion


        #region On Init

        /// <summary>
        /// Allow children to apply post processing effects (change target, position, ...) during initialization
        /// </summary>
        protected virtual void ApplyPostProcessing() { }

        /// <summary>
        /// If spell has a special component (server side) : add it
        /// </summary>
        protected virtual void AddSpecialComponent() 
        {
            if (!IsServer)
                return;

            // Construct the full type name including the namespace
            string spellComponentFullName = $"Game.Spells.SpecialEffects.{m_SpellData.Name}";

            // Try to get the Type from the fully qualified name
            Type componentType = Type.GetType(spellComponentFullName);

            // Check if the Type is valid and is a MonoBehaviour
            if (componentType == null || !componentType.IsSubclassOf(typeof(SpecialEffect)))
                return;

            // Add the component to this GameObject
            SpecialEffect specialEffect = (SpecialEffect)gameObject.AddComponent(componentType);
            specialEffect.Initialize(m_SpellData.Level);
        }

        /// <summary>
        /// Instantiate the graphics of the spell
        /// </summary>
        protected virtual void InitGraphics()
        {
            m_GraphicsContainer = Finder.Find(gameObject, c_GraphicsContainer, throwError: false);
            if (m_GraphicsContainer == null)
                m_GraphicsContainer = new GameObject(c_GraphicsContainer);

            if (m_SpellData.Graphics != null)
            {
                m_Graphics = Instantiate(m_SpellData.Graphics, m_GraphicsContainer.transform);
                SwapColliders(m_Graphics);

                var audioSource = Finder.FindComponent<AudioSource>(m_Graphics);
                if (audioSource != null)
                    SoundFXManager.AdjustVolume(ref audioSource);
            }

            transform.localScale = new Vector3(m_SpellData.Size, m_SpellData.Size, m_SpellData.Size);

            if (m_SpellData.PermanantSoundFX != null)
                SoundFXManager.PlaySoundFXClip(m_SpellData.PermanantSoundFX, transform);
        }

        protected virtual Collider2D CopyCollider(Collider2D collider)
        {
            // destroy the collider on the Spell before adding the new one
            Destroy(this.GetComponent<Collider2D>());

            // Get the type of the original collider
            Type colliderType = collider.GetType();

            // Add a new collider of the same type to this GameObject
            Collider2D newCollider = this.gameObject.AddComponent(colliderType) as Collider2D;

            // Copy properties from the original collider to the new one
            if (newCollider != null)
            {
                CopyColliderProperties(collider, newCollider);
            }

            return newCollider;
        }

        /// <summary>
        /// Swap default Collider with Graphics Collider if it has one
        /// </summary>
        protected virtual void SwapColliders(GameObject graphics)
        {
            // Check if the graphics GameObject has a enabled Collider2D component
            Collider2D graphicsCollider = graphics.GetComponent<Collider2D>();
            if (graphicsCollider == null || ! graphicsCollider.enabled)
                return;

            // copy properties of the graphics collider on this object
            CopyCollider(graphicsCollider);

            // Destroy the original collider on the graphics GameObject
            Destroy(graphicsCollider);
        }

        /// <summary>
        /// Copies properties from one collider to another.
        /// </summary>
        /// <param name="source">The original collider to copy from.</param>
        /// <param name="destination">The new collider to copy to.</param>
        private void CopyColliderProperties(Collider2D source, Collider2D destination)
        {
            if (source == null || destination == null)
                return;

            // General properties
            destination.isTrigger = source.isTrigger;
            destination.offset = source.offset;

            // Specific properties for BoxCollider2D
            if (source is BoxCollider2D sourceBoxCollider && destination is BoxCollider2D destinationBoxCollider)
            {
                destinationBoxCollider.size = sourceBoxCollider.size;
            }
            // Specific properties for CircleCollider2D
            else if (source is CircleCollider2D sourceCircleCollider && destination is CircleCollider2D destinationCircleCollider)
            {
                destinationCircleCollider.radius = sourceCircleCollider.radius;
            }
            // Specific properties for PolygonCollider2D
            else if (source is PolygonCollider2D sourcePolygonCollider && destination is PolygonCollider2D destinationPolygonCollider)
            {
                destinationPolygonCollider.points = sourcePolygonCollider.points;
            }
            // Specific properties for EdgeCollider2D
            else if (source is EdgeCollider2D sourceEdgeCollider && destination is EdgeCollider2D destinationEdgeCollider)
            {
                destinationEdgeCollider.points = sourceEdgeCollider.points;
            }
            // Add more collider types if necessary
        }

        #endregion


        #region Inherited Manipulators  

        /// <summary>
        /// 
        /// </summary>
        protected virtual void Update()
        {
            if (m_IsOver)
                return;

            UpdateMovement();
        }

        #endregion


        #region Protected Manipulators

        /// <summary>
        /// Update the position of the spell and [SERVER] check if the spell has reached its max distance
        /// </summary>
        protected virtual void UpdateMovement() { }

        /// <summary>
        /// Check if a Player has been hit
        /// </summary>
        /// <param name="controller"></param>
        protected virtual void OnHit(Controller controller)
        {
            // not alive : skip
            if (!controller.Life.IsAlive)
                return;

            // already hit
            if (m_HittedPlayerId.Contains(controller.PlayerId))
                return;

            // apply effects on ally or enemy : if none, skip
            if (! CheckHitEnemy(controller) && ! CheckHitAlly(controller))
                return;

            // apply force
            if (m_SpellData.Force.IsActive)
                controller.Movement.AddForce(m_SpellData.Force);

            // if spell has "OnHit" GFX : call on CLIENT that spell has touched something
            if (m_SpellData.HasGfxEventAt(ESpellEvent.OnHit, checkEnd: false))
                CallSpellEventClientRPC(ESpellEvent.OnHit, controller.PlayerId);

            // add plyer id to list of hitted players
            m_HittedPlayerId.Add(controller.OwnerClientId);

            // energy gain
            m_Controller.EnergyHandler.AddEnergy(m_SpellData.EnergyGain);

            // update hit count
            if (m_HittedPlayerId.Count <= m_SpellData.MaxHit && m_SpellData.MaxHit > 0)
                End();
        }

        /// <summary>
        /// Check if ability hit an enemy
        /// </summary>
        /// <param name="targetController"> controller of hit target </param>
        /// <returns></returns>
        protected virtual bool CheckHitEnemy(Controller targetController)
        {
            // Target is Ally - return
            if (targetController.Team == m_Controller.Team)
                return false;

            // no base Damages, StateEffects or OnHit effects - return
            if (m_SpellData.Damage <= 0 && m_SpellData.ExecutionDamages <= 0 && m_SpellData.EnemyStateEffects.Count == 0 && m_SpellData.OnHit.Count == 0)
                return false;

            // check if target has counter(s)
            if (targetController.CounterHandler.CheckCounters(this))
                return false;

            HitEnemy(targetController);

            return true;
        }

        void HitEnemy(Controller targetController)
        {
            // apply spell base damages on target
            if (m_SpellData.Damage > 0)
                ApplyDamagesOnTarget(GetBoostedDamages(targetController), targetController);

            // apply execution damages on target
            if (m_SpellData.ExecutionDamages > 0)
                ApplyDamagesOnTarget(GetBoostedExecutionDamages(targetController), targetController);

            // apply state effects specifics to enemies
            ApplyEnemyStateEffects(targetController);
        }

        void ApplyDamagesOnTarget(int damages, Controller targetController)
        {
            // get final damages after shields and resistances
            int finalDamages = targetController.Life.Hit(damages, m_Controller.PlayerId, m_SpellData.Parent, m_SpellData.SpellCategory);

            ErrorHandler.Log(m_SpellData.Name + " : " + finalDamages, ELogTag.Spells);

            // apply lifesteal if any (remove 1 because floats values are always based on 1 as default value)
            float lifeSteal = SpellData.LifeSteal + Mathf.Max(0f, m_Controller.StateHandler.GetFloat(EStateEffectProperty.BonusLifeSteal) - 1);
            if (lifeSteal > 0 && finalDamages > 0)
            {
                m_Controller.Life.Heal((int)Mathf.Round(lifeSteal * finalDamages), m_Controller.PlayerId, m_SpellData.Parent, m_SpellData.SpellCategory);
            }
        }

        /// <summary>
        /// Check if ability hit an ally
        /// </summary>
        /// <param name="controller"> controller of hit target </param>
        /// <returns></returns>
        protected virtual bool CheckHitAlly(Controller controller)
        {
            if (controller.Team != m_Controller.Team)
                return false;

            bool test = false;
            if (m_SpellData.Heal > 0)
            {
                controller.Life.Heal(m_SpellData.Heal, m_Controller.PlayerId, m_SpellData.Parent, m_SpellData.SpellCategory);
                test = true;
            }

            if (m_SpellData.Shield > 0)
            {
                controller.Life.AddShield(m_SpellData.Shield, m_Controller.PlayerId, m_SpellData.Parent, m_SpellData.SpellCategory);
                test = true;
            }

            if (m_SpellData.AllyStateEffects.Count > 0)
            {
                ApplyAllyStateEffects(controller);
                test = true;
            }
            
            return test;
        }

        public virtual int GetBoostedDamages(Controller targetController)
        {
            var damages = m_SpellData.Damage;

            if (damages <= 0)
                return 0;

            if (m_SpellData.StateEffectStackFactor != EStateEffect.None)
            {
                damages *= targetController.StateHandler.GetStacks(m_SpellData.StateEffectStackFactor);
            }

            return m_Controller.StateHandler.ApplyBonusDamages(damages, targetController);
        }

        public virtual int GetBoostedExecutionDamages(Controller target)
        {
            if (m_SpellData.ExecutionDamages <= 0)
                return 0;

            return (int)Math.Round(m_Controller.StateHandler.ApplyBonusDamages(m_SpellData.ExecutionDamages, target) * (1 - target.Life.PercHp));
        }

        protected virtual void AddExtraEffects()
        {
            bool IsAutoAttack = m_SpellData.Name == m_Controller.SpellHandler.AutoAttack.ToString();

            // if spell is AutoAttack & controller has a "AutoAttackRune" : add effects of the rune to the spell
            m_Controller.StateHandler.AddExtraEffects(ref m_BaseSpellData, IsAutoAttack);
        }

        /// <summary>
        /// Set the value of the target, update direction and rotation
        /// </summary>
        /// <param name="target"></param>
        protected virtual void SetTarget(Vector3 target)
        {
            m_Target = target;
        }

        #endregion


        #region State Effects

        /// <summary>
        /// Spawn prefabs that procs on hitting a target
        /// </summary>
        /// <param name="targetController"></param>
        protected virtual void SpawnOnHitPrefab()
        {
            if (! IsServer)
                return;

            m_SpellData.SpawnOnHitPrefab(OwnerClientId, transform.position, transform.position);
        }

        protected virtual void ApplyStateEffects(Controller targetController, List<SStateEffectData> stateEffects)
        {
            if (!IsServer)
                return;

            if (!targetController.Life.IsAlive)
                return;

            foreach (var effect in stateEffects)
            {
                targetController.StateHandler.AddStateEffect(effect, m_Controller, m_SpellData.Level);
            }
        }

        /// <summary>
        /// Apply on hit effects targetting enemies
        /// </summary>
        /// <param name="targetController"></param>
        protected virtual void ApplyEnemyStateEffects(Controller targetController)
        {
            ApplyStateEffects(targetController, m_SpellData.EnemyStateEffects);
        }

        /// <summary>
        /// Apply on hit effects targetting allies
        /// </summary>
        /// <param name="targetController"></param>
        protected virtual void ApplyAllyStateEffects(Controller targetController)
        {
            ApplyStateEffects(targetController, m_SpellData.AllyStateEffects);
        }

        #endregion


        #region Spell Event

        /// <summary>
        /// From SERVER to CLIENT, call for the CallSpellEvent() method
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <param name="clientID"></param>
        [ClientRpc]
        public virtual void CallSpellEventClientRPC(ESpellEvent spellEvent)
        {
            CallSpellEvent(spellEvent, null);
        }

        /// <summary>
        /// From SERVER to CLIENT, call for the CallSpellEvent() method
        ///     -> surcharge with a clientID (if necessary)
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <param name="clientID"></param>
        [ClientRpc]
        public virtual void CallSpellEventClientRPC(ESpellEvent spellEvent, ulong clientID)
        {
            CallSpellEvent(spellEvent, GameManager.Instance.GetPlayer(clientID));
        }

        /// <summary>
        /// Spell event : instantiate/destroy the graphics matching the event of the spell
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <param name="targetController"></param>
        protected virtual void CallSpellEvent(ESpellEvent spellEvent, Controller targetController = null)
        {
            if (gameObject == null || gameObject.IsDestroyed())
            {
                ErrorHandler.Warning("Unable to display graphism for spell event " + spellEvent + " : GameObject is destroyed");
                return;
            }

            if (m_SpellData == null)
            {
                ErrorHandler.Warning("Unable to display graphism for spell event " + spellEvent + " : SpellData is null");
                return;
            }

            foreach (var spawnPrefab in m_SpellData.SpellEventActions)
            {
                if (spawnPrefab.GFXLifetime.StartSpellPart != spellEvent)
                    continue;

                spawnPrefab.Spawn(m_Controller, m_SpellData, this, null, targetController, transform.position, m_Target);
            }

            OnSpellEvent?.Invoke(spellEvent);
        }

        #endregion


        #region Targetting

        /// <summary>
        /// Get Controller for the target
        /// </summary>
        /// <returns></returns>
        protected virtual Controller GetTargetController()
        {
            if (! SpellData.IsAutoTarget)
                return null;

            return m_SpellData.GetTargetController(m_Controller.PlayerId);
        }

        protected virtual void RecalculateTarget(ref Transform baseTarget)
        {
            Controller targetController = GetTargetController();
            if ( targetController != null )
                baseTarget = targetController.transform;
        }

        #endregion


        #region Tools

        protected bool TryGetController(Collider2D collider, out Controller controller)
        {
            controller = null;
            if (collider.gameObject.layer != LayerMask.NameToLayer("Player"))
                return false;

            // check that players has controller 
            controller = Finder.FindComponent<Controller>(collider.gameObject);
            if (controller == null)
            {
                ErrorHandler.Error("Controller not found for player " + collider.gameObject.name);
                return false;
            }

            return true;
        }

        #endregion


        #region Listeners

        protected virtual void RegisterListeners() 
        {
            if(GameManager.Exists)
                GameManager.Instance.State.OnValueChanged += OnGameStateChanged;
        }

        protected virtual void UnRegisterListeners() 
        {
            if (GameManager.Exists)
                GameManager.Instance.State.OnValueChanged -= OnGameStateChanged;
        }

        void OnGameStateChanged(EGameState oldValue, EGameState state)
        {
            if (state >= EGameState.GameOver)
                Destroy(gameObject);
        }

        #endregion


        #region Debug

        public virtual void DebugMessage()
        {
            Debug.Log("Spell " + m_SpellData.name);
            Debug.Log("     + ClientId " + OwnerClientId);
            Debug.Log("     + Target " + m_Target);
        }

        #endregion
    }
}