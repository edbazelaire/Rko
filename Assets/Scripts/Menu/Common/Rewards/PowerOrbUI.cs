using Assets.Scripts.Managers.Sound;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Menu.MainMenu.MainTab.Chests
{
    public class PowerOrbUI : ChestUI
    {
        #region Members

        const string UPGRADE_FAILED_ANIMATION = "ChestShake";

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_AuraEffects.SetActive(true);
        }
        
        #endregion


        #region Animation & Particles

        public override void ActivateIdle(bool withAura = false, bool withSound = false)
        {
            m_Animator.Play(IDLE_ANIMATION);
            ActivateAura(withAura);
        }

        public override void ActivateOpen(bool withOpenParticles = true)
        {
            if (m_AudioSource.isActiveAndEnabled)
                Destroy(m_AudioSource);

            StartCoroutine(PlayOpenAnimation());
        }

        public override void ActivateAura(bool activate = true)
        {
            m_AuraEffects.SetActive(activate);
        }

        public override void ActivateOpenParticles(bool activate = true)
        {
            m_OpeningEffects.SetActive(activate);
        }

        public override IEnumerator PlayOpenAnimation()
        {
            if (m_AudioSource != null)
                Destroy(m_AudioSource);

            yield return PlayAnimationOnce(OPEN_ANIMATION);

            ActivateOpenParticles(true);

            m_SpriteRenderer.enabled = false;

            SoundFXManager.PlayOnce(SoundFXManager.OpenOrbSoundFX);

            yield return new WaitForSeconds(0.5f);

            ActivateOpenParticles(false);
        }

        public IEnumerator UpgradeFailedAnimation()
        {
            if (m_AudioSource != null)
                Destroy(m_AudioSource);

            SoundFXManager.PlayOnce(SoundFXManager.UpgradeFailSoundFX);

            yield return PlayAnimationOnce(UPGRADE_FAILED_ANIMATION);
        }

        public IEnumerator UpgradeSuccessAnimation()
        {
            yield return null;
        }

        #endregion


        #region Data

        protected override void LoadData()
        {
            
        }

        #endregion
    }
}