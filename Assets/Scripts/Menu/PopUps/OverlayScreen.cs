using Assets.Scripts.Managers;
using Assets.Scripts.Managers.Sound;
using Enums;
using System;
using System.Collections;
using Tools;
using Tools.Animations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class OverlayScreen : MObject
    {
        #region Members
        
        [SerializeField, Tooltip("Is that screen blocking quick method to go to another screen")]
        protected bool m_IsBlockingQuickClose = false;

        protected Canvas m_Canvas;
        protected float m_EndingTimer = 0f;
        /// <summary> is the game object done displaying (or done exiting display) </summary>
        protected bool m_IsDisplayed = false;

        // ===========================================================================================================
        // PUBLIC DEPENDENT MEMBERS
        public bool IsCurrent => this == ScreenManager.CurrentScreen;
        public Canvas Canvas => m_Canvas;
        public bool Initialized => m_Initialized;
        public bool IsDisplayed => ! gameObject.IsDestroyed() && m_IsDisplayed;

        public string PopUpName
        {
            get
            {
                return name.Replace(" (Clone)", "");
            }
        }

        public EPopUpState PopUpState
        {
            get
            {
                if (Enum.TryParse(PopUpName, out EPopUpState popupState))
                    return EPopUpState.None;

                return popupState;
            }
        }

        #endregion



        #region Init & End

        public override void Initialize()
        {
            ScreenManager.AddScreen(this);

            gameObject.SetActive(false);

            OnEnter();

            CoroutineManager.DelayMethod(LoadAndSetup);

            //m_Initialized = true;
        }

        /// <summary>
        /// Happens BEFORE loading and Setting of the GameObject
        /// </summary>
        protected virtual void OnEnter() { }

        /// <summary>
        /// Happens right before destroying the prefab
        /// </summary>
        protected virtual void OnExit()
        {
            PlaySoundFX();

            ScreenManager.RemoveScreen(this);

            UnRegisterButtons();
            UnRegisterListeners();
        }

        #endregion


        #region Loading

        protected virtual void LoadAndSetup()
        {
            // find components of the GameObject
            FindComponents();

            // adjust size ratio to match the size of the screen
            AdjustAspectRatio();

            // init & setup UI
            OnPrefabLoaded();

            // register buttons
            RegisterButtons();

            // register listeners
            RegisterListeners();
           
            // init completed - display game object and start autoscript if any
            OnInitializationCompleted();
        }

        protected override void FindComponents() 
        {
            m_Canvas = Finder.FindComponent<Canvas>(gameObject);
         
            m_Canvas.worldCamera        = Camera.main;
            m_Canvas.sortingLayerName   = "Overlay";
            m_Canvas.sortingOrder       = ScreenManager.OrderInLayer;
        }
        
        protected virtual void AdjustAspectRatio() { }

        protected virtual void OnPrefabLoaded() { }

        protected virtual void OnInitializationCompleted()
        {
            // re-activate game object
            gameObject.SetActive(true);

            // call init done
            m_Initialized = true;

            // play sound animation
            PlaySoundFX();

            // play enter animation
            EnterAnimation();
        }

        protected virtual void EnterAnimation() 
        {
            var fadeIn = gameObject.AddComponent<Fade>();
            fadeIn.Initialize("", duration: 0.35f, startOpacity: 0);

            m_IsDisplayed = true;
        }

        #endregion


        #region Ending

        /// <summary>
        /// Happens before destroying the game object
        /// </summary>
        public virtual void Exit()
        {
            OnExit();

            StartCoroutine(DestroyCoroutine());
        }

        protected virtual IEnumerator DestroyCoroutine()
        {
            yield return ExitAnimation();

            Destroy(gameObject);
        }

        protected virtual IEnumerable ExitAnimation() { yield break; }

        #endregion


        #region Sound & Other

        protected virtual void PlaySoundFX()
        {
            SoundFXManager.PlayOnce(SoundFXManager.OpenOverlayScreenSoundFX);
        }

        #endregion


        #region Updates

        protected virtual void Update()
        {
            if (!m_Initialized)
                return;

            CheckTouch();
        }

        void CheckTouch()
        {
            if (Input.GetMouseButtonDown(0)) // Check for left mouse button click
            {
                GameObject clickedObject = EventSystem.current.currentSelectedGameObject;

                // dont do anything on touching button 
                if (clickedObject != null && clickedObject.GetComponent<Button>() != null)
                    return;

                // Invoke the event with the clicked GameObject as parameter
                OnTouch(clickedObject);
            }
        }

        #endregion


        #region Buttons

        protected virtual void RegisterButtons()
        {
            var buttons = Finder.FindComponents<Button>(gameObject, throwError: false);
            if (buttons == null)
                return;

            foreach (var button in buttons)
            {
                button.onClick.AddListener(() => OnUIButton(button.name));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void UnRegisterButtons()
        {
            var buttons = Finder.FindComponents<Button>(gameObject, throwError: false);
            if (buttons == null)
                return;

            foreach (var button in buttons)
            {
                button.onClick.RemoveAllListeners();
            }
        }

        /// <summary>
        /// When a button is clicked : call a listener method depending on the name 
        /// </summary>
        /// <param name="bname"></param>
        protected virtual void OnUIButton(string bname)
        {
            switch (bname)
            {
                case "Background":
                case "CancelButton":
                    OnCancelButton();
                    break;

                default:
                    //ErrorHandler.Warning("Unregistered button : " + bname);
                    return;
            }
        }
        protected virtual void OnCancelButton()
        {
            Exit();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="colliderName"></param>
        protected virtual void OnTouch(GameObject gameObject)
        {

        }

        #endregion


        
    }
}