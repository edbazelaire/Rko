using Managers.Tuto;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;


namespace Game.UI
{
    public class HandUI : MObject
    {
        #region Members

        [SerializeField]
        Vector3 m_ClickOffset;

        Animator m_Animator;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Animator = Finder.FindComponent<Animator>(gameObject);
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


        #region GUI Manipulators

        public void Activate(bool activate)
        {
            gameObject.SetActive(activate);
        }

        public IEnumerator ClickOn(GameObject gameObject, Vector3? clickOffset = null)
        {
            clickOffset ??= m_ClickOffset;
            transform.position = gameObject.transform.position + clickOffset.Value;
            var button = Finder.FindComponent<Button>(gameObject, throwError: false);
            bool hasButton = button != null;
            if (! hasButton)
            {
                button = gameObject.AddComponent<Button>();
            }

            bool clicked = false;
            void WaitButtonClicked()
            {
                clicked = true;
            }
            button.onClick.AddListener(WaitButtonClicked);

            Activate(true);

            float timer = 0f;
            while (! clicked)
            {
                timer -= Time.deltaTime;
                if (timer <= 0)
                {
                    timer = 2.5f;
                    m_Animator.Play("ClickOn");
                }
                yield return null;
            }

            Activate(false);
            button.onClick.RemoveListener(WaitButtonClicked);

            // remove button at the end if did not had one at first
            if (!hasButton)
                Destroy(button);
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
