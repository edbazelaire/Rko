using Assets;
using Data;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Game.Character
{
    public class SpellHandler : NetworkBehaviour
    {
        #region Members

        // ===================================================================================
        // CONSTANTS
        const string                        c_SpellSpawn            = "SpellSpawn";
        const float                         c_GlobalCooldown        = 0f; 

        // ===================================================================================
        // NETWORK VARIABLES       
        /// <summary> list of spells that links spellID to spellValue <summary>
        NetworkList<int>                    m_SpellsNet;
        /// <summary> list of spells that links spellID to spellValue <summary>
        NetworkList<int>                    m_SpellLevelsNet;        
        /// <summary> global cooldown when a spell is cast </summary>
        NetworkVariable<float>              m_GlobalCooldown        = new NetworkVariable<float>(0);
        /// <summary> currently selected spell </summary>
        NetworkVariable<int>                m_SelectedSpellNet      = new NetworkVariable<int>((int)ESpell.Count);
        /// <summary> position where the spell will land </summary>
        NetworkVariable<Vector3>            m_TargetPos             = new NetworkVariable<Vector3>(default);
        /// <summary> is cast forced to "not allowed" ? </summary>
        NetworkVariable<bool>               m_CastBlocked           = new NetworkVariable<bool>(false);

        // ===================================================================================
        // PRIVATE VARIABLES    
        /// <summary> owner's controller </summary>
        Controller                          m_Controller;
        /// <summary> coroutine of casting a spell </summary>
        Coroutine                           m_CastCoroutine;
        /// <summary> enum of the character's auto attack </summary>
        ESpell                              m_AutoAttack;
        /// <summary> enum of the character's special ability </summary>
        ESpell                              m_SpecialAbility;
        /// <summary> enum of the character's ultimate </summary>
        ESpell                              m_Ultimate;
        /// <summary> overriding spell data (in case of replacement or someting) </summary>
        Dictionary<ESpell, SpellData>       m_OverridingSpellData;
        /// <summary> is the player currently casting a spell ? </summary>
        bool                                m_IsCasting;
        /// <summary> list of cooldowns that links spellID to its cooldown <summary>
        List<float>                         m_Cooldowns;
        /// <summary> association of spells and current selection state </summary>
        Dictionary<ESpell, ESpellSelectionState> m_SpellSelectionStates;
        /// <summary> base spawn position of the spell </summary>
        Transform                           m_SpellSpawn;
        /// <summary> spell that will be selected at the end of the current one </summary>
        ESpell                              m_NextSelectedSpell;
        /// <summary> time before the animation ends </summary>
        float                               m_AnimationTimer;
        /// <summary> is current spell casted can be cancelled ? </summary>
        bool                                m_IsCurrentSpellCancellable    = true;

        // ===================================================================================
        // PUBLIC ACCESSORS
        public NetworkVariable<int>         SelectedSpellNet        => m_SelectedSpellNet;
        public bool                         IsCasting               => m_IsCasting;
        public bool                         IsCastingUncancellable  => m_IsCasting && ! m_IsCurrentSpellCancellable;
        public float                        AnimationTimer          => m_AnimationTimer;
        public ESpell                       SelectedSpell           => m_SelectedSpell;
        public ESpell                       AutoAttack              => m_AutoAttack;
        public ESpell                       SpecialAbility          => m_SpecialAbility;
        public ESpell                       Ultimate                => m_Ultimate;
        public Transform                    SpellSpawn              => m_SpellSpawn;
        public Vector3                      TargetPos               => m_TargetPos.Value;   

        // ===================================================================================
        // EVENTS
        public Action<string, ESpellEvent> OnPreSpellEvent;
        public Action<ESpell, ESpellSelectionState> SpellSelectionEvent;

        #endregion


        #region Inherited Manipulators

        private void Awake()
        {
            m_SpellsNet         = new NetworkList<int>(default);
            m_SpellLevelsNet    = new NetworkList<int>(default);
            
            m_Cooldowns            = new List<float>();
            m_SpellSelectionStates = new();
        }

        public override void OnNetworkSpawn()
        {
            m_Controller = Finder.FindComponent<Controller>(gameObject);
            m_SpellSpawn = Finder.FindComponent<Transform>(gameObject, c_SpellSpawn);
            m_OverridingSpellData = new Dictionary<ESpell, SpellData>();
        }

        void Update()
        {
            // only server can update cooldowns
            if (!IsServer)
                return;
            
            UpdateCooldowns();
        }

        #endregion


        #region Initialization

        /// <summary>
        /// Initialize the spell handler
        /// </summary>
        /// <param name="spells"></param>
        public void Initialize(ESpell autoAttack, ESpell specialAbility, ESpell ultimate, List<ESpell> extraSpells, List<int> spellLevels)
        {
            if (!IsServer)
                return;
          
            m_AutoAttack = autoAttack;
            m_SpecialAbility = specialAbility;
            m_Ultimate = ultimate;

            // insert autoattack and ultimate at the start (not necessary but i prefer)
            extraSpells.Insert(0, autoAttack);
            spellLevels.Insert(0, m_Controller.CharacterLevel);
            extraSpells.Insert(1, specialAbility);
            spellLevels.Insert(1, m_Controller.CharacterLevel);
            extraSpells.Insert(2, ultimate);
            spellLevels.Insert(2, m_Controller.CharacterLevel);

            // setup spells, spell levels and isAutoTarget 
            for (int i=0; i < extraSpells.Count; i++)
            {
                m_SpellsNet.Add((int)extraSpells[i]);
                m_SpellLevelsNet.Add(spellLevels[i]);
                m_Cooldowns.Add(0);
                m_SpellSelectionStates[extraSpells[i]] = ESpellSelectionState.None;
            }

            // set ultimate to inactive by default
            m_SpellSelectionStates[ultimate] = ESpellSelectionState.Inactive;

            // set auto attack as default selected spell and next selected spell if not auto target
            m_SelectedSpell = ESpell.Count;
            m_NextSelectedSpell = ESpell.Count;

            RegisterListeners();
        }

        public void Activate(bool activate)
        {
            if (! activate)
            {
                StopAllCoroutines();
            }

            this.enabled = activate;
        }

        #endregion


        #region End

        public override void OnDestroy()
        {
            base.OnDestroy();

            UnRegisterListeners();
        }

        #endregion


        #region Server RPC

        /// <summary>
        /// Ask the server to select the given spell
        /// </summary>
        /// <param name="spell"></param>
        [ServerRpc]
        public void AskSpellSelectionServerRPC(ESpell spell)
        {
            if (!IsServer)
                return;

            TrySelectSpell(spell);
        }

        [ServerRpc] 
        public void RequestStartCastServerRPC(ESpell spell)
        {
            if (!IsServer)
                return;

            // in case that was not set
            TryStartCastSpell(spell);
        }

        #endregion


        #region Spell Selection

        /// <summary>
        /// Check if the given spell can be selected (no cooldown and enought energy)
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public bool CanSelect(ESpell spell)
        {
            if (spell == ESpell.Count)
                return true;

            return GetCooldown(spell) <= 0f                                                        // spell not on cooldown 
                && GetSpellData(spell).EnergyCost <= m_Controller.EnergyHandler.Energy.Value;      // check enought energy
        }

        /// <summary>
        /// Select the given spell
        /// </summary>
        /// <param name="spell"></param>
        bool TrySelectSpell(ESpell spell)
        {
            if (!IsServer)
                return false;

            if (!m_Controller.GameRunning)
                return false;

            if (!CanSelect(spell))
                return false;

            if (spell != ESpell.Count && spell != AutoAttack)
            {
                ErrorHandler.Log("TrySelectSpell " + spell, ELogTag.SpellHandler);
                ErrorHandler.Log("     -- CHECK : is already Casting (" + m_SelectedSpell + ") : " + m_IsCasting, ELogTag.SpellHandler);
            }

            // set in queue if possible 
            if ((m_IsCasting || m_CastCoroutine != null) && ! SpellLoader.GetSpellData(m_SelectedSpell).IsCancellable)
            {
                ErrorHandler.Log("     -- Setting spell (" + spell + ") as m_NextSelectedSpell", ELogTag.SpellHandler);
                m_NextSelectedSpell = spell;
                return true;
            }

            // on spell selection, reset NextSelectedSpell to default auto attack
            m_SelectedSpell = spell;
            m_NextSelectedSpell = ESpell.Count;

            if (spell == ESpell.Count)
                return true;

            bool success = TryStartCastSpell(spell);

            if (m_Controller.IsPlayer)
                ErrorHandler.Log("TryStartCastSpell " + spell + " success : " + success, ELogTag.SpellHandler);

            if (!success)
                ErrorHandler.Log("Trying to cast spell " + spell + " on selection but was not able", ELogTag.SpellHandler);

            return true;
        }

        /// <summary>
        /// Refresh the state of the spell selection and fire event on change
        /// </summary>
        /// <param name="spell"></param>
        void RefreshSpellSelectionState(ESpell spell)
        {
            ESpellSelectionState spellSelectionState = ESpellSelectionState.None;
            if (GetCooldown(spell) > 0)
                spellSelectionState = ESpellSelectionState.Cooldown;
            else if (GetSpellData(spell).EnergyCost > m_Controller.EnergyHandler.Energy.Value)
                spellSelectionState = ESpellSelectionState.Inactive;

            SetSpellSelection(spell, spellSelectionState);
        }

        public void SetSpellSelection(ESpell spell, ESpellSelectionState spellSelectionState)
        {
            // NO CHANGES - skip
            if (m_SpellSelectionStates[spell] == spellSelectionState)
                return;

            m_SpellSelectionStates[spell] = spellSelectionState;
            SpellActionvationEventClientRPC(spell, spellSelectionState);
        }

        #endregion


        #region Casting

        /// <summary>
        /// Check if the given spell can be cast (no cooldown, enought energy, not doing a blocking action or in a state that prevents casts)
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public bool CanCast(ESpell spell)
        {
            if (spell == ESpell.Count)
            {
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log("Spell cast (" + spell + ") BLOCKED : no spell selected", ELogTag.SpellHandler);
                return false;
            }

            if (!m_SpellsNet.Contains((int)spell))
            {
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Error("Trying to select spell (" + spell + ") but spell does not exists");
                return false;
            }

            if (!CanSelect(spell))
            {
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log("Spell cast (" + spell + ") BLOCKED : spell can not be selected", ELogTag.SpellHandler);
                return false;
            }

            // check : cast is not forced blocked
            if (m_CastBlocked.Value)
            {
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log("Spell cast (" + spell + ") BLOCKED : cast is forced cancel", ELogTag.SpellHandler);
                return false;
            }

            // check : global cooldown done
            if (m_GlobalCooldown.Value > 0f)
            {
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log("Spell cast (" + spell + ") BLOCKED : m_GlobalCooldown (" + m_GlobalCooldown.Value + ") > 0", ELogTag.SpellHandler);
                return false;
            }

            // check state effect blocking the cast
            if (HasStateBlockingCast())
            {
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log("Spell cast (" + spell + ") BLOCKED : HasStateBlockingCast()", ELogTag.SpellHandler);
                return false;
            }

            // performing a special animation : cant cast or move
            if (m_Controller.StateHandler.HasState(EStateEffect.SpecialAnimation))
            {
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log("Spell cast (" + spell + ") BLOCKED : Has state 'SpecialAnimation'", ELogTag.SpellHandler);
                return false;
            }

            // check : is casting an other spell
            if (m_IsCasting && ! m_IsCurrentSpellCancellable)
            {
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log("Spell cast (" + spell + ") BLOCKED : is casting an other spell", ELogTag.SpellHandler);
                return false;
            }

            if (m_CastCoroutine != null && ! m_IsCurrentSpellCancellable)
            {
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log("Spell cast (" + spell + ") BLOCKED : Coroutine not over", ELogTag.SpellHandler);
                return false;
            }

            if (! CheckEnemyTargetable(spell))
            {
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log("Spell cast (" + spell + ") BLOCKED : Enemy is not targetable", ELogTag.SpellHandler);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Does the player have any states bloking the cast ?
        /// </summary>
        /// <returns></returns>
        public bool HasStateBlockingCast()
        {
            return m_Controller.StateHandler.IsStunned                                  // is stunned
                || m_Controller.StateHandler.IsSilenced                                 // is silenced
                || m_Controller.StateHandler.HasState(EStateEffect.Frozen)              // is frozen 
                || m_Controller.StateHandler.HasState(EStateEffect.Jump)                // is jumping
                || m_Controller.CounterHandler.IsBlockingCast.Value;                    // is using a counter
        }

        /// <summary>
        /// Check if enemy can be targetted by provided spell
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public bool CheckEnemyTargetable(ESpell spell)
        {
            SpellData spellData = GetSpellData(spell);
            spellData.ForceAutoTarget();

            if (spellData.SpellTarget != ESpellTarget.FirstEnemy)
                return true;

            return ! GameManager.Instance.GetFirstEnemy(GameManager.Instance.GetPlayer(m_Controller.PlayerId).Team).StateHandler.IsUnTargetable;
        }

        /// <summary>
        /// Cast the given spell
        /// </summary>
        /// <param name="spell"></param>
        public bool TryStartCastSpell(ESpell spell)
        {
            if ((m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI)) && spell != m_AutoAttack)
                ErrorHandler.Log("TryStartCastSpell : " + spell, ELogTag.SpellHandler);

            if (!IsServer)
                return false;

            if (!CanCast(spell))
                return false;

            // if curently casting another spell, cancel it
            if (m_IsCasting)
                CancelCast();

            m_SelectedSpell = spell;

            // cast spell
            m_CastCoroutine = StartCoroutine(StartCast(spell));

            return true;
        }

        /// <summary>
        /// Cast the given spell
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        IEnumerator StartCast(ESpell spell)
        {
            if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                ErrorHandler.Log("StartCastSpell : " + spell, ELogTag.SpellHandler);

            // only owner can ask for cast
            if (!IsServer)
                yield break;

            // SETUP : get spell data and set animation to motion
            SpellData spellData = GetSpellData(spell);

            if (spellData.LockTargetAt != ESpellEvent.OnCast)
                LockTarget(spellData);

            // SETUP : casting data
            m_IsCurrentSpellCancellable = spellData.IsCancellable;
            m_IsCasting = true;

            // cancel current movement
            m_Controller.Movement.CancelMovement(true);

            // set animation timer
            m_AnimationTimer = spellData.AnimationTimer / CurrentCastSpeedFactor;

            // call for the spell animation
            CallSpellEvent(spellData.name, ESpellEvent.OnStartCast);
            m_Controller.AnimationHandler.PlayAnimationClientRPC(spellData.Animation, m_AnimationTimer);

            // wait for animation to finish (if not already)
            while (m_AnimationTimer > 0f)
            {
                m_AnimationTimer -= Time.deltaTime;

                // if player is moving, cancel the spell
                if ((spellData.IsCancellable && m_Controller.Movement.IsMoving) || HasStateBlockingCast() || ! CheckEnemyTargetable(spell))
                {
                    // reset Animator
                    CancelCast();
                    yield break;
                }

                yield return null;
            }

            if (m_Controller.IsPlayer)
                ErrorHandler.Log("     -- CAST DONE : " + spell, ELogTag.SpellHandler);

            // ask server to cast the spell
            Cast(spell);
            m_Controller.Movement.CancelMovement(false);

            // reset spell selection
            TrySelectSpell(m_NextSelectedSpell);
        }

        /// <summary>
        /// Ask the server to cast the selected spell
        /// </summary>
        void Cast(ESpell spell)
        {
            if (!IsServer)
                return;

            if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                ErrorHandler.Log("Cast : " + spell, ELogTag.SpellHandler);

            SpellData spellData = GetSpellData(spell, m_SpellLevelsNet[GetSpellIndex(spell)]);
            if (spellData.LockTargetAt == ESpellEvent.OnCast)
                LockTarget(spellData);

            // get spawn position and cast the spell
            StartCoroutine(spellData.CastDelay(m_Controller.PlayerId, m_TargetPos.Value, m_SpellSpawn.position, m_SpellSpawn.rotation, recalculateTarget: false));

            // spend the energy of the spell
            if (spellData.EnergyCost > 0)
                m_Controller.EnergyHandler.SpendEnergy(spellData.EnergyCost);

            // inform that casting is done
            CallSpellEvent(spellData.name, ESpellEvent.OnCast);
            m_IsCasting = false;
            m_Controller.AnimationHandler.CancelCastAnimationClientRpc();
            m_IsCurrentSpellCancellable = true;
            m_CastCoroutine = null;

            // setup global cooldown
            m_GlobalCooldown.Value = c_GlobalCooldown;

            // setup cooldown
            SetCooldown(spell, CalculateCooldown(spellData.Cooldown));
        }

        /// <summary>
        /// Set timer to 0 to cancel the cast
        /// </summary>
        public void CancelCast()
        {
            // reset cancel current movement
            m_Controller.Movement.CancelMovement(false);

            // set animation timer to 0
            m_AnimationTimer = 0f;

            // stop casting
            m_IsCasting = false;
            m_IsCurrentSpellCancellable = true;

            // cancel cast animation
            m_Controller.AnimationHandler.CancelCastAnimationClientRpc();

            // call PreSpellEvent
            CallSpellEvent(m_SelectedSpell.ToString(), ESpellEvent.OnEnd);

            // check Coroutine
            if (m_CastCoroutine != null)
            {
                StopCoroutine(m_CastCoroutine);
                m_CastCoroutine = null;
            }
        }

        #endregion


        #region Spell Target & Position

        void LockTarget(SpellData spellData)
        {
            spellData.ForceAutoTarget();

            // recalculate target depending on spell and conditions (autocast, ...)
            var target = m_TargetPos.Value;
            spellData.CalculateTarget(ref target, m_Controller.PlayerId);
            m_TargetPos.Value = target;
        }

        #endregion


        #region Private Manipulators

        /// <summary>
        /// Update cooldowns
        /// </summary>
        void UpdateCooldowns()
        {
            if (m_GlobalCooldown.Value > 0f)
                m_GlobalCooldown.Value -= Time.deltaTime;

            foreach (ESpell spell in Spells)
            {
                if (GetCooldown(spell) <= 0f)
                    continue;

                SetCooldown(spell, GetCooldown(spell) - Time.deltaTime);
            }
        }

        #endregion


        #region Cooldown Management

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public void SetCooldown(ESpell spell, float cooldown)
        {
            // only server can change a cooldown value
            if (!IsServer)
                return;

            if (cooldown < 0f)
                cooldown = 0f;

            m_Cooldowns[GetSpellIndex(spell)] = cooldown;

            RefreshSpellSelectionState(spell);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spellType"></param>
        /// <returns></returns>
        public float GetCooldown(ESpell spellType)
        {
            if (! NetworkManager.Singleton.IsConnectedClient || GameManager.IsGameOver)
                return 0f;
            return m_Cooldowns[GetSpellIndex(spellType)];
        }

        public void ResetCooldowns()
        {
            if (!IsServer)
                return;

            foreach (var spellId in m_SpellsNet)
            {
                ResetSpell((ESpell)spellId);
            }
        }

        public void ResetSpell(ESpell spell)
        {
            if (!IsServer)
                return;

            SetCooldown(spell, 0f);
        }

        #endregion


        #region Target Management

        public float GetCastSpeed(string spell)
        {
            return Mathf.Max(0.01f, spell == AutoAttack.ToString() ? Settings.AutoAttackSpeedFactor * m_Controller.StateHandler.GetFloat(EStateEffectProperty.AttackSpeed) : Settings.CastSpeedFactor * m_Controller.StateHandler.GetFloat(EStateEffectProperty.CastSpeed));
        }

        public float CalculateCooldown(float baseCooldown)
        {
            return Mathf.Max(0f, baseCooldown - m_Controller.StateHandler.GetInt(EStateEffectProperty.CooldownReduction)) * Mathf.Max(0f, 2 - m_Controller.StateHandler.GetFloat(EStateEffectProperty.CooldownReductionPerc));
        }

        #endregion


        #region Public Manipulators

        public void ReplaceAutoAttack(SpellData spellData)
        {
            ReplaceSpell(AutoAttack, spellData);
        } 

        public void ReplaceSpell(ESpell spell, SpellData spellData)
        {
            if (!IsServer)
                return;

            m_OverridingSpellData[spell] = spellData;
        }

        public void RemoveOverridingSpell(ESpell originalSpell, string replacementSpell)
        {
            Debug.Log("RemoveOverridingSpell : " + originalSpell);
            if (m_OverridingSpellData.ContainsKey(originalSpell) && m_OverridingSpellData[originalSpell].Name == replacementSpell)
                m_OverridingSpellData.Remove(originalSpell);
        }

        /// <summary>
        /// Force player to not cast anything
        /// </summary>
        /// <param name="block"></param>
        public void ForceBlockCast(bool block)
        {
            if (!IsServer)
                return;

            m_CastBlocked.Value = block;
        }

        #endregion


        #region Helpers 

        /// <summary>
        /// Get the index if the spell in list of index
        /// </summary>
        /// <param name="spellType"></param>
        /// <returns></returns>
        public int GetSpellIndex(ESpell spellType)
        {
            if (!Spells.Contains(spellType))
            {
                ErrorHandler.Warning($"SpellHandler : spell {spellType} was not found in list of spells");
                return 0;
            }

            return Spells.IndexOf(spellType);
        }

        #endregion


        #region Listeners

        void RegisterListeners()
        {
            m_Controller.EnergyHandler.Energy.OnValueChanged += OnEnergyChanged;
        }

        void UnRegisterListeners()
        {
            m_Controller.EnergyHandler.Energy.OnValueChanged -= OnEnergyChanged;
        }

        public void OnEnergyChanged(int _, int newValue)
        {
            if (GameManager.IsGameOver)
                return;

            foreach (var spell in Spells)
            {
                RefreshSpellSelectionState(spell);
            }
        }

        [ClientRpc]
        void SpellActionvationEventClientRPC(ESpell spell, ESpellSelectionState spellActivation)
        {
            SpellSelectionEvent?.Invoke(spell, spellActivation);
        }

        public void CallSpellEvent(string spellName, ESpellEvent spellEvent)
        {
            var spellData = SpellLoader.GetSpellData(spellName, destroy: true);
            
            // check has effect linked to that event
            if (! spellData.HasGfxEventAt(spellEvent))
            {
                if (spellEvent == ESpellEvent.OnCast && spellData.CastSoundFX != null)
                    GameManager.Instance.PlayCastSoundClientRPC(spellName);
                return;
            }

            // check if one of the effects requests a spawn position
            if (! spellData.HasTargetGfxEventAt(spellEvent))
            {
                // NO POSITION REQUESTED
                CallSpellEventClientRPC(spellName, spellEvent);
            }
            else
            {
                // POSITION REQUESTED : add target pos to the variables
                CallSpellEventClientRPC(spellName, spellEvent, m_TargetPos.Value);
            }
        }

        [ClientRpc]
        public void CallSpellEventClientRPC(string spellName, ESpellEvent spellEvent)
        {
            m_Controller.GFXHandler.SpawnSpellGFX(spellName, spellEvent);
            OnPreSpellEvent?.Invoke(spellName, spellEvent);
        }

        [ClientRpc]
        public void CallSpellEventClientRPC(string spellName, ESpellEvent spellEvent, Vector3 targetPos)
        {
            m_Controller.GFXHandler.SpawnSpellGFX(spellName, spellEvent, targetPos);
            OnPreSpellEvent?.Invoke(spellName, spellEvent);
        }
        
        #endregion


        #region Getter / Setter / Dependent Properties

        protected SpellData GetSpellData(ESpell spell, int level = 1)
        {
            if (m_OverridingSpellData.ContainsKey(spell))
            {
                return m_OverridingSpellData[spell];
            }

            return SpellLoader.GetSpellData(spell, level);
        }

        ESpell m_SelectedSpell
        {
            get => (ESpell)m_SelectedSpellNet.Value;
            set => m_SelectedSpellNet.Value = (int)value;
        }

        public List<ESpell> Spells
        {
            get
            {
                List<ESpell> spells = new List<ESpell>();

                try
                {
                    foreach (int spellId in m_SpellsNet)
                        spells.Add((ESpell)spellId);
                } catch (Exception ex) 
                {
                    ex.Equals(null);
                }
                
                return spells;
            }
        }

        public List<int> SpellLevels
        {
            get
            {
                List<int> levels = new List<int> ();
                foreach (int level in m_SpellLevelsNet)
                    levels.Add(level);
                
                return levels;
            }
        }

        public bool IsAutoAttack => m_SelectedSpell == AutoAttack;

        public float CurrentCastSpeedFactor => GetCastSpeed(m_SelectedSpell.ToString());

        #endregion
    }
}