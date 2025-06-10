using Assets;
using Assets.Scripts.Data.DataStructures.SpellRequirement;
using Data;
using Data.DataStructures.SpellSubStructures;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Game.NetworkStructures;
using Game.Spells;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Character
{
    public class SpellHandler : NetworkBehaviour
    {
        #region Members

        // ===================================================================================
        // CONSTANTS
        const float                         c_GlobalCooldown        = 0f;

        // ===================================================================================
        // ACTIONS
        public Action<string, ESpellEvent>          OnPreSpellEvent;
        public Action<ESpell, ESpellSelectionState> SpellSelectionEvent;
        public Action<ESpell, float>                OnCooldownEvent;
        public Action<float>                        RelocationTargetChangedEvent;
        public Action<string, ESpellProperty, int>  SpellOverrideEvent;

        // ===================================================================================
        // NETWORK VARIABLES       
        /// <summary> enum of the character's auto attack </summary>
        NetworkVariable<ESpell>             m_AutoAttack            = new NetworkVariable<ESpell>(ESpell.None);
        /// <summary> enum of the character's special ability </summary>
        NetworkVariable<ESpell>             m_SpecialAbility        = new NetworkVariable<ESpell>(ESpell.None);
        /// <summary> enum of the character's ultimate </summary>
        NetworkVariable<ESpell>             m_Ultimate              = new NetworkVariable<ESpell>(ESpell.None);
        /// <summary> list of spells that links spellID to spellValue <summary>
        NetworkList<int>                    m_SpellsNet;
        /// <summary> list of number of charges left for each spells <summary>
        NetworkList<int>                    m_NChargesNet;
        /// <summary> list of spell levels (usefull for UI purpuses) <summary>
        NetworkList<int>                    m_SpellLevelsNet;
        /// <summary> global cooldown when a spell is cast </summary>
        NetworkVariable<float>              m_GlobalCooldown        = new NetworkVariable<float>(0);
        /// <summary> currently selected spell </summary>
        NetworkVariable<int>                m_SelectedSpellIndexNet = new NetworkVariable<int>((int)ESpell.None);
        /// <summary> position where the spell will land </summary>
        NetworkVariable<Vector3>            m_TargetPos             = new NetworkVariable<Vector3>(default);
        /// <summary> is cast forced to "not allowed" ? </summary>
        NetworkVariable<bool>               m_CastBlocked           = new NetworkVariable<bool>(false);

        // ===================================================================================
        // PRIVATE VARIABLES    
        /// <summary> owner's controller </summary>
        Controller                          m_Controller;
        /// <summary> spell data of the currently selected spell </summary>
        SpellData                           m_SelectedSpellData;
        /// <summary> target position requested by the player (if spell is moving) </summary>
        Vector3                             m_RelocationTargetPos;
        /// <summary> list of spell listening to the spell data relocation </summary>
        List<SpellData>                     m_RelocationSpellData;
        /// <summary> coroutine of casting a spell </summary>
        Coroutine                           m_CastCoroutine;
        /// <summary> overriding spell data (in case of replacement or someting) </summary>
        Dictionary<ESpell, List<SpellData>> m_OverridingSpellData;
        /// <summary> is the player currently casting a spell ? </summary>
        bool                                m_IsCasting;
        /// <summary> list of all spell data linked to spellID <summary>
        List<SpellData>                     m_SpellsData;
        /// <summary> list of cooldowns that links spellID to its cooldown <summary>
        List<float>                         m_Cooldowns;
        /// <summary> association of spells and current selection state </summary>
        Dictionary<ESpell, ESpellSelectionState> m_SpellSelectionStates;
        /// <summary> spell that will be selected at the end of the current one </summary>
        ESpell                              m_NextSelectedSpell;
        /// <summary> time before the animation ends </summary>
        float                               m_AnimationTimer;
        /// <summary> is current spell casted can be cancelled ? </summary>
        bool                                m_IsCurrentSpellCancellable    = true;

        /// <summary> base spawn position of the spell </summary>
        Transform m_SpellSpawn => m_Controller.GFXHandler.GetBodyPart(EBodyPart.SpellSpawn).transform;
        string m_SelectedSpell => m_SelectedSpellData != null ? m_SelectedSpellData.Name : ESpell.None.ToString();
        public float CurrentCastSpeedFactor => GetCastSpeed(m_SelectedSpell.ToString());


        // ===================================================================================
        // PUBLIC ACCESSORS
        public NetworkVariable<int>         SelectedSpellIndexNet   => m_SelectedSpellIndexNet;
        public NetworkList<int>             NChargesNet             => m_NChargesNet;
        public NetworkList<int>             SpellLevelsNet          => m_SpellLevelsNet;
        public List<SpellData>              SpellsData              => m_SpellsData;
        public bool                         IsCasting               => m_IsCasting;
        public bool                         IsCastingUncancellable  => m_IsCasting && ! m_IsCurrentSpellCancellable;
        public float                        AnimationTimer          => m_AnimationTimer;
        public string                       SelectedSpell           => m_SelectedSpell;
        public ESpell                       AutoAttack              => m_AutoAttack.Value;
        public ESpell                       SpecialAbility          => m_SpecialAbility.Value;
        public ESpell                       Ultimate                => m_Ultimate.Value;
        public Transform                    SpellSpawn              => m_SpellSpawn;
        public Vector3                      TargetPos               => m_TargetPos.Value;   
        public Vector3                      RelocationTargetPos     => m_RelocationTargetPos;

        #endregion


        #region Inherited Manipulators

        private void Awake()
        {
            m_SpellsNet             = new NetworkList<int>(default);
            m_NChargesNet           = new NetworkList<int>(default);
            m_SpellLevelsNet        = new NetworkList<int>(default);

            m_SpellsData            = new List<SpellData>();
            m_Cooldowns             = new List<float>();
            m_Cooldowns             = new List<float>();
            m_SpellSelectionStates  = new();
        }

        public override void OnNetworkSpawn()
        {
            m_Controller = Finder.FindComponent<Controller>(gameObject);
            m_OverridingSpellData = new ();
            m_RelocationSpellData = new ();
        }

        void Update()
        {
            // only server can update cooldowns
            if (!IsServer)
                return;

            if (! GameManager.Instance.IsGameStarted)
                return;

            if (GameManager.IsGameOver)
                return;
            
            UpdateCooldowns();
            UpdateSpellsSelectionState();
            UpdateTargetPosition();
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
          
            m_AutoAttack.Value        = autoAttack;
            m_SpecialAbility.Value    = specialAbility;
            m_Ultimate.Value          = ultimate;

            // insert autoattack and ultimate at the start (not necessary but i prefer)
            if (autoAttack != ESpell.None)
            {
                extraSpells.Insert(0, autoAttack);
                spellLevels.Insert(0, m_Controller.CharacterLevel);
            }

            if (specialAbility != ESpell.None)
            {
                extraSpells.Insert(1, specialAbility);
                spellLevels.Insert(1, m_Controller.CharacterLevel);
            }

            if (specialAbility != ESpell.None)
            {
                extraSpells.Insert(2, ultimate);
                spellLevels.Insert(2, m_Controller.CharacterLevel);
            }

            // setup spells, spell levels and isAutoTarget 
            for (int i=0; i < extraSpells.Count; i++)
            {
                if (extraSpells[i] == ESpell.None)
                    continue;

                m_SpellsNet     .Add((int)extraSpells[i]);
                m_SpellsData    .Add(SpellLoader.GetSpellData(extraSpells[i], spellLevels[i]));
                m_NChargesNet   .Add(m_SpellsData[i].Charges);
                m_SpellLevelsNet.Add(spellLevels[i]);
                m_Cooldowns     .Add(0);
                m_SpellSelectionStates[extraSpells[i]] = ESpellSelectionState.None;
            }

            // set ultimate to inactive by default
            m_SpellSelectionStates[ultimate] = ESpellSelectionState.Inactive;

            // set auto attack as default selected spell and next selected spell if not auto target
            m_SelectedSpellData = null;
            m_NextSelectedSpell = ESpell.None;

            RegisterListeners();
            RegisterListenersClientRPC();
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

        #endregion


        #region Spell Selection

        public bool CanSelect(ESpell spell) => CanSelect(spell, out string _);
        

        /// <summary>
        /// Check if the given spell can be selected (no cooldown and enought energy)
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public bool CanSelect(ESpell spell, out string reason)
        {
            if (spell == ESpell.None)
            {
                reason = "Spell is None";
                return true;
            }

            return CanSelect(m_SpellsData[GetSpellIndex(spell)], out reason);
        }

        public bool CanSelect(SpellData spellData, out string reason)
        {
            // COOLDOWN : check that spell has no current cooldown
            if (GetCharges(spellData.Name) == 0)
            {
                reason = "Spell selection (" + spellData.Name + ") BLOCKED : No charges left";
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            // ENERGY : check that has enought energy to cast the spell 
            if (spellData.EnergyCost > m_Controller.EnergyHandler.Energy.Value)
            {
                reason = "Spell selection (" + spellData.Name + ") BLOCKED : Not enought energy";
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            // check if spell must be unique and has already instance 
            if (!CheckUniqueSpell(spellData))
            {
                reason = "Spell selection (" + spellData.Name + ") BLOCKED : Unique spell";
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            // check that spell requirements are met
            if (!CheckSpellRequirements(spellData))
            {
                reason = "Spell selection (" + spellData.Name + ") BLOCKED : CheckSpellRequirements";
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            reason = "";
            return true;
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

            if (spell != ESpell.None && spell != m_AutoAttack.Value)
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
            int spellIndex = GetSpellIndex(spell);
            m_SelectedSpellIndexNet.Value = spellIndex;
            m_NextSelectedSpell = ESpell.None;

            if (spell == ESpell.None)
                return true;

            bool success = TryStartCastSpell(m_SpellsData[spellIndex]);

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

            // no charges : handled by the SpellItemUI directly
            if (GetCharges(spell.ToString()) == 0)
                spellSelectionState = ESpellSelectionState.Cooldown;

            // Un-selectable : change selection state to Inactive
            else if (!CanSelect(spell))
                spellSelectionState = ESpellSelectionState.Inactive;

            SetSpellSelection(spell, spellSelectionState);
        }

        public void SetSpellSelection(ESpell spell, ESpellSelectionState spellSelectionState)
        {
            // NO CHANGES - skip
            if (m_SpellSelectionStates[spell] == spellSelectionState)
                return;

            m_SpellSelectionStates[spell] = spellSelectionState;
            SpellActivationEventClientRPC(spell, spellSelectionState);
        }

        public ESpellSelectionState GetSpellSelectionState(ESpell spell)
        {
            return m_SpellSelectionStates[spell];
        }

        #endregion


        #region Casting

        public bool CanCast(ESpell spell)
        {
            if (spell == ESpell.None)
            {
                return false;
            }

            return CanCast(SpellLoader.GetSpellData(spell), out string _);
        }

        /// <summary>
        /// Check if the given spell can be cast (no cooldown, enought energy, not doing a blocking action or in a state that prevents casts)
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public bool CanCast(SpellData spellData, out string reason)
        {
            if (! CanSelect(spellData, out reason))
            {
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            // check : cast is not forced blocked
            if (m_CastBlocked.Value)
            {
                reason = "Spell cast (" + spellData.Name + ") BLOCKED : cast is forced cancel";
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            // check : global cooldown done
            if (m_GlobalCooldown.Value > 0f)
            {
                reason = "Spell cast (" + spellData.Name + ") BLOCKED : m_GlobalCooldown (" + m_GlobalCooldown.Value + ") > 0";
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            // check state effect blocking the cast
            if (!m_Controller.StateHandler.CanCast)
            {
                reason = "Spell cast (" + spellData.Name + ") BLOCKED : HasStateBlockingCast()";
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            // performing a special animation : cant cast or move
            if (m_Controller.StateHandler.HasState(EStateEffect.SpecialAnimation) || m_Controller.StateHandler.HasState(EStateEffect.Vanish))
            {
                reason = "Spell cast (" + spellData.Name + ") BLOCKED : Has state 'SpecialAnimation'";
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            // check : is casting an other spell
            if (m_IsCasting && ! m_IsCurrentSpellCancellable)
            {
                reason = "Spell cast (" + spellData.Name + ") BLOCKED : is casting another non cancellable spell";
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            if (m_CastCoroutine != null && ! m_IsCurrentSpellCancellable)
            {
                reason = "Spell cast (" + spellData.Name + ") BLOCKED : Coroutine not over";
                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            // check if enemy can be targeted
            if (! CheckEnemyTargetable(spellData))
            {
                reason = "Spell cast (" + spellData.Name + ") BLOCKED : Enemy is not targetable";

                if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                    ErrorHandler.Log(reason, ELogTag.SpellHandler);
                return false;
            }

            reason = "";
            return true;
        }

        public bool CheckUniqueSpell(SpellData spellData)
        {
            switch (spellData)
            {
                case SpawnerData spawnerData:
                    return ! spawnerData.IsUnique 
                        || ! GameManager.Instance.TryFindSpellInArena(spawnerData.Name, out Spell _, m_Controller);
                    
                default:
                    return true;
            }
        }

        /// <summary>
        /// Check if enemy can be targetted by provided spell
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public bool CheckEnemyTargetable(SpellData spellData)
        {
            spellData.ForceAutoTarget();

            if (spellData.SpellTarget != ESpellTarget.FirstEnemy)
                return true;

            return ! GameManager.Instance.GetFirstEnemy(m_Controller.Team).StateHandler.IsUnTargetable;
        }

        public bool CheckSpellRequirements(SpellData spellData)
        {
            if (spellData.SpellRequirements.Count == 0)
                return true;

            // AT LEAST ONE : return SUCCESS
            foreach (SpellRequirements spellRequirement in spellData.SpellRequirements)
            {
                if (spellRequirement.CheckRequirement(m_Controller, null))
                    return true;
            }

            // No Requirement was met : FAILURE
            return false;
        }

        public bool TryConsumeSpellRequirements(SpellData spellData)
        {
            foreach (SpellRequirements spellRequirement in spellData.SpellRequirements)
            {
                if (! spellRequirement.TryApplyRequirements(m_Controller, null))
                    return false;
            }

            return true;
        }

        public bool TryStartCastSpell(SpellData spellData)
        {
            return TryStartCastSpell(spellData, out string _);
        }

        public bool TryStartCastSpell(ESpell spell, int level)
        {
            return TryStartCastSpell(GetSpellData(spell, level), out string _);
        }

        public bool TryStartCastSpell(ESpell spell, int level, out string reason)
        {
            return TryStartCastSpell(GetSpellData(spell, level), out reason);
        }

        /// <summary>
        /// Cast the given spell
        /// </summary>
        /// <param name="spell"></param>
        public bool TryStartCastSpell(SpellData spellData, out string reason)
        {
            if ((m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI)) && spellData.Name != m_AutoAttack.Value.ToString())
                ErrorHandler.Log("TryStartCastSpell : " + spellData.Name, ELogTag.SpellHandler);

            if (!IsServer)
            {
                reason = "Not Server";
                return false;
            }

            if (! CanCast(spellData, out reason))
                return false;

            // check if can consume spell requirements
            if (! TryConsumeSpellRequirements(spellData))
            {
                reason = "Unable to consume spell requirements for " + spellData.Name;
                return false;
            }

            // if curently casting another spell, cancel it
            if (m_IsCasting)
                CancelCast();

            // set as selected spell
            m_SelectedSpellData = spellData;

            // cast spell
            m_CastCoroutine = StartCoroutine(StartCast(spellData));

            return true;
        }

        /// <summary>
        /// Cast the given spell
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        IEnumerator StartCast(SpellData spellData)
        {
            // duplicate spell data for safety
            spellData = spellData.Clone();

            if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                ErrorHandler.Log("StartCastSpell : " + spellData.Name, ELogTag.SpellHandler);

            // only owner can ask for cast
            if (!IsServer)
                yield break;

            if (spellData.LockTarget == ESpellEvent.OnStartCast)
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
            if (spellData.Animation != EAnimation.None)
            {
                // special animation for the spell
                if (spellData.Animation == EAnimation.Self)
                    m_Controller.AnimationHandler.PlayAnimationClientRPC(spellData.Name, m_AnimationTimer);

                // classic anmiation
                else
                    m_Controller.AnimationHandler.PlayAnimationClientRPC(spellData.Animation, m_AnimationTimer);
            }

            // wait for animation to finish (if not already)
            while (m_AnimationTimer > 0f)
            {
                m_AnimationTimer -= Time.deltaTime;

                // if player is moving, cancel the spell
                if ((spellData.IsCancellable && m_Controller.Movement.IsMoving) || ! m_Controller.StateHandler.CanCast || ! CheckEnemyTargetable(spellData))
                {
                    // reset Animator
                    CancelCast();
                    yield break;
                }

                yield return null;
            }

            if (m_Controller.IsPlayer)
                ErrorHandler.Log("     -- CAST DONE : " + spellData.Name, ELogTag.SpellHandler);

            // ask server to cast the spell
            Cast(spellData);

            // call that cast is over for the Controller (animation, movement blocked, ...)
            if (spellData.IsCompletedOnCast)
                OnCastCompleted();
        }

        /// <summary>
        /// Ask the server to cast the selected spell
        /// </summary>
        void Cast(SpellData spellData)
        {
            if (!IsServer)
                return;

            if (m_Controller.IsPlayer || Main.LogTags.Contains(ELogTag.AI))
                ErrorHandler.Log("Cast : " + spellData.Name, ELogTag.SpellHandler);

            if (spellData.LockTarget == ESpellEvent.OnCast)
                LockTarget(spellData);

            // get spawn position and cast the spell
            StartCoroutine(spellData.CastDelay(m_Controller.PlayerId, m_TargetPos.Value, m_SpellSpawn.position, m_SpellSpawn.rotation, recalculateTarget: false));

            // spend the energy of the spell
            if (spellData.EnergyCost > 0)
                m_Controller.EnergyHandler.SpendEnergy(spellData.EnergyCost);

            // setup global cooldown
            m_GlobalCooldown.Value = c_GlobalCooldown;

            // setup cooldown
            int index = GetSpellIndex(spellData.Name);
            if (index >= 0)
                ConsumeCharge(spellData.Name);
        }

        /// <summary>
        /// Set timer to 0 to cancel the cast
        /// </summary>
        public void CancelCast()
        {
            if (!IsCasting && m_CastCoroutine != null)
                return;

            // check Coroutine
            if (m_CastCoroutine != null)
                StopCoroutine(m_CastCoroutine);

            // reset properties linked to casting a spell
            ResetCastProperties();

            // call PreSpellEvent
            CallSpellEvent(m_SelectedSpell.ToString(), ESpellEvent.OnCancelCast);
        }

        public void OnCastCompleted()
        {
            // reset properties and variables
            ResetCastProperties();

            // reset spell selection
            TrySelectSpell(m_NextSelectedSpell);
        }

        void ResetCastProperties()
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

            // reset Coroutine
            m_CastCoroutine = null;
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

        [ServerRpc]
        void SendTargetAdjustmentServerRpc(float x)
        {
            RelocationTargetChangedEvent?.Invoke(x);
        }

        /// <summary>
        /// Update target position to the requested direction
        /// </summary>
        void UpdateTargetPosition()
        {
            // no need to update pos if not requtested pos is asked, or if pos already reached
            if (m_RelocationTargetPos == default || m_TargetPos.Value.x == m_RelocationTargetPos.x)
                return;

            int teamFactor = m_Controller.Team == 0 ? 1 : -1;

            // calculate expected position at that frame
            var xPos = m_RelocationTargetPos.x;
            var direction = m_TargetPos.Value.x > xPos ? -1 : 1;
            var pos = m_TargetPos.Value + new Vector3(direction * m_SelectedSpellData.SpellRelocation.Speed * teamFactor * Time.deltaTime, 0f, 0f);

            // clamp position to max/min allowed position
            if (direction < 0 && pos.x < xPos)
                pos.x = xPos;
            else if (direction > 0 && pos.x > xPos)
                pos.x = xPos;

            // clamp position to target area
            if (m_SelectedSpellData.ClampTargetPos)
                m_SelectedSpellData.ClampTargetX(ref pos, m_Controller.PlayerId);

            // set new position
            m_TargetPos.Value = pos;
        }

        #endregion


        #region Updates

        /// <summary>
        /// Update cooldowns
        /// </summary>
        void UpdateCooldowns()
        {
            if (m_GlobalCooldown.Value > 0f)
                m_GlobalCooldown.Value -= Time.deltaTime;

            for (int i = 0; i < m_Cooldowns.Count; i++)
            {
                // at max charges - skip
                if (m_NChargesNet[i] == m_SpellsData[i].Charges)
                    continue;

                m_Cooldowns[i] -= Time.deltaTime;

                // cooldown not over - skip
                if (m_Cooldowns[i] > 0f)
                    continue;

                // clean cooldown and recharge the spell
                m_Cooldowns[i] = 0f;
                AddCharge(m_SpellsNet[i].ToString(), 1);

                // if not at full charges : start another cooldown
                if (m_NChargesNet[i] < m_SpellsData[i].Charges)
                    StartCooldown(i);
            }
        }

        void UpdateSpellsSelectionState()
        {
            foreach (var spell in Spells)
            {
                RefreshSpellSelectionState(spell);
            }
        }

        #endregion


        #region Cooldown & Charges Management

        public void ConsumeCharge(string spellName, int nCharges = 1)
        {
            AddCharge(spellName, -nCharges);

            if (GetCooldown(spellName) <= 0)
                StartCooldown(GetSpellIndex(spellName));

            // if number of charges is 0 - send cooldown value to the Player so he can display it
            int spellIndex = GetSpellIndex(spellName);
            if (m_NChargesNet[spellIndex] == 0)
                SendCooldownEventClientRPC(m_SpellsData[spellIndex].Spell, m_Cooldowns[spellIndex]);
        }

        public int GetCharges(string spellName)
        {
            return m_NChargesNet[GetSpellIndex(spellName)];
        }

        public void AddCharge(string spellName, int nCharges)
        {
            int spellIndex = GetSpellIndex(spellName);
            if (spellIndex < 0)
            {
                ErrorHandler.Error("Unable to find spell " + spellName + " in list of spells");
                return;
            }

            // calculate final number of charges
            int finalCharges = m_NChargesNet[spellIndex] + nCharges;

            // CHECK : number of charges is between 0 and MAX
            int maxCharges = m_SpellsData[spellIndex].GetInt(ESpellProperty.Charges);
            if (finalCharges < 0)
            {
                ErrorHandler.Warning("Number of charges for spell " + spellName + "(" + finalCharges + ") is < 0" );
                finalCharges = 0;
            } else if (finalCharges > maxCharges)
            {
                ErrorHandler.Warning("Number of charges for spell " + spellName + "(" + finalCharges + ") is below > max number of charges (" + maxCharges + ")");
                finalCharges = maxCharges;
            }

            // set number of charges
            m_NChargesNet[GetSpellIndex(spellName)] = finalCharges;
        }

        /// <summary>
        /// Start cooldown for spell at index
        /// </summary>
        /// <param name="spellIndex"></param>
        public void StartCooldown(int spellIndex)
        {
            SetCooldown(m_SpellsData[spellIndex].Name, CalculateCooldown(m_SpellsData[spellIndex].Cooldown));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public void SetCooldown(string spellName, float cooldown)
        {
            // only server can change a cooldown value
            if (!IsServer)
                return;

            if (cooldown < 0f)
                cooldown = 0f;

            m_Cooldowns[GetSpellIndex(spellName)] = cooldown;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public float GetCooldown(ESpell spell)
        {
            return GetCooldown(spell.ToString());
        }

        public float GetCooldown(string spellName)
        {
            if (!NetworkManager.Singleton.IsConnectedClient || GameManager.IsGameOver)
                return 0f;

            int index = GetSpellIndex(spellName);
            // not in list of spells : no cooldown
            if (index < 0)
                return 0f;

            return m_Cooldowns[index];
        }

        public void ReduceCooldowns(float cooldownReduction)
        {
            for (int i = 0; i < m_Cooldowns.Count; i++)
            {
                if (m_Cooldowns[i] <= 0)
                    continue;

                // update cooldown server value
                m_Cooldowns[i] = Mathf.Max(0, m_Cooldowns[i] - cooldownReduction);

                // fire event that cooldown has been updated
                OnCooldownEvent?.Invoke(Spells[i], m_Cooldowns[i]);
            }
        }

        public void ResetCooldowns()
        {
            if (!IsServer)
                return;

            foreach (var spellId in m_SpellsNet)
            {
                ResetCooldown((ESpell)spellId);
            }
        }

        public void ResetCooldown(ESpell spell)
        {
            if (!IsServer)
                return;

            SetCooldown(spell.ToString(), 0f);
        }

        #endregion


        #region Target Management

        public float GetCastSpeed(string spell)
        {
            return Mathf.Max(0.01f, spell == AutoAttack.ToString() ? Settings.AutoAttackSpeedFactor * m_Controller.StateHandler.GetFloat(EStateEffectProperty.AttackSpeed) : Settings.CastSpeedFactor * m_Controller.StateHandler.GetFloat(EStateEffectProperty.CastSpeed));
        }

        public float CalculateCooldown(float baseCooldown)
        {
            var cooldownReduction = m_Controller.StateHandler.GetInt(EStateEffectProperty.CooldownReduction);
            var cooldownPerc = 2 - m_Controller.StateHandler.GetFloat(EStateEffectProperty.CooldownReductionPerc);
            return Mathf.Max(0f, (baseCooldown - cooldownReduction) * cooldownPerc);
        }

        #endregion


        #region Public Manipulators

        public void ReplaceSpell(ESpell spell, SpellData spellData)
        {
            Debug.Log("ReplaceSpell : " + spell + " - with " + spellData.Name);

            if (! m_OverridingSpellData.ContainsKey(spell) || m_OverridingSpellData[spell].IsNullOrEmpty())
                m_OverridingSpellData[spell] = new();

            m_OverridingSpellData[spell].Add(spellData);
        }

        public void RemoveOverridingSpell(ESpell originalSpell, string replacementSpell)
        {
            Debug.Log("RemoveOverridingSpell : " + originalSpell + " - with " + replacementSpell);

            // no overriding data for that spell
            if (! m_OverridingSpellData.ContainsKey(originalSpell) || m_OverridingSpellData[originalSpell].IsNullOrEmpty())
                return;

            // check exists
            int index = m_OverridingSpellData[originalSpell].FindIndex(spellData => spellData.Name == replacementSpell);
            if (index < 0)
                return;

            // remove from list
            m_OverridingSpellData[originalSpell].RemoveAt(index);
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
        /// <param name="spell"></param>
        /// <returns></returns>
        public int GetSpellIndex(ESpell spell)
        {
            return GetSpellIndex(spell.ToString());
        }

        public int GetSpellIndex(string spellName)
        {
            if (spellName == ESpawn.None.ToString())
                return -1;

            // init
            ESpell spell;

            // if is overriding, get the index of the spell it is overriding
            spell = m_OverridingSpellData.FirstOrDefault(kvp => kvp.Value.Any(spellData => spellData.Name == spellName)).Key;
  
            // not overriding - try and parse it
            if (spell == ESpell.None)
            {
                if (Enum.TryParse(spellName, out spell))
                {
                    if (!Spells.Contains(spell))
                    {
                        ErrorHandler.Warning($"SpellHandler : spell {spell} was not found in list of spells");
                        return -1;
                    }
                }
                else
                {
                    ErrorHandler.Warning($"SpellHandler : unable to parse {spellName} into spell");
                    return -1;
                }
            }

            return Spells.IndexOf(spell);
        }

        #endregion


        #region Listeners

        public void RegisterListeners()
        {

        }

        [ClientRpc]
        void RegisterListenersClientRPC()
        {
            if (!IsOwner)
                return;

            if (!m_Controller.IsPlayer)
                return;

            // TODO : Change for ONE big zone for click events ? 
            // register to the TargettableArea listener
            ArenaManager.GetTargettableArea(m_Controller.Team, enemyArea: true).ClickedEvent += SendTargetAdjustmentServerRpc;
            ArenaManager.GetTargettableArea(m_Controller.Team, enemyArea: false).ClickedEvent += SendTargetAdjustmentServerRpc;
        }

        public void UnRegisterListeners()
        {

        }

        void OnRelocationTargetChanged(float x)
        {
            m_RelocationTargetPos = new Vector3(x, 0f, 0f);
        }

        void RegisterSpellRelocation(SpellData spellData)
        {
            if (m_RelocationSpellData.Count == 0)
                RelocationTargetChangedEvent += OnRelocationTargetChanged;

            m_RelocationSpellData.Add(spellData);
        }

        void CheckUnregisterSpellRelocation(ESpellEvent spellEvent)
        {
            if (m_RelocationSpellData.Count == 0)
                return;

            for (int i = m_RelocationSpellData.Count - 1; i >= 0; i--)
            {
                if (m_RelocationSpellData[i].HasSpellRelocationEventAt(spellEvent, checkStart: false, checkEnd: true))
                    m_RelocationSpellData.RemoveAt(i);
            }

            if (m_RelocationSpellData.Count > 0)
                return;

            // Unregister and reset relocation pos
            RelocationTargetChangedEvent -= OnRelocationTargetChanged;
            m_RelocationTargetPos = default;
        }

        [ClientRpc]
        void SpellActivationEventClientRPC(ESpell spell, ESpellSelectionState spellActivation)
        {
            SpellSelectionEvent?.Invoke(spell, spellActivation);
        }

        [ClientRpc]
        void SendCooldownEventClientRPC(ESpell spell, float cooldown)
        {
            OnCooldownEvent?.Invoke(spell, cooldown);
        }

        public void CallSpellEvent(string spellName, ESpellEvent spellEvent, Vector2? targetPosition = null, float? forcedDuration = null, Vector2? forcedPosition = null)
        {
            var spellData = SpellLoader.GetSpellData(spellName, destroy: true);

            // call just on server side
            OnPreSpellEvent?.Invoke(spellName, spellEvent);

            // CHECK Relocation Spell
            if (m_SelectedSpellData != null && m_SelectedSpellData.HasSpellRelocationEventAt(spellEvent, checkStart: true, checkEnd: false))
                RegisterSpellRelocation(m_SelectedSpellData);

            // -- check unregistering
            CheckUnregisterSpellRelocation(spellEvent);

            ushort spellEventByte = (ushort)spellEvent;

            if (targetPosition == null)
                targetPosition = m_TargetPos.Value;

            // FORCED POSITION REQUESTED
            if (forcedPosition != null)
            {
                if (forcedDuration == null)
                    CallSpellEventClientRPC(spellName, spellEventByte, new Vector2Short(forcedPosition.Value));
                else
                    CallSpellEventClientRPC(spellName, spellEventByte, new Vector2Short(forcedPosition.Value), forcedDuration.Value);
            }

            // HAS TARGET POS REQUESTED
            else if (spellData.HasTargetGfxEventAt(spellEvent))
            {
                // POSITION REQUESTED : add target pos to the variables
                if (forcedDuration == null)
                    CallSpellEventClientRPC(spellName, spellEventByte, new Vector2Short(targetPosition.Value));
                else
                    CallSpellEventClientRPC(spellName, spellEventByte, new Vector2Short(targetPosition.Value), forcedDuration.Value);
            }

            // NO POSITION REQUESTED
            else
            {
                if (forcedDuration == null)
                    CallSpellEventClientRPC(spellName, spellEventByte);
                else
                    CallSpellEventClientRPC(spellName, spellEventByte, forcedDuration.Value);
            }
        }

        [ClientRpc]
        public void CallSpellOverrideClientRPC(FixedString32Bytes spellName, ESpellProperty spellProperty, byte value)
        {
            SpellOverrideEvent?.Invoke(spellName.ToString(), spellProperty, value);
        }

        /// <summary>
        /// Base method for called when "CallSpellEventClientRPC" is called
        /// </summary>
        /// <param name="spellName"></param>
        /// <param name="spellEvent"></param>
        public void OnClientSpellEvent(string spellName, ESpellEvent spellEvent)
        {
            // only owner can see that, and host do not need to since the event gets called on SERVER side already
            if (!IsOwner || IsHost)
                return;

            // call event
            OnPreSpellEvent?.Invoke(spellName, spellEvent);
        }

        [ClientRpc]
        public void CallSpellEventClientRPC(FixedString32Bytes spellName, ushort spellEvent)
        {
            //Debug.LogWarning("CallSpellEventClientRPC() - Player " + m_Controller.PlayerId);
            //Debug.Log("     + spellEvent : " + (ESpellEvent)spellEvent);
            //Debug.Log("     + spellName : " + spellName);

            m_Controller.GFXHandler.SpawnSpellGFX(spellName.ToString(), (ESpellEvent)spellEvent);
            OnClientSpellEvent(spellName.ToString(), (ESpellEvent)spellEvent);
        }

        [ClientRpc]
        public void CallSpellEventClientRPC(FixedString32Bytes spellName, ushort spellEvent, float forcedDuration)
        {
            //Debug.LogWarning("CallSpellEventClientRPC() - Player " + m_Controller.PlayerId);
            //Debug.Log("     + spellEvent : " + (ESpellEvent)spellEvent);
            //Debug.Log("     + spellName : " + spellName);
            //Debug.Log("     + forcedDuration : " + forcedDuration);

            m_Controller.GFXHandler.SpawnSpellGFX(spellName.ToString(), (ESpellEvent)spellEvent, forcedDuration: forcedDuration);
            OnClientSpellEvent(spellName.ToString(), (ESpellEvent)spellEvent);
        }

        [ClientRpc]
        public void CallSpellEventClientRPC(FixedString32Bytes spellName, ushort spellEvent, Vector2Short targetPos)
        {
            //Debug.LogWarning("CallSpellEventClientRPC() - Player " + m_Controller.PlayerId);
            //Debug.Log("     + spellEvent : " + (ESpellEvent)spellEvent);
            //Debug.Log("     + spellName : " + spellName);
            //Debug.Log("     + targetPos : " + targetPos);

            m_Controller.GFXHandler.SpawnSpellGFX(spellName.ToString(), (ESpellEvent)spellEvent, targetPos);
            OnClientSpellEvent(spellName.ToString(), (ESpellEvent)spellEvent);
        }

        [ClientRpc]
        public void CallSpellEventClientRPC(FixedString32Bytes spellName, ushort spellEvent, Vector2Short targetPos, float forcedDuration)
        {
            //Debug.LogWarning("CallSpellEventClientRPC() - Player " + m_Controller.PlayerId);
            //Debug.Log("     + spellEvent : " + (ESpellEvent)spellEvent);
            //Debug.Log("     + spellName : " + spellName);
            //Debug.Log("     + targetPos : " + targetPos);
            //Debug.Log("     + forcedDuration : " + forcedDuration);

            m_Controller.GFXHandler.SpawnSpellGFX(spellName.ToString(), (ESpellEvent)spellEvent, targetPos, forcedDuration);
            OnClientSpellEvent(spellName.ToString(), (ESpellEvent)spellEvent);
        }
        
        #endregion


        #region Getter / Setter / Dependent Properties

        public bool IsAutoAttack(SpellData spellData)
        {
            // default case : is the default auto attack
            if (spellData.Name == AutoAttack.ToString())
                return true;

            // check if is override
            SpellData currentAutoAttackData = GetSpellData(AutoAttack, spellData.Level);
            if (currentAutoAttackData.Name == spellData.Name)
                return true;

            // check if current data is multi type
            if (currentAutoAttackData is MultiProjectilesData mpd)
                return mpd.ProjectileData != null && mpd.ProjectileData.Name == spellData.Name;

            return false;
        }

        public SpellData GetSpellData(ESpell spell, int level)
        {
            if (m_OverridingSpellData.ContainsKey(spell) && ! m_OverridingSpellData[spell].IsNullOrEmpty())
            {
                return m_OverridingSpellData[spell].Last().Clone(level);
            }

            return SpellLoader.GetSpellData(spell, level);
        }

        public SpellData GetSpellDataAtIndex(int index, bool clone = true)
        {
            SpellData spellData = m_SpellsData[index];

            if (m_OverridingSpellData.ContainsKey(spellData.Spell) && !m_OverridingSpellData[spellData.Spell].IsNullOrEmpty())
            {
                spellData = m_OverridingSpellData[spellData.Spell].Last();
            }

            if (clone) 
                return spellData.Clone();

            return spellData;
        }

        public void OverrideSpellDataAtIndex(int index, List<SOverridingData> overridingData, int level)
        {
            var spellData = GetSpellDataAtIndex(index, clone: false);
            spellData.AddOverridingData(overridingData, level);

            // Handle changes that require event to Client
            foreach (SOverridingData ovData in overridingData)
            {
                switch (ovData.Property)
                {
                    case ESpellProperty.Charges:
                        NChargesNet[index] = spellData.Charges;
                        CallSpellOverrideClientRPC(spellData.Name, ovData.Property, (byte)spellData.Charges);
                        break;

                    default: 
                        break;
                }
            }
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


        #endregion
    }
}