using Enums;
using Game;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Tools;
using UnityEngine;
using Unity.VisualScripting;
using Data.GameManagement;
using System;
using System.Linq;
using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using Data.DataStructures.SpellSubStructures;

namespace Data
{
    [CreateAssetMenu(fileName = "MultiSpellData", menuName = "Game/Spells/MultiSpellData")]
    public class MultiSpellData : SpellData
    {
        #region Members

        public override ESpellType SpellType => ESpellType.MultiSpell;

        [Header("Sub Spell")]
        [SerializeField, Tooltip("Type of path that the spell is taking")]
        protected SpellData SubSpellData;
        [SerializeField, Tooltip("Use the character auto attack as SubSpellData")]
        public bool m_UseAutoAttack;
        [SerializeField, Tooltip("Use the character auto attack as SubSpellData")]
        protected List<SOverridingData> m_OverridingSubData;

        [Header("Multiple Spell Data")]
        [Tooltip("Type of multiple projectile launch")]
        public EMultiProjectileType MultiProjectileType;
        [Tooltip("Position of the subspells")]
        public SMultiSpellSpawn SubSpellSpawn;
        [Tooltip("Target of the subspells")]
        public SMultiSpellSpawn SubSpellTarget;
        [SerializeField, Tooltip("Min/Max height of spell spawn")]
        protected SMinMax m_YMinMax;
        [SerializeField, Tooltip("Should the subspell recalculate its target on spawn ?")]
        protected bool m_RecalculateTarget = false;
        [SerializeField, Tooltip("Should the subspell recalculate its position on spawn ?")]
        protected bool m_RecalculatePosition = true;
        [SerializeField, Tooltip("Is the chacter blocked until the end of the cast ?")]
        protected bool m_IsBlocking = true;
        [SerializeField, Tooltip("Number of projectiles launched")]
        protected int m_NProjectiles = 1;
        [SerializeField, Description("Number of breaking points that divides the size of the zone")]
        protected int m_NBreakPoints = 1;
        [SerializeField, Tooltip("Size of the projectile zone")]
        protected float m_ProjectileZoneSize = 0f;
        [SerializeField, Tooltip("Delay between each projectile cast")]
        protected float m_DelayBetweenLaunches = 0f;
        [SerializeField, Tooltip("Number of waves")]
        protected int m_NWaves = 1;
        [SerializeField, Tooltip("Delay between each waves")]
        protected float m_DelayBetweenWaves = 0f;

        [Header("Extra Sound Effects & Animations")]
        [Tooltip("Animation on each wave casted")]
        public EAnimation OnCastWaveAnimation = EAnimation.None;
        [Tooltip("Animation on each wave projectile")]
        public EAnimation OnCastProjectileAnimation = EAnimation.None;
        [Tooltip("Sound Effect on each wave casted")]
        public AudioClip OnCastWaveSoundFX = null;
        [Tooltip("Sound Effect on each projectile casted")]
        public AudioClip OnCastProjectileSoundFX = null;

        // ============================================================================================
        // Public Accessors
        public int NProjectiles                 => (int)GetScaledValue(ESpellProperty.NProjectiles, m_NProjectiles);
        public int NWaves                       => (int)GetScaledValue(ESpellProperty.NWaves, m_NWaves);
        public float DelayBetweenLaunches       => GetScaledValue(ESpellProperty.DelayBetweenLaunches, m_DelayBetweenLaunches);
        public float DelayBetweenWaves          => GetScaledValue(ESpellProperty.DelayBetweenWaves, m_DelayBetweenWaves);
        public float ProjectileZoneSize         => m_ProjectileZoneSize * Settings.SpellSizeFactor;
        /// <summary> is the "IsCasting" over once the spell has been casted (before delay) ? </summary> ///
        public override bool IsCompletedOnCast  => ! m_IsBlocking;
        
        // ============================================================================================
        // Private Members
        bool m_IsCancelled;
        SpellData m_FinalSubSpellData;

        #endregion


        #region Casting & Spawning

        public override void Cast(ulong clientId, Vector3 target, Vector3 position = default, Quaternion rotation = default, bool recalculateTarget = true, bool recalculatePosition = true, bool recalculateRotation = true)
        {
            // init sub spell data
            m_FinalSubSpellData = GetSubSpellData(GameManager.Instance.GetPlayer(clientId));

            // error - exit
            if (m_FinalSubSpellData == null)
                return;

            // recalculate target depending on spell type
            if (recalculateTarget)
                CalculateTarget(ref target, clientId);

            // recalculate position depending on spell type
            if (recalculatePosition)
                RecalculatePosition(ref position, target, clientId);

            // recalculate rotation
            if (recalculateRotation)
                RecalculateRotation(ref rotation);

            if (NProjectiles < 1)
            {
                ErrorHandler.Error("Bad config for spell " + name + " : NProjectiles (" + NProjectiles + ")  < 1");
                return;
            }

            GameManager.Instance.GetPlayer(clientId).StartCoroutine(CastMultipleWaves(clientId, target, position, rotation));
        }

        public IEnumerator CastMultipleWaves(ulong clientId, Vector3 target, Vector3 position = default, Quaternion rotation = default)
        {
            // block movement and cast until the end
            Controller controller = GameManager.Instance.GetPlayer(clientId);

            ErrorHandler.Log("MutliSpell STARTED : " + Name + " =============================================================", ELogTag.MultiSpells);

            for (int i = 0; i < NWaves; i++)
            {
                ErrorHandler.Log("     + " + Name + " WAVE [" + i + " / "+ NWaves + "] - start", ELogTag.MultiSpells);

                yield return CastOneWave(clientId, target, position, rotation);

                ErrorHandler.Log("     + " + Name + " WAVE [" + i + " / " + NWaves + "] - over", ELogTag.MultiSpells);

                if (i == NWaves - 1 || m_IsCancelled)
                    break;

                // play animation only if blocked during the animation
                if (m_IsBlocking)
                {
                    // play wave animation
                    controller.AnimationHandler.PlayAnimationClientRPC(Animation, DelayBetweenWaves);

                    // spawn SubSpell - SpellGFX
                    controller.SpellHandler.CallSpellEvent(Name, ESpellEvent.OnStartCast, m_Level);
                }

                // =============================================================================
                // PREPARING NEXT PROJECTILE 
                var delay = DelayBetweenWaves;
                while (delay > 0)
                {
                    if (IsCancellable && ! controller.StateHandler.CanCast && m_IsBlocking)
                    {
                        m_IsCancelled = true;
                        controller.SpellHandler.CallSpellEvent(Name, ESpellEvent.OnCancelCast, m_Level);
                        break;
                    }

                    delay -= Time.deltaTime;
                    yield return null;
                }

                if (m_IsCancelled)
                    break;

                // call end of wave cast
                if (m_IsBlocking)
                {
                    // spawn SubSpell - SpellGFX
                    controller.SpellHandler.CallSpellEvent(Name, ESpellEvent.OnCast, m_Level);
                }
            }

            // cancel animation only if blocked during the animation
            if (m_IsBlocking)
                controller.AnimationHandler.CancelCastAnimationClientRpc();

            // if spell is blocking Controller during the spawn of all multi projectiles, call that the cast has been completed
            if (! IsCompletedOnCast)
            {
                GameManager.Instance.GetPlayer(clientId).SpellHandler.OnCastCompleted();
            }

            ErrorHandler.Log("MutliSpell ENDED : " + Name + " =============================================================", ELogTag.MultiSpells);

            CallSpellEvent(controller, ESpellEvent.OnEnd, target);
            Destroy(this);
        }

        public IEnumerator CastOneWave(ulong clientId, Vector3 target, Vector3 position = default, Quaternion rotation = default)
        {
            // recalculate target at each waves
            CalculateTarget(ref target, clientId);

            // Play wave sound if any
            if (OnCastWaveSoundFX != null)
                GameManager.Instance.PlayCastWaveSoundClientRPC(Name);

            // block movement and cast until the end
            Controller caster = GameManager.Instance.GetPlayer(clientId);

            // calculate next projectile target
            Vector3 projectileTarget = CalculateMultiSpellTarget(target, 0, caster.Team);

            for (int i = 0; i < NProjectiles; i++)
            {
                ErrorHandler.Log("          - " + Name + " Projectile (" + i + " / " + NProjectiles + ")", ELogTag.MultiSpells);

                CastOneProjectile(
                    caster, 
                    target:     projectileTarget,
                    position:   SubSpellSpawn.Recalculate(position, i, caster.Team, NProjectiles), 
                    rotation:   rotation
                );

                // last projectile ? exit loop
                if (i + 1 >= NProjectiles)
                    break;

                // =============================================================================
                // SPECIAL CASE : Jumps - wait that the player has came back
                if (m_FinalSubSpellData is JumpData jumpData)
                {
                    yield return new WaitUntil(() => caster.StateHandler.HasState(EStateEffect.Jump));
                    yield return new WaitUntil(() => !caster.StateHandler.HasState(EStateEffect.Jump));
                }

                // =============================================================================
                // PREPARING NEXT PROJECTILE 
                // -- calculate next projectile target
                projectileTarget = CalculateMultiSpellTarget(target, i+1, caster.Team);

                // -- call event of "StartCast" for next spell
                if (m_FinalSubSpellData != null)
                    m_FinalSubSpellData.CallSpellEvent(caster, ESpellEvent.OnStartCast, projectileTarget);

                // -- cast animation if any
                if (OnCastProjectileAnimation != EAnimation.None)
                {
                    // special animation for the spell
                    if (OnCastProjectileAnimation == EAnimation.Self)
                        caster.AnimationHandler.PlayAnimationClientRPC(Name, DelayBetweenLaunches);

                    // classic anmiation
                    else
                        caster.AnimationHandler.PlayAnimationClientRPC(OnCastProjectileAnimation, DelayBetweenLaunches);
                }

                // start delay
                var delay = DelayBetweenLaunches;
                while (delay > 0)
                {
                    if (IsCancellable && ! caster.StateHandler.CanCast && m_IsBlocking)
                    {
                        m_IsCancelled = true;
                        caster.SpellHandler.CallSpellEvent(m_FinalSubSpellData.Name, ESpellEvent.OnCancelCast, m_FinalSubSpellData.Level);
                        yield break;
                    }

                    delay -= Time.deltaTime;
                    yield return null;
                }
            }
        }

        public void CastOneProjectile(Controller controller, Vector3 target, Vector3 position = default, Quaternion rotation = default)
        {
            // play wave sound if any
            if (OnCastProjectileSoundFX != null)
                GameManager.Instance.PlayCastProjectileSoundClientRPC(Name);

            // cast sup spell with delay
            controller.StartCoroutine(m_FinalSubSpellData.CastDelay(
                clientId:               controller.PlayerId,
                target:                 target,
                position:               position,
                rotation:               rotation,
                delay:                  m_FinalSubSpellData.Delay,
                recalculateTarget:      m_RecalculateTarget,
                recalculatePosition:    m_RecalculatePosition
            ));
        }

        #endregion


        #region End & Destruction

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        #endregion


        #region Postion & Target

        /// <summary>
        /// Calculate position of the projectile number "i" depending on his type
        /// </summary>
        /// <param name="target">   base target of the projectile   </param>
        /// <param name="i">        projectile number               </param>
        /// <returns></returns>
        protected Vector3 CalculateMultiSpellTarget(Vector3 target, int i, int team)
        {
            if (SubSpellTarget.HasTargetX)
            {
                target = SubSpellTarget.Recalculate(target, i, team, m_NProjectiles);
                target.y = UnityEngine.Random.Range(m_YMinMax.Min, m_YMinMax.Max);
                return target;
            }

            // ==========================================================================================================
            // DEPRECATED
            ErrorHandler.Warning("Using deprecated method to calculate SpellTarget for : " + Name);

            (float min, float max) = ArenaManager.GetAreaBounds(team, SpellTarget == ESpellTarget.None || IsEnemyTarget);

            var zoneSize = ProjectileZoneSize;
            // if projectile size < 0 : use all the size of the arena
            if (zoneSize < 0f)
            {
                zoneSize    = ArenaManager.Instance.TargettableAreaSize;
                target.x    = min + zoneSize / 2;
            }

            switch (MultiProjectileType)
            {
                case EMultiProjectileType.None:
                    break;

                case (EMultiProjectileType.Line):
                    target.x += ((team == 0 ? -1 : 1) * zoneSize / 2) + (i+1) * (team == 0 ? 1 : -1) * zoneSize / (NProjectiles + 1);
                    break;

                case (EMultiProjectileType.Random):
                    target.x += UnityEngine.Random.Range(-zoneSize / 2, zoneSize / 2);
                    break;

                case (EMultiProjectileType.RandomLine):
                    if (m_NBreakPoints <= 0)
                    {
                        ErrorHandler.Error("Bad number of BreakPoints (" + m_NBreakPoints + ") for " + Name + " with Target type EMultiProjectileType.RandomLine");
                        break; 
                    }
                    var nBreakPoints = m_NBreakPoints + 1;
                    var index = UnityEngine.Random.Range(1, nBreakPoints);
                    target.x += ((team == 0 ? -1 : 1) * zoneSize / 2) + index * (team == 0 ? 1 : -1) * zoneSize / nBreakPoints;
                    break;

                default:
                    ErrorHandler.Warning("Unhandled MultiProjectileType : " + MultiProjectileType);
                    break;
            }

            target.x = Mathf.Clamp(target.x, min, max);
            target.y = UnityEngine.Random.Range(m_YMinMax.Min, m_YMinMax.Max);

            return target;
        }

        #endregion


        #region Overriders 

        public void SetSubSpellData(SpellData spellData)
        {
            SubSpellData        = spellData;
            m_FinalSubSpellData = spellData;
        }

        public SpellData GetSubSpellData(Controller controller)
        {
            if (SubSpellData == null && !m_UseAutoAttack)
            {
                ErrorHandler.Error("Bad setting for spell : " + Name + " - no SubSpellData provided and UseAutoAttack is set to FALSE");
                return null;
            }

            SpellData finalSpellData;
            if (m_UseAutoAttack)
            {
                finalSpellData = controller.SpellHandler.GetSpellData(controller.SpellHandler.AutoAttack, m_Level);

                // handle case where auto attack is a multispell data
                if (finalSpellData is MultiSpellData multiSpellData && multiSpellData.SubSpellData != null)
                {
                    finalSpellData = multiSpellData.SubSpellData.Clone(m_Level);
                } else if (finalSpellData is MultiProjectilesData multiProjectilesData && multiProjectilesData.ProjectileData != null)
                {
                    finalSpellData = multiProjectilesData.ProjectileData;
                }
            } else
            {
                finalSpellData = SubSpellData.Clone(m_Level);
            }

            finalSpellData.SetParent(Parent);

            // apply overriding data if any
            finalSpellData.AddOverridingData(m_OverridingSubData, m_Level);
            finalSpellData.AnimationTimer = DelayBetweenLaunches;

            return finalSpellData;
        }

        #endregion


        #region Level

        public override void SetLevel(int level)
        {
            base.SetLevel(level);
        }

        #endregion


        #region Info Display

        public override string GetDescription()
        {
            string description = base.GetDescription();
            if (SubSpellData != null)
                description = TextHandler.ReplaceSubSpellData(description, SubSpellData.Clone(m_Level, true));
            description = description.Replace("[DelayBetweenWaves]", DelayBetweenWaves.ToString());
            return description;
        }

        public override Dictionary<string, object> GetInfo()
        {
            string[] keysToIgnore = new string[] { "Cooldown", "Cast" };
            var infoDict = base.GetInfo();
            if (m_UseAutoAttack)
            {
                infoDict["Type"] = "Projectile";
            }
            else if (SubSpellData != null)
            {
                foreach (var item in SubSpellData.Clone(m_Level).GetInfo())
                {
                    if (keysToIgnore.Contains(item.Key))
                        continue;

                    infoDict[item.Key] = item.Value;    
                }
            } 

            infoDict["Projectiles"] = NProjectiles;

            if (NWaves > 1)
            {
                infoDict["Waves"] = NWaves;
            }

            return infoDict;
        }

        #endregion
    }
}