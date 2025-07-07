using Data;
using Data.DataStructures.SpellSubStructures;
using Enums;
using Game.Loaders;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Tools;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "AutoAttackEffect", menuName = "Game/StateEffects/Aura/AutoAttackEffect")]
    public class AutoAttackEffect : SpellEffect
    {
        #region Members

        [Header("Auto Attack")]
        [SerializeField] protected SpellData m_ReplacementData;

        // ==============================================================================
        // DATA
        protected ESpell m_ReplacedSpell = ESpell.None;

        // ==============================================================================
        // PUBLIC MANIPULATORS
        public override EStateEffectType StateEffectType => EStateEffectType.AutoAttackBuff;

        #endregion


        #region Init & End

        public override bool Initialize(Controller controller, Controller caster, SStateEffectData? stateEffectData = null)
        {
            if (! base.Initialize(controller, caster, stateEffectData))
                return false;

            if (m_ReplacementData == null)
                return true;

            // setup replacement data parent
            m_ReplacementData.SetParent(m_Parent);

            if (m_ReplacementData.SpellType == ESpellType.MultiProjectiles)
            {
                var autoAttackData = m_Controller.SpellHandler.GetSpellData(m_Controller.SpellHandler.AutoAttack, m_Level);
                if (autoAttackData.SpellType != ESpellType.Projectile)
                {
                    ErrorHandler.Error("Unhandled case : trying to set multiprojectile AutoAttack BUFF on a non projectile auto attack");
                    return false;
                }

                m_ReplacementData.AnimationTimer = autoAttackData.AnimationTimer;
                (m_ReplacementData as MultiProjectilesData).OverrideProjectile(autoAttackData as ProjectileData);
            }

            else if (m_ReplacementData.SpellType == ESpellType.MultiSpell)
            {
                var autoAttackData = m_Controller.SpellHandler.GetSpellData(m_Controller.SpellHandler.AutoAttack, m_Level);
                m_ReplacementData.AnimationTimer = autoAttackData.AnimationTimer;
                (m_ReplacementData as MultiSpellData).SetSubSpellData(autoAttackData as ProjectileData);
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


        #region Level & Scaling

        public override StateEffect Clone(int level = 0, string parent = "", string origin = "")
        {
            // clone this spell
            AutoAttackEffect data = (AutoAttackEffect)base.Clone(level == 0 ? m_Level : level, parent, origin);

            if (m_ReplacementData != null)
                data.SetReplacementData(m_ReplacementData.Clone(level));

            return data;
        }

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);

            if (m_ReplacementData != null)
                m_ReplacementData.SetLevel(level);
        }

        public void SetReplacementData(SpellData spellData)
        {
            m_ReplacementData = spellData;
        }

        #endregion


        #region Infos & Description

        public override string GetDescription()
        {
            if (m_Description == "")
                return m_ReplacementData.GetDescription();

            return base.GetDescription();
        }

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