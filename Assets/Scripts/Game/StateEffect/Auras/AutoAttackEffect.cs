using Data;
using Enums;
using Game.Loaders;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "AutoAttackEffect", menuName = "Game/StateEffects/Aura/AutoAttackEffect")]
    public class AutoAttackEffect : SpellEffect
    {
        [Header("Auto Attack")]
        [SerializeField] protected SpellData                m_ReplacementData;

        // ==============================================================================
        // DATA
        protected ESpell m_ReplacedSpell = ESpell.Count;

        // ==============================================================================
        // PUBLIC MANIPULATORS
        public override EStateEffectType StateEffectType => EStateEffectType.AutoAttackBuff;

        #region Init & End

        public override bool Initialize(Controller controller, Controller caster, SStateEffectData? stateEffectData = null)
        {
            if (!base.Initialize(controller, caster, stateEffectData))
                return false;

            if (m_ReplacementData == null)
                return true;

            if (m_ReplacementData.SpellType == ESpellType.MultiProjectiles)
            {
                var autoAttackData = SpellLoader.GetSpellData(m_Controller.SpellHandler.AutoAttack, level: m_Controller.CharacterLevel);
                if (autoAttackData.SpellType != ESpellType.Projectile)
                {
                    ErrorHandler.Error("Unhandled case : trying to set multiprojectile AutoAttack BUFF on a non projectile auto attack");
                    return false;
                }

                (m_ReplacementData as MultiProjectilesData).OverrideProjectile(autoAttackData as ProjectileData);
            }

            m_ReplacedSpell = m_Controller.SpellHandler.AutoAttack;
            m_Controller.SpellHandler.ReplaceSpell(m_Controller.SpellHandler.AutoAttack, m_ReplacementData);

            return true;
        }

        protected override void OnDestroy()
        {
            if (m_ReplacementData != null && m_Controller != null)
                m_Controller.SpellHandler.RemoveOverridingSpell(m_Controller.SpellHandler.AutoAttack, m_ReplacementData.Name);

            base.OnDestroy(); 
        }

        #endregion


        #region Infos & Description

        public override Dictionary<string, object> GetInfos()
        {
            var infos = base.GetInfos();
            if (m_ReplacementData != null)
                m_ReplacementData.AddAsSubSpellInfos(ref infos);
            return infos;
        }

        #endregion

    }
}