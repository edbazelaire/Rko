using Assets.Scripts.Managers.Sound;
using System.Collections;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Menu.MainMenu.MainTab.Chests
{
    public class PowerOrbUI : ChestUI
    {
        #region Members

        const string UPGRADE_FAILED_ANIMATION = "ChestShake";

        GameObject m_OnClickEffects;
        GameObject m_UpgradeEffects;
        GameObject m_LocatedClickEffect;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_OnClickEffects = Finder.Find(gameObject, "OnClickEffects");
            m_UpgradeEffects = Finder.Find(gameObject, "UpgradeEffects");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_AuraEffects.SetActive(true);
            m_OnClickEffects.SetActive(false);
            m_UpgradeEffects.SetActive(false);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            StopAllCoroutines();
        }

        #endregion


        #region Animation Activation

        public override void ActivateIdle(bool withAura = false, bool withSound = false)
        {
            m_Animator.Play(IDLE_ANIMATION);
            ActivateAura(withAura);
        }

        public override void ActivateAura(bool activate = true)
        {
            m_AuraEffects.SetActive(activate);
        }

        public override void ActivateOpenParticles(bool activate = true)
        {
            m_OpeningEffects.SetActive(activate);
        }

        public override void ActivateOpen(bool withOpenParticles = true)
        {
            if (m_AudioSource.isActiveAndEnabled)
                Destroy(m_AudioSource);

            StartCoroutine(PlayOpenAnimation());
        }

        #endregion


        #region On Click

        public void PlayOnClickAnimation()
        {
            StartCoroutine(OnClickAnimation());
        }

        public IEnumerator OnClickAnimation()
        {
            var duration = 1f;
            m_OnClickEffects.SetActive(true);

            while (duration > 0f)
            {
                duration -= Time.deltaTime;
                yield return null;
            }

            m_OnClickEffects.SetActive(false);
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
            var duration = 1f;

            m_UpgradeEffects.SetActive(true);

            while (duration > 0f)
            {
                duration -= Time.deltaTime;
                yield return null;
            }

            m_UpgradeEffects.SetActive(true);
        }

        #endregion


        #region Open Effect



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

        #endregion



        

       


        #region Data

        protected override void LoadData()
        {
            
        }

        #endregion
    }
}