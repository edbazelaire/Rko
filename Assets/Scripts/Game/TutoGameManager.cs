using Enums;
using Game.UI;
using Menu.Common.Buttons;
using System.Collections;
using UnityEngine;


namespace Game
{
    public class TutoGameManager : MObject
    {
        #region Members 

        static TutoGameManager s_Instance;

        Controller m_Controller;
        Controller m_EnemyController;

        public static TutoGameManager Instance
        {
            get
            {
                if (s_Instance != null)
                    return s_Instance;

                s_Instance = FindAnyObjectByType<TutoGameManager>();
                if (s_Instance == null)
                    s_Instance = new GameObject("TutoGameManager").AddComponent<TutoGameManager>();

                if (!s_Instance.m_Initialized)
                    s_Instance.Initialize();

                return s_Instance;
            }
        }

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region Activation

        public void Activate(Controller controller, Controller enemy)
        {
            // save controller
            m_Controller = controller;
            m_EnemyController = enemy;

            // activate TutoManager
            gameObject.SetActive(true);

            // activate TutoUI
            GameUIManager.TutoGameUI.gameObject.SetActive(true);
            GameUIManager.TutoGameUI.Initialize();

            // setup controller for Tutorial (remove actions and spells)
            foreach (ESpell spell in m_Controller.SpellHandler.Spells)
            {
                m_Controller.SpellHandler.SetSpellSelection(spell, ESpellSelectionState.Inactive);
            }
            m_Controller.SpellHandler.Activate(false);
            m_Controller.AutoAttackHandler.Activate(false);

            // start the tutorial coroutine
            StartCoroutine(StartTuto());
        }

        IEnumerator StartTuto()
        {
            GameUIManager.TutoGameUI.Activate(true);

            // presentation
            yield return GameUIManager.TutoGameUI.PlayPresentation();

            // start game
            GameManager.Instance.SetState(EGameState.GameRunning);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            //GameManager.Instance.State += OnValueChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}
