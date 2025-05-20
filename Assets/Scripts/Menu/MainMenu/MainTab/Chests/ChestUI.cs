using Assets.Scripts.Managers.Sound;
using Data;
using Enums;
using Game.Loaders;
using System;
using System.Collections;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Menu.MainMenu.MainTab.Chests
{
    public class ChestUI : MObject
    {
        #region Members

        public const string IDLE_ANIMATION = "ChestIdle";
        public const string OPEN_ANIMATION = "ChestOpen";

        public const string c_ChestPreview = "ChestPreview";
        public const string c_AuraEffects  = "AuraEffects";
        public const string c_OpenEffects  = "OpenEffects";

        private     ChestRewardData     m_ChestData;

        protected   GameObject          m_Preview;
        protected   SpriteRenderer      m_SpriteRenderer;
        protected   Sprite              m_Icon;
        protected   GameObject          m_AuraEffects;
        protected   GameObject          m_OpeningEffects;
        protected   Animator            m_Animator;
        protected   AudioSource         m_AudioSource;

        public Sprite Icon => m_Icon;

        private EChest m_ChestType 
        {
            get
            {
                string myName = name;
                if (myName.EndsWith("(Clone)"))
                    myName = myName[..^"(Clone)".Length];
                if (myName.EndsWith("Chest"))
                    myName = myName[..^"Chest".Length];

                if (!Enum.TryParse(myName, out EChest chest))
                {
                    ErrorHandler.Error("Unable to parse chest " + name + " into a EChestType");
                    chest = EChest.Common;
                }
                return chest;
            }
        }
        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            LoadData();

            m_Preview           = Finder.Find(gameObject, "Preview");
            m_SpriteRenderer    = Finder.FindComponent<SpriteRenderer>(m_Preview);
            m_Icon              = m_SpriteRenderer.sprite;
            m_Animator          = Finder.FindComponent<Animator>(m_Preview);
            m_AuraEffects       = Finder.Find(gameObject, c_AuraEffects);
            m_OpeningEffects    = Finder.Find(gameObject, c_OpenEffects);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_AuraEffects.SetActive(false);
            m_OpeningEffects.SetActive(false);
        }

        protected virtual void LoadData()
        {
            m_ChestData = ItemLoader.GetChestRewardData(m_ChestType);
        }
        
        #endregion


        #region Animation & Particles

        protected virtual IEnumerator PlayAnimationOnce(string animationName)
        {
            m_Animator.Play(animationName);

            // wait for the animation to start
            while (!m_Animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
            {
                yield return null;
            }

            // wait for the end of the animation
            while (m_Animator.GetCurrentAnimatorStateInfo(0).IsName(animationName))
            {
                yield return null;
            }
        }

        public virtual void ActivateIdle(bool activate = true, bool withAura = false, bool withSound = false, bool isLocal = false)
        {
            m_Animator.enabled = activate;

            if (activate)
                m_Animator.Play(IDLE_ANIMATION);

            ActivateAura(withAura, isLocal: isLocal);

            if (withSound && m_ChestData.IdleSoundFX != null)
                m_AudioSource = SoundFXManager.PlaySoundFXClip(m_ChestData.IdleSoundFX);
        }

        public virtual void ActivateOpen(bool withOpenParticles = true)
        {
            if (m_AudioSource.isActiveAndEnabled)
                Destroy(m_AudioSource);

            StartCoroutine(PlayOpenAnimation());
        }

        public virtual void ActivateAura(bool activate = true, bool isLocal = false)
        {
            if (!m_AuraEffects)
                return;

            m_AuraEffects.SetActive(activate);

            if (!activate)
                return;
            
            Vector3 chestScale = m_Preview.transform.lossyScale;

            // Set simulation space on all ParticleSystems
            foreach (var ps in m_AuraEffects.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.simulationSpace = isLocal ? ParticleSystemSimulationSpace.Local : ParticleSystemSimulationSpace.World;
                ps.transform.localScale = new Vector3(ps.transform.localScale.x * chestScale.x, ps.transform.localScale.y * chestScale.y, ps.transform.localScale.z * chestScale.z);
            }
        }


        public virtual void ActivateOpenParticles(bool activate = true)
        {
            m_OpeningEffects.SetActive(activate);
        }

        public virtual IEnumerator PlayOpenAnimation()
        {
            m_Animator.Play(OPEN_ANIMATION);
            ActivateOpenParticles(true);

            if (m_AudioSource != null)
                Destroy(m_AudioSource);

            SoundFXManager.PlayOnce(m_ChestData.OpenSoundFX != null ? m_ChestData.OpenSoundFX : SoundFXManager.DefaultChestOpenSoundFX);

            // wait for the animation to start
            while (!m_Animator.GetCurrentAnimatorStateInfo(0).IsName(OPEN_ANIMATION))
            {
                yield return null;
            }

            // wait for the end of the animation
            while (m_Animator.GetCurrentAnimatorStateInfo(0).IsName(OPEN_ANIMATION))
            {
                yield return null;
            }

            ActivateOpenParticles(false);
        }

        #endregion
    }
}