using Data;
using Enums;
using Game.Loaders;
using Game.Spells;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Character
{
    public class AnimationHandler : NetworkBehaviour
    {
        #region Members

        bool m_Initialized = false;

        // ===========================================================================================
        // GameObject & Components 
        /// <summary> controller of this AnimationHandler </summary>
        Controller              m_Controller;
        /// <summary> animator of the Character </summary>
        Animator                m_Animator;

        #endregion


        #region Init & End

        public void Initialize(Animator animator)
        {
            if (animator == null)
            {
                ErrorHandler.Error("animator not found");
            }

            m_Controller = GetComponent<Controller>();
            m_Animator = animator;

            m_Controller.StateHandler.StateEffectList.OnListChanged         += OnStateEffectListChanged;
            m_Controller.StateHandler.SpeedBonus.OnValueChanged             += OnSpeedBonusValueChanged;
            m_Controller.StateHandler.AnimationState.OnValueChanged         += OnStateAnimationValueChanged;
            m_Controller.CounterHandler.HasCounter.OnValueChanged           += OnHasCounterValueChanged;

            m_Initialized = true;
        }

        #endregion


        #region Inherited Manipulators

        public void Update()
        {
            if (!m_Initialized)
                return;

            MoveAnimation(m_Controller.Movement.IsMoving);
        }

        #endregion


        #region Public Manipulators

        public void CancelCurrentAnimation()
        {
            m_Animator.Play("Idle");
        }

        /// <summary>
        /// Update Animation MovementSpeed factor depending on current speed
        /// </summary>
        public void UpdateMovementSpeed()
        {
            ErrorHandler.Log("Setting Animation MovementSpeed factor : " + m_Controller.Movement.Speed, ELogTag.Animation);
            m_Animator.SetFloat("MovementSpeed", m_Controller.Movement.Speed);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="win"></param>
        public void GameOverAnimation(bool win)
        {
            ErrorHandler.Log("Player (" + m_Controller.PlayerId + ") GameOverAnimation", ELogTag.Animation);
            m_Animator.SetTrigger(win ? EAnimation.Win.ToString() : EAnimation.Loss.ToString());
        }

        #endregion


        #region General Animations

        [ClientRpc]
        public void PlayAnimationClientRPC(EAnimation animation, float duration = -1f)
        {
            PlayAnimation(animation, duration);
        }

        public void PlayAnimation(EAnimation animation, float duration = -1f)
        {
            if (animation == EAnimation.None || duration == 0f)
                return;

            ErrorHandler.Log(animation + " animation with a duration of " + duration, ELogTag.Animation);

            // update speed of the animation
            m_Animator.SetFloat("CastSpeed", duration <= 0f ? 1f : 1 / duration);

            m_Animator.SetTrigger(animation.ToString());
        }

        #endregion


        #region Specific Animations Manipulators

        void MoveAnimation(bool isMoving)
        {
            m_Animator.SetBool("IsMoving", isMoving);
        }

        [ClientRpc]
        public void CancelCastAnimationClientRpc(EAnimation animation = EAnimation.None)
        {
            CancelCastAnimation(animation);
        }

        public void CancelCastAnimation(EAnimation animation = EAnimation.None)
        {
            // if current animation is not requested animation to cancel => return
            if (animation != EAnimation.None && !IsCurrentAnimation(animation.ToString()))
                return;

            ErrorHandler.Log("CancelCastAnimation : " + animation, ELogTag.Animation);

            m_Animator.SetTrigger(EAnimation.CancelCast.ToString());
            m_Animator.SetFloat("CastSpeed", 1f);

            // Reset the trigger to avoid it staying "active"
            CoroutineManager.DelayMethod(() => m_Animator.ResetTrigger(EAnimation.CancelCast.ToString()));
        }

        #endregion


        #region Helpers

        public bool IsCurrentAnimation(string animation)
        {
            return m_Animator.GetCurrentAnimatorStateInfo(0).IsName(animation);
        }

        #endregion


        #region Events Listeners

        /// <summary>
        /// Change MovementSpeed parameter in the Animator when the speed value changes
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        void OnSpeedBonusValueChanged(float oldValue, float newValue)
        {
            UpdateMovementSpeed();
        }

        /// <summary>
        /// Change MovementSpeed parameter in the Animator when the speed value changes
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        void OnStateAnimationValueChanged(EAnimation oldValue, EAnimation newValue)
        {
            ErrorHandler.Log("STATE ANIMATION CHANGED : animation = " + newValue.ToString().ToUpper(), ELogTag.Animation);

            if (newValue == EAnimation.None)
            {
                m_Animator.SetTrigger("StopStateAnimation");
                return;
            }

            m_Animator.SetTrigger(newValue.ToString());
        }

        /// <summary>
        /// Change MovementSpeed parameter in the Animator when the speed value changes
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        void OnHasCounterValueChanged(bool oldValue, bool newValue)
        {
            ErrorHandler.Log("STATE ANIMATION CHANGED : Counter = " + newValue.ToString().ToUpper(), ELogTag.Animation);

            m_Animator.SetBool("HasCounter", newValue);
        }

        /// <summary>
        /// When a state effect is added or removed
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        void OnStateEffectListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            ErrorHandler.Log(changeEvent.Type + " " + changeEvent.Value, ELogTag.Animation);

            if (changeEvent.Type != NetworkListEvent<FixedString64Bytes>.EventType.RemoveAt && changeEvent.Type != NetworkListEvent<FixedString64Bytes>.EventType.Remove)
                OnAddStateEffect(changeEvent.Value.ToString());
            else
                OnRemoveStateEffect(changeEvent.Value.ToString());

            if (changeEvent.Value == EStateEffect.Jump.ToString())
            {
                if (changeEvent.Type == NetworkListEvent<FixedString64Bytes>.EventType.Add)
                {
                    m_Controller.Collider.enabled = false;
                    m_Animator.SetTrigger(EAnimation.Jump.ToString());
                }
                else
                {
                    m_Controller.Collider.enabled = true;
                    m_Animator.SetTrigger(EAnimation.CancelCast.ToString());
                }
                return;
            }
        }

        void OnAddStateEffect(string stateEffectName)
        {
            StateEffect data = SpellLoader.GetStateEffect(stateEffectName);

            if (data.Animation != EAnimation.None)
                m_Animator.SetTrigger(data.Animation.ToString());
        }

        void OnRemoveStateEffect(string stateEffectName)
        {
            StateEffect data = SpellLoader.GetStateEffect(stateEffectName);
         
            if (data.Animation != EAnimation.None)
                m_Animator.SetTrigger(EAnimation.CancelStateEffect.ToString());
        }
               
        #endregion
    }
}