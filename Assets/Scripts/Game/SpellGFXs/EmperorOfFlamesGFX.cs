using Data;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Game.SpellGFXs
{
    public class EmperorOfFlamesGFX : SpellGFX
    {
        #region Members

        [SerializeField] float m_MoveFireSize = 10f;
        [SerializeField] float m_CharacterHight = 2f;
        [SerializeField] float m_GrowingChargeSize = 0.3f;

        // ================================================================================
        // GameObjects & Components
        ParticleSystem m_FireTorch;
        ParticleSystem m_FireMovingLeft;
        ParticleSystem m_FireMovingRight;
        ParticleSystem m_FireEnergyCharge;
        ParticleSystem m_EnergyExplosion;

        // ================================================================================
        // Data
        Queue<IEnumerator> m_AnimationQueue = new Queue<IEnumerator>();
        float m_AnimationTimer;
        float m_Timer;
        Vector3 m_BasePosition;

        Transform m_CharacterPreview => m_Controller.GFXHandler.CharacterPreview.transform;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            // Find all Particles
            m_FireTorch         = Finder.FindComponent<ParticleSystem>("FireTorch");
            m_FireMovingLeft    = Finder.FindComponent<ParticleSystem>("FireMoving_Left");
            m_FireMovingRight   = Finder.FindComponent<ParticleSystem>("FireMoving_Right");
            m_FireEnergyCharge  = Finder.FindComponent<ParticleSystem>("FireEnergyCharge");
            m_EnergyExplosion   = Finder.FindComponent<ParticleSystem>("EnergyExplosion");

            // deactivate all Particles
            m_FireTorch.gameObject.SetActive(false);
            m_FireMovingLeft.gameObject.SetActive(false);
            m_FireMovingRight.gameObject.SetActive(false);
            m_FireEnergyCharge.gameObject.SetActive(false);
            m_EnergyExplosion.gameObject.SetActive(false);

            // init data 
            m_BasePosition = m_CharacterPreview.localPosition;
            m_AnimationTimer = m_SpellData.AnimationTimer / m_Controller.SpellHandler.GetCastSpeed(m_SpellData.Name);
            m_Timer = m_AnimationTimer;
        }

        protected override void StartAnimation()
        {
            // Queue animations
            m_AnimationQueue.Enqueue(MoveFire());
            m_AnimationQueue.Enqueue(FireEruption());
            m_AnimationQueue.Enqueue(ChargeEnergy());

            // Start the first animation
            StartCoroutine(NextAnimation());
        }

        public override void End()
        {
            // deactivate energu charge
            m_FireEnergyCharge.gameObject.SetActive(false);

            // activate explosion
            m_EnergyExplosion.transform.position = m_CharacterPreview.position;
            m_EnergyExplosion.gameObject.SetActive(true);

            // make controller back on its position
            m_Controller.AnimationHandler.CancelCurrentAnimation();
            m_CharacterPreview.localPosition = m_BasePosition;

            base.End();
        }

        protected override void ForceEnd()
        {
            m_FireEnergyCharge.gameObject.SetActive(false);
            m_EnergyExplosion.gameObject.SetActive(false);

            base.ForceEnd();
        }

        #endregion


        #region Animations

        private void Update()
        {
            m_Timer -= Time.deltaTime;

            if (m_Timer < 0)
                End();
        }

        private IEnumerator NextAnimation(float timer = 0f)
        {
            if (m_AnimationQueue.Count == 0)
                yield break;

            yield return new WaitForSeconds(timer);

            IEnumerator currentAnimation = m_AnimationQueue.Dequeue();
            yield return StartCoroutine(currentAnimation);
        }

        IEnumerator MoveFire()
        {
            var halfTimer = m_SpellData.AnimationTimer * 0.25f / 2;         // half the time the animation should take
            var timer = halfTimer * 2;

            m_FireMovingLeft.gameObject.SetActive(true);
            m_FireMovingRight.gameObject.SetActive(true);

            StartCoroutine(NextAnimation(0.9f * timer));

            while (timer > 0)
            {
                timer -= Time.deltaTime;
                m_FireMovingLeft.transform.localPosition = new Vector3(- m_MoveFireSize * (1 - Mathf.Abs(halfTimer - timer) / halfTimer), 0f, 0f);
                m_FireMovingRight.transform.localPosition = new Vector3(m_MoveFireSize * (1 - Mathf.Abs(halfTimer - timer) / halfTimer), 0f, 0f);
                yield return null;
            }

            m_FireMovingLeft.gameObject.SetActive(false);
            m_FireMovingRight.gameObject.SetActive(false);
        }

        IEnumerator FireEruption()
        {
            m_FireTorch.gameObject.SetActive(true);

            var baseTimer = m_SpellData.AnimationTimer * 0.25f;         // half the time the animation should take
            var timer = baseTimer;                                      // remaining time for the animation
            var baseScale = m_FireTorch.transform.localScale;           // base scale of the fire torch

            StartCoroutine(NextAnimation(0.2f * timer));

            m_Controller.AnimationHandler.PlayAnimation(Enums.EAnimation.Airborne, -1);
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                m_FireTorch.transform.localScale = baseScale * (baseTimer - timer) / baseTimer;
                m_CharacterPreview.localPosition = new Vector3(m_BasePosition.x, m_BasePosition.y + m_CharacterHight * (baseTimer - timer) / baseTimer, 0f);
                yield return null;
            }

            m_FireTorch.gameObject.SetActive(false);                // deactivate at the end of the animation
            m_FireTorch.transform.localScale = baseScale;           // reset scale
        }

        IEnumerator ChargeEnergy()
        {
            m_FireEnergyCharge.transform.parent = m_CharacterPreview;
            m_FireEnergyCharge.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            m_FireEnergyCharge.gameObject.SetActive(true);

            var baseTimer = m_Timer - 0.1f;                              // timer is current remaining time
            var timer = baseTimer;                                      // remaining time for the animation
            var baseScale = m_FireEnergyCharge.transform.localScale;    // base scale of the fire torch

            while (timer > 0)
            {
                timer -= Time.deltaTime;
                m_FireEnergyCharge.transform.localScale = baseScale * (1 + m_GrowingChargeSize * (baseTimer - timer) / baseTimer);
                yield return null;
            }

            m_FireEnergyCharge.gameObject.SetActive(false);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}
