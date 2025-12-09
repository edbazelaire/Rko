using Assets.Scripts.Game;
using Assets.Scripts.Game.Pool;
using Assets.Scripts.Managers.Sound;
using Data;
using Data.DataStructures.SpellSubStructures;
using Enums;
using Game.Loaders;
using Game.NetworkStructures;
using Game.Spells.SpecialEffects;
using MyBox;
using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using Tools.Helpers;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;


namespace Game.Spells
{
    public class Spell : PooledNetworkBehaviour
    {
        #region Members

        // ========================================================================================================
        // Constants
        const string c_GraphicsContainer = "GraphicsContainer";
        List<string> AFTER_EFFECTS => new(){ EStateEffect.Frozen.ToString() };

        // ========================================================================================================
        // Actions
        /// <summary> static action allowing SpellGFX instantiated on client side from the SpellHandler to make the connection the sell on spawn </summary>
        public static Action<Spell>         OnSpellSpawn;
        /// <summary> action allowing SpellGFX to react at spell events </summary>
        public Action<ESpellEvent>          OnSpellEvent;

        // ========================================================================================================
        // Data
        protected SpellData         m_BaseSpellData;
        SpellData m_SpellData => m_BaseSpellData;
        protected Controller        m_Caster;
        protected Collider2D        m_Collider;
        protected Vector3           m_Target;
        protected Vector3           m_RelocationTargetPos;
        protected NetworkObject     m_NetworkObjectComponent;
        protected GameObject        m_GraphicsContainer;
        protected GameObject        m_Graphics;
        protected int               m_Team;

        /// <summary> in case of persistance of graphisme, allows to stop spell behavior </summary>
        protected bool              m_IsOver = false;
        /// <summary> timer counting down the remaining spell duration time </summary>
        protected float             m_DurationTimer;
        /// <summary> timer delaying the end of spell graphismes after end of spell </summary>
        protected float             m_PersistanceTimer;
        /// <summary> counter of remaining number of target that this spell can hit </summary>
        protected List<ulong>       m_HittedPlayerId;
        /// <summary> on cast prefabs (spawn on cast) </summary>
        protected List<GameObject>  m_OnCastPrefabs;
        /// <summary> is this spell currently checking relocation ? </summary>
        protected bool              m_IsCheckingRelocation;

        // ========================================================================================================
        // Events
        public Action OnSpellEndedEvent;

        // ========================================================================================================
        // Public Accessors
        public SpellData    SpellData           => m_SpellData;
        public Controller   Caster              => m_Caster;
        public Collider2D   Collider            => m_Collider;
        public Vector3      Target              => m_Target;
        public GameObject   Graphics            => m_Graphics;
        public GameObject   GraphicsContainer   => m_GraphicsContainer;
        public int          Team                => m_Team;
        public bool         IsOver              => m_IsOver;

        public bool IsAutoAttack => m_Caster.SpellHandler.IsAutoAttack(m_SpellData);

        #endregion


        #region Init & End

        public override void OnSpawned() 
        {
            base.OnSpawned();

            m_NetworkObjectComponent = gameObject.GetComponent<NetworkObject>();
            m_IsOver = false;
            m_Collider = this.GetComponent<Collider2D>(); 

            // destroy data (to avoid charging memory)
            if (m_BaseSpellData != null)
            {
                Destroy(m_BaseSpellData);
                m_BaseSpellData = null;
            }
        }

        public override void OnDespawned()
        {
            base.OnDespawned();

            // make sure is over is called
            m_IsOver = true;

            // call an end on client side (this method happens localy so no need to get throught RPC)
            CallSpellEvent(ESpellEvent.OnOver);

            // unregister from any listeners
            UnRegisterListeners();

            // deactivate game object (to make sure it happens after network despawn)
            gameObject.SetActive(false);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (GameManager.IsGameOver)
                return;

            OnNetworkDespawn();
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
        public virtual void Initialize(ulong clientId, Vector3 target, SpellData spellData)
        {
            m_Caster                = GameManager.Instance.GetPlayer(clientId);
            m_Team                  = m_Caster.Team;
            m_HittedPlayerId        = new List<ulong>();
            m_RelocationTargetPos   = default;
            m_DurationTimer         = spellData.Duration;

            // setup spell data
            m_BaseSpellData = spellData.Clone(level: spellData.Level);

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

            // if is channeling : check that caster 
            if (m_SpellData.IsChanneling && ! m_Caster.SpellHandler.TryStartChanneling(this))
            {
                End();
                return;
            }

            // initialize solid body
            InitSolidBody();

            // initialize size of the object
            InitSize();

            // initialize graphics of the spell (with delay if has any)
            InitGraphics();

            // register listeners
            RegisterListeners();

            // call event that spell has spawn
            CallSpellEvent(ESpellEvent.OnSpawn);

            // send event to the Analytics
            if (Enum.TryParse(m_SpellData.Name, out ESpell spell))
                GameAnalyticsManager.Instance.IncreaseCounter(m_Caster.IsSpawn ? m_Caster.SpawnOwner.PlayerId : m_Caster.PlayerId, spell.ToString());

            // -- also increase counter of TriggerEffect that procced the effect
            if (m_SpellData.Parent != m_SpellData.Name && (SpellLoader.IsRune(m_SpellData.Parent) || SpellLoader.IsPowerUp(m_SpellData.Parent)))
                GameAnalyticsManager.Instance.IncreaseCounter(m_Caster.IsSpawn ? m_Caster.SpawnOwner.PlayerId : m_Caster.PlayerId, m_SpellData.Parent);
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

            // call on end event
            CallSpellEvent(ESpellEvent.OnEnd);

            // ending effect
            SpawnOnHitPrefab();

            // terminate spell 
            Terminate();
        }

        /// <summary>
        /// Terminate a spell (just call end of spell with/without graphics)
        /// </summary>
        /// <param name="instant"></param>
        public virtual void Terminate(bool instant = false)
        {
            // destroy the spell game object
            if (instant)
            {
                ReturnObjectToPool();
                return;
            }
            
            StartCoroutine(DestroySpell());
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

            if (!GameManager.Exists)
            {
                Destroy(this);
                yield break;
            }

            // return spell to the PoolManager
            ReturnObjectToPool();
        }

        void ReturnObjectToPool()
        {
            if (GameManager.Instance.IsOfflineMode)
                PoolManager.ReturnObject(gameObject, checkSpawnLogic: true);
            else
                PoolManager.ReturnObject(m_NetworkObjectComponent);
        }

        #endregion


        #region Client RPCs

        /// <summary>
        /// Client is initialized with same data as server to create a previsualisation of the spell
        /// </summary>
        /// <param name="target"></param>
        /// <param name="spellType"></param>
        [ClientRpc]
        public void InitializeClientRpc(ulong clientId, Vector2Short targetPos, FixedString32Bytes spellName, byte level)
        {
            if (IsHost)
                return;
            var spellData = SpellLoader.GetSpellData(spellName.ToString(), level);
            Initialize(clientId, targetPos, spellData);
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
        /// Adjust the size of the spell depending on expected size and parent
        /// </summary>
        protected virtual void InitSize()
        {
            // set size of the 
            Vector3 parentScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            Vector3 targetScale = Vector3.one * m_SpellData.Size;
            transform.localScale = new Vector3(
                parentScale.x != 0 ? targetScale.x / parentScale.x : targetScale.x,
                parentScale.y != 0 ? targetScale.y / parentScale.y : targetScale.y,
                parentScale.z != 0 ? targetScale.z / parentScale.z : targetScale.z
            );
        }

        /// <summary>
        /// Initialize Module SolidBody
        /// </summary>
        protected virtual void InitSolidBody()
        {
            if (! m_SpellData.HasSolidBody)
                return;

            m_SpellData.MSolidBody.Initialize(this, m_SpellData.Level);
        }

        /// <summary>
        /// Instantiate the graphics of the spell
        /// </summary>
        protected virtual void InitGraphics()
        {
            // Initialize Graphics Container
            m_GraphicsContainer = Finder.Find(gameObject, c_GraphicsContainer, throwError: false);
            if (m_GraphicsContainer == null)
                m_GraphicsContainer = new GameObject(c_GraphicsContainer);
            UIHelper.CleanContent(m_GraphicsContainer);

            // Spawn GFX and check if there is a special Collider
            bool replacedCollider = false;
            if (m_SpellData.Graphics != null)
            {
                m_Graphics = PoolManager.Pool(m_SpellData.Graphics, m_GraphicsContainer.transform, activate: false);
                m_Graphics.transform.localScale     = Vector3.one;
                m_Graphics.transform.localPosition  = Vector3.zero;
                m_Graphics.transform.localRotation  = Quaternion.identity;
                m_Graphics.SetActive(true);
                replacedCollider = SwapColliders(m_Graphics);

                var audioSource = Finder.FindComponent<AudioSource>(m_Graphics);
                if (audioSource != null)
                    SoundFXManager.AdjustVolume(ref audioSource);
            } 

            // NO COLLIDER REPLACED -> reset to default CircleCollider2D
            if (!replacedCollider)
            {
                if (m_Collider != null)
                    Destroy(m_Collider);
                m_Collider = transform.AddComponent<CircleCollider2D>();
                m_Collider.isTrigger = true;
                m_Collider.offset = new Vector2(0f, 0f);
                (m_Collider as CircleCollider2D).radius = 0.5f;
            }

            if (m_SpellData.PermanantSoundFX != null)
                SoundFXManager.PlaySoundFXClip(m_SpellData.PermanantSoundFX, transform);
        }

        protected virtual Collider2D CopyCollider(Collider2D collider)
        {
            // destroy the collider on the Spell before adding the new one
            Destroy(m_Collider);

            // Get the type of the original collider
            Type colliderType = collider.GetType();

            // Add a new collider of the same type to this GameObject
            Collider2D newCollider = gameObject.AddComponent(colliderType) as Collider2D;

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
        protected bool SwapColliders(GameObject graphics)
        {
            // Check if the graphics GameObject has a enabled Collider2D component
            Collider2D graphicsCollider = graphics.GetComponent<Collider2D>();
            if (graphicsCollider == null || ! graphicsCollider.enabled)
                return false;

            // copy properties of the graphics collider on this object
            m_Collider = CopyCollider(graphicsCollider);

            // Destroy the original collider on the graphics GameObject
            Destroy(graphicsCollider);
            return true;
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
            // Specific properties for CircleCollider2D
            else if (source is CapsuleCollider2D sourceCapsuleCollider && destination is CapsuleCollider2D destinationCapsuleCollider)
            {
                destinationCapsuleCollider.size = sourceCapsuleCollider.size;
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
            else
            {
                ErrorHandler.Warning("Unhanlded case : " + source.GetType());
            }
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

            UpdateTimer();

            if (m_IsOver)
                return;

            UpdateMovement();
            UpdateRelocation();
        }

        #endregion


        #region Movement & Relocation

        protected virtual void UpdateTimer()
        {
            if (m_IsOver || m_SpellData == null)
                return;

            // if inifite zone, do nothing
            if (m_SpellData.Duration < 0f)
                return;

            m_DurationTimer -= Time.deltaTime;
            if (m_DurationTimer < 0f)
            {
                End();
                return;
            }
        }

        /// <summary>
        /// Update the position of the spell and [SERVER] check if the spell has reached its max distance
        /// </summary>
        protected virtual void UpdateMovement() 
        {
            if (!m_SpellData.IsFollowing)
                return;

            transform.position = m_SpellData.MFollowing.UpdatePosition(transform.position, m_Caster.PlayerId, null);
        }

        /// <summary>
        /// If spell has relocation, update its position
        /// </summary>
        protected virtual void UpdateRelocation() 
        {
            // no need to update pos if not requtested pos is asked, or if pos already reached
            if (m_RelocationTargetPos == default || transform.position.x == m_RelocationTargetPos.x)
                return;

            int teamFactor = m_Caster.Team == 0 ? 1 : -1;

            // calculate expected position at that frame
            var xPos = m_RelocationTargetPos.x;
            var direction = transform.position.x > xPos ? -1 : 1;
            var pos = transform.position + new Vector3(direction * m_SpellData.SpellRelocation.Speed * teamFactor * Time.deltaTime, 0f, 0f);

            // clamp position to max/min allowed position
            if (direction < 0 && pos.x < xPos)
                pos.x = xPos;
            else if (direction > 0 && pos.x > xPos)
                pos.x = xPos;

            // clamp position to target area
            if (m_SpellData.ClampTargetPos)
                m_SpellData.ClampTargetX(ref pos, m_Caster.PlayerId);

            // set new position
            transform.position = pos;
        }

        #endregion


        #region Hit Methods

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
            if (m_SpellData.Force.IsActive && TargetHelper.IsAllowedTarget(controller, m_Caster, m_SpellData.Force.Targets))
                controller.Movement.AddForce(m_SpellData.Force);

            // if spell has "OnHit" GFX : call on CLIENT that spell has touched something
            CallSpellEvent(ESpellEvent.OnHit, controller);

            // energy gain (if hitting not structure object)
            if (! controller.CharacterData.IsStructure)
                m_Caster.EnergyHandler.AddEnergy(m_SpellData.EnergyGain);

            // add player id to list of hitted players
            AddHittedPlayer(controller.PlayerId);
        }

        /// <summary>
        /// Check if ability hit an enemy
        /// </summary>
        /// <param name="targetController"> controller of hit target </param>
        /// <returns></returns>
        protected virtual bool CheckHitEnemy(Controller targetController)
        {
            // Target is Ally - return
            if (targetController.Team == m_Caster.Team)
                return false;

            // no base Damage, StateEffects or OnHit effects - return
            if (m_SpellData.Damages.IsNullOrEmpty() && m_SpellData.Heal <= 0 && m_SpellData.EnemyStateEffects.Count == 0 && m_SpellData.OnHit.Count == 0)
                return false;

            // check if target has counter(s)
            if (targetController.CounterHandler != null && targetController.CounterHandler.CheckCounters(this))
                return false;

            HitEnemy(targetController);

            return true;
        }

        public void HitEnemy(Controller targetController)
        {
            // split state effects application into effects that are applied before / after damages
            (List<SStateEffectData> beforeEffects, List<SStateEffectData> afterEffects) = SplitStateEffectsPriority(m_SpellData.EnemyStateEffects);

            // apply state effects specifics to enemies that are applied BEFORE damages
            ApplyStateEffects(targetController, beforeEffects);

            // apply all damages
            ApplyDamages(targetController);

            // apply all heals
            ApplyHeal(targetController);

            // apply state effects specifics to enemies that are applied AFTER damages
            ApplyStateEffects(targetController, afterEffects);
        }

        void ApplyDamages(Controller targetController)
        {
            if (m_SpellData.Damages.IsNullOrEmpty())
                return;

            foreach (SDamage damage in m_SpellData.Damages)
            {
                ApplyDamageOnTarget(damage, targetController);
            }
        }

        void ApplyDamageOnTarget(SDamage damage, Controller targetController)
        {
            // calculate damage
            int value = m_Caster.StateHandler.ApplyBonusDamage(
                damage.Get(m_SpellData.Level, m_Caster, targetController),
                targetController,
                damageCategory:     damage.DamageCategory,
                hitCategory:        damage.HitCategory,
                specialCondition:   m_SpellData.Parent
            );

            // get final damages after shields and resistances
            int finalDamage = targetController.Life.Hit(value, m_Caster.PlayerId, m_SpellData.Parent, damage.DamageCategory, damage.HitCategory);

            ErrorHandler.Log(() => m_SpellData.Name + " : " + finalDamage, ELogTag.Spells);

            // apply lifesteal if any (remove 1 because floats values are always based on 1 as default value)
            float lifeSteal = SpellData.LifeSteal + Mathf.Max(0f, m_Caster.StateHandler.GetFloat(EStateEffectProperty.BonusLifeSteal) - 1);
            if (lifeSteal > 0 && finalDamage > 0)
            {
                m_Caster.Life.Heal((int)Mathf.Round(lifeSteal * finalDamage), m_Caster.PlayerId, m_SpellData.Parent, m_SpellData.SpellCategory);
            }
        }

        /// <summary>
        /// Add plyer id to list of hitted players (to avoid multiple procs on the same target)
        /// </summary>
        /// <param name="playerId"></param>
        public void AddHittedPlayer(ulong playerId)
        {
            m_HittedPlayerId.Add(playerId);

            // update hit count
            if (m_SpellData.MaxHit > 0 && m_HittedPlayerId.Count >= m_SpellData.MaxHit)
                End();
        }

        /// <summary>
        /// Check if ability hit an ally
        /// </summary>
        /// <param name="targetController"> controller of hit target </param>
        /// <returns></returns>
        protected virtual bool CheckHitAlly(Controller targetController)
        {
            if (targetController.Team != m_Caster.Team)
                return false;

            bool test = false;

            if (m_SpellData.AllyStateEffects.Count > 0)
                test = true;

            // split state effects application into effects that are applied before / after damages
            (List<SStateEffectData> beforeEffects, List<SStateEffectData> afterEffects) = SplitStateEffectsPriority(m_SpellData.AllyStateEffects);

            // apply state effects that are applied BEFORE damages
            ApplyStateEffects(targetController, beforeEffects);

            // try to apply Heal/Shield on the target
            test |= ApplyHeal(targetController);

            // apply state effects that are applied AFTER damages
            ApplyStateEffects(targetController, afterEffects);

            return test;
        }

        /// <summary>
        /// Try to apply Heal/Shield on the target
        /// </summary>
        /// <param name="targetController"></param>
        /// <returns></returns>
        protected virtual bool ApplyHeal(Controller targetController)
        {
            bool test = false;

            if (m_SpellData.Heal > 0)
            {
                targetController.Life.Heal(
                    m_Caster.StateHandler.ApplyBonusInt(m_SpellData.Heal, EStateEffectProperty.Heal, targetController, specialCondition: m_SpellData.Name),
                    m_Caster.PlayerId, m_SpellData.Parent, m_SpellData.SpellCategory);
                test = true;
            }

            if (m_SpellData.Shield > 0)
            {
                targetController.Life.AddShield(
                    m_Caster.StateHandler.ApplyBonusInt(m_SpellData.Shield, EStateEffectProperty.Shield, targetController, specialCondition: m_SpellData.Name),
                    m_Caster.PlayerId, m_SpellData.Parent, m_SpellData.SpellCategory);
                test = true;
            }

            return test;
        }

        public virtual int GetBoostedDamage(Controller targetController)
        {
            if (m_SpellData.Damages.IsNullOrEmpty())
                return 0;

            int damage = 0;
            foreach (SDamage sDamage in  m_SpellData.Damages)
            {
                damage += m_Caster.StateHandler.ApplyBonusDamage(sDamage.Get(m_SpellData.Level, m_Caster, targetController), targetController, sDamage.DamageCategory, sDamage.HitCategory, m_SpellData.Parent);
            }

            return damage;
        }

        /// <summary>
        /// Apply additional effects to the spell
        /// </summary>
        protected virtual void AddExtraEffects()
        {
            // if spell is AutoAttack & controller has a "AutoAttackRune" : add effects of the rune to the spell
            m_Caster.StateHandler.AddExtraEffects(ref m_BaseSpellData, m_Caster.SpellHandler.IsAutoAttack(m_SpellData));
        }

        /// <summary>
        /// Spawn prefabs that procs on hitting a target
        /// </summary>
        /// <param name="targetController"></param>
        public virtual void SpawnOnHitPrefab()
        {
            if (!IsServer)
                return;

            m_SpellData.SpawnOnHitPrefab(m_Caster.PlayerId, transform.position, transform.position);
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

        protected virtual void ApplyStateEffects(Controller targetController, List<SStateEffectData> stateEffects)
        {
            if (!IsServer)
                return;

            if (!targetController.Life.IsAlive)
                return;

            if (targetController.StateHandler == null)
                return;

            var caster = m_Caster.IsSpawn ? m_Caster.SpawnOwner : m_Caster;
            foreach (var effect in stateEffects)
            {
                targetController.StateHandler.AddStateEffect(effect, caster, m_SpellData.Level, origin: m_SpellData.Parent);
            }
        }

        /// <summary>
        /// Split list of state effects between effects that needs to be applied BEFORE damages and those that are applied AFTER
        /// </summary>
        /// <param name="stateEffects"></param>
        protected virtual (List<SStateEffectData>, List<SStateEffectData>) SplitStateEffectsPriority(List<SStateEffectData> stateEffects)
        {
            List<SStateEffectData> beforeEffects = new List<SStateEffectData>();
            List<SStateEffectData> afterEffects = new List<SStateEffectData>();

            foreach (var effect in stateEffects)
            {
                if (AFTER_EFFECTS.Contains(effect.StateEffect.ToString()))
                    afterEffects.Add(effect);
                else
                    beforeEffects.Add(effect);
            }

            return (beforeEffects, afterEffects);
        }

        #endregion


        #region Spell Event

        /// <summary>
        /// On SpellEvent occurring, call event on server side - then on client side if necessary
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <param name="targetController"></param>
        public virtual void CallSpellEvent(ESpellEvent spellEvent, Controller targetController = null)
        {
            if (!IsServer)
                return;

            // Invoke event
            OnSpellEvent?.Invoke(spellEvent);

            // ======================================================================================
            // SPAWN SUB EFFECTS
            m_SpellData.CallSubEffects(
                spellEvent:         spellEvent, 
                level:              m_SpellData.Level,
                parent:             m_SpellData.Parent,
                caster:             m_Caster,
                targetController:   targetController, 
                targetPosition:     m_Target, 
                position:           transform.position
            );

            // ======================================================================================
            // CHECK GFX (send event to client)
            if (GameManager.Instance.IsOfflineMode)
            {
                CallSpellEventGFX(spellEvent, targetController);
                CheckSpellRelocationEvent(spellEvent);
                return;
            }

            // -- Online mode : send event to client
            if (m_SpellData.HasGfxEventAt(spellEvent, checkEnd: false))
            {
                if (targetController == null)
                    CallSpellEventClientRPC(spellEvent);
                else
                    CallSpellEventClientRPC(spellEvent, targetController.PlayerId);
            }
        }

        /// <summary>
        /// From SERVER to CLIENT, call for the CallSpellEvent() method
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <param name="clientID"></param>
        [ClientRpc]
        void CallSpellEventClientRPC(ESpellEvent spellEvent)
        {
            CallSpellEventGFX(spellEvent, null);
            CheckSpellRelocationEvent(spellEvent);
        }

        /// <summary>
        /// From SERVER to CLIENT, call for the CallSpellEvent() method
        ///     -> surcharge with a clientID (if necessary)
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <param name="targetId"></param>
        [ClientRpc]
        void CallSpellEventClientRPC(ESpellEvent spellEvent, ulong targetId)
        {
            CallSpellEventGFX(spellEvent, GameManager.Instance.GetPlayer(targetId));
            CheckSpellRelocationEvent(spellEvent);
        }

        /// <summary>
        /// Spell event : instantiate/destroy the graphics matching the event of the spell
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <param name="targetController"></param>
        protected virtual void CallSpellEventGFX(ESpellEvent spellEvent, Controller targetController = null)
        {
            if (spellEvent == ESpellEvent.OnSpawn)
                OnSpellSpawn?.Invoke(this);

            if (! IsHost)
                OnSpellEvent?.Invoke(spellEvent);

            if (gameObject == null || gameObject.IsDestroyed())
            {
                ErrorHandler.Error("Unable to display graphism for spell event " + spellEvent + " : GameObject is destroyed");
                return;
            }

            // if spell data were destroyed - exit
            if (m_SpellData == null)
                return;

            // ======================================================================================
            // SPAWN GFX
            foreach (var spawnPrefab in m_SpellData.SpellEventActions)
            {
                if (spawnPrefab.GFXLifetime.StartSpellPart != spellEvent)
                    continue;

                spawnPrefab.Spawn(m_Caster, m_SpellData, this, null, targetController, transform.position, m_Target);
            }
        }

        #endregion


        #region Spell Relocation

        void CheckSpellRelocationEvent(ESpellEvent spellEvent)
        {
            // must have spell relocation at this event
            if (!m_SpellData.HasSpellRelocationEventAt(spellEvent))
                return;

            // must be caster of the spell
            if (GameManager.Instance.OwnerClientId != m_Caster.PlayerId) 
                return;

            // register/unregister spell relocation
            RegisterSpellRelocation(spellEvent);
        }

        void RegisterSpellRelocation(ESpellEvent spellEvent)
        {
            if (m_SpellData.SpellRelocation.Lifetime.StartSpellPart >= spellEvent && ! m_IsCheckingRelocation)
            {
                ErrorHandler.Log(() => "REGISTERING Spell Relocation : " + m_SpellData.Name);
                m_IsCheckingRelocation = true;
                ArenaManager.Instance.ClickableArea.ClickedEvent += OnRelocationTargetChanged;
            }

            else if (m_SpellData.SpellRelocation.Lifetime.EndSpellPart <= spellEvent && m_IsCheckingRelocation)
            {
                ErrorHandler.Log(() => "UN-REGISTERING Spell Relocation : " + m_SpellData.Name);
                m_IsCheckingRelocation = false;
                ArenaManager.Instance.ClickableArea.ClickedEvent -= OnRelocationTargetChanged;
            }
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

            return m_SpellData.GetTargetController(m_Caster.PlayerId, m_SpellData.SpellTarget, m_SpellData.CurrentTargetId);
        }

        #endregion


        #region Tools

        protected bool TryGetController(Collider2D collider, out Controller controller, bool throwError = true)
        {
            // check that players has controller 
            controller = Finder.FindComponent<Controller>(collider.gameObject);
            if (controller == null && throwError)
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
            if (!GameManager.Exists)
                return;
           
            GameManager.Instance.State.OnValueChanged += OnGameStateChanged;
        }

        protected virtual void UnRegisterListeners() 
        {
            if (! GameManager.Exists)
                return;

            // close all current listeners for that spell
            OnSpellEvent = null;
            // stop listening to the GameManager
            GameManager.Instance.State.OnValueChanged -= OnGameStateChanged;

            if (m_Caster != null && m_IsCheckingRelocation)
            {
                m_IsCheckingRelocation = false;
                ArenaManager.Instance.ClickableArea.ClickedEvent -= OnRelocationTargetChanged;
            }
        }

        void OnGameStateChanged(EGameState oldValue, EGameState state)
        {
            if (state >= EGameState.GameOver)
                Destroy(gameObject);
        }

        [ServerRpc]
        void OnRelocationTargetChangedServerRPC(float x)
        {
            OnRelocationTargetChanged(x);
        }

        void OnRelocationTargetChanged(float x)
        {
            if (IsClient)
            {
                OnRelocationTargetChangedServerRPC(x);
                return;
            }

            ErrorHandler.Log(() => "OnRelocationTargetChanged() - " + m_SpellData.Name + " : " + x, ELogTag.SpellRelocation);
            m_RelocationTargetPos = new Vector3(x, 0f, 0f);
        }

        #endregion


        #region Debug

        #endregion
    }
}